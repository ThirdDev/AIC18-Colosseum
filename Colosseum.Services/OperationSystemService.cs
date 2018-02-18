using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Colosseum.Tools.SystemExtensions;
using Colosseum.Tools.SystemExtensions.Collection.Generic;
using Colosseum.Tools.SystemExtensions.Diagnostics;

namespace Colosseum.Services
{
    public static class OperationSystemService
    {
        private static readonly List<Process> _processes = new List<Process>();

        public static async Task RunCommandAsync(
            CommandInfo commandInfo,
            ProcessPayload processPayload,
            DirectoryInfo logDir,
            string workingDirectory,
            bool log = true,
            Action<string> outputReceived = null,
            Action<string> errorReceived = null,
            Action exited = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var process = new Process
            {
                StartInfo =
                {
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    WorkingDirectory = workingDirectory,
                    FileName = commandInfo.FileName,
                    Arguments = commandInfo.Args
                }
            };

            var logCommandInfoSemaphore = new SemaphoreSlim(1, 1);
            async Task<(string stdOut, string stdErr)> logFilePathAsync()
            {
                var commandInfoFilePath = Path.Combine(logDir.FullName, "command.info");
                await logCommandInfoSemaphore.WaitAsync(cancellationToken);
                try
                {
                    if (!File.Exists(commandInfoFilePath))
                    {
                        await File.WriteAllTextAsync(commandInfoFilePath, JsonConvert.SerializeObject(commandInfo, Formatting.Indented), cancellationToken);
                    }
                }
                finally
                {
                    logCommandInfoSemaphore.Release();
                }

                var stdOutFile = Path.Combine(logDir.FullName, "out.txt");
                var stdErrFile = Path.Combine(logDir.FullName, "err.txt");

                return (stdOutFile, stdErrFile);
            }

            #region EventListeners
            outputReceived = outputReceived ?? (x => { });
            errorReceived = errorReceived ?? (x => { });
            exited = exited ?? (() => { });

            var processEndLocks = new HashSet<object>();

            process.OutputDataReceived += async (sender, data) =>
            {
                var endLock = new object();
                processEndLocks.AddThreadSafe(endLock);
                try
                {
                    if (data.Data.IsNullOrWhiteSpace().Not())
                    {
                        await writeCommandLogAsync((await logFilePathAsync()).stdOut, $"{DateTime.Now:s}: {data.Data}", log, cancellationToken);
                        outputReceived(data.Data);
                    }
                }
                finally
                {
                    processEndLocks.RemoveThreadSafe(endLock);
                }
            };

            process.ErrorDataReceived += async (sender, data) =>
            {
                var endLock = new object();
                processEndLocks.AddThreadSafe(endLock);
                try
                {
                    if (data.Data.IsNullOrWhiteSpace().Not())
                    {
                        await writeCommandLogAsync((await logFilePathAsync()).stdErr, $"{DateTime.Now:s}: {data.Data}", log, cancellationToken);
                        errorReceived(data.Data);
                    }
                }
                finally
                {
                    processEndLocks.RemoveThreadSafe(endLock);
                }

            };

            process.Exited += async (_, __) =>
            {
                var endLock = new object();
                processEndLocks.AddThreadSafe(endLock);
                try
                {
                    await writeCommandLogAsync((await logFilePathAsync()).stdOut, $"{DateTime.Now:s}: exited.", log, cancellationToken);
                    exited();
                }
                finally
                {
                    processEndLocks.RemoveThreadSafe(endLock);
                }
            };
            #endregion

            cancellationToken.Register(() =>
            {
                if (processPayload.IsRunning())
                {
                    processPayload.Kill();
                }
            });

            process.EnableRaisingEvents = true;

            _processes.AddThreadSafe(process);

            if (process.Start())
            {
                processPayload.ProcessId = process.Id;
                await writeCommandLogAsync((await logFilePathAsync()).stdOut, $"{DateTime.Now:s}: started.", log, cancellationToken);
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
            }
            else
            {
                throw new Exception("could not start process");
            }

            await process.WaitForExitAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                process.Kill();

            await Task.Run(async () => { while (processPayload.IsRunning()) { await Task.Delay(100, cancellationToken); } }, cancellationToken);
            await Task.Run(async () => { while (processEndLocks.ToList().Any()) { await Task.Delay(100, cancellationToken); } }, cancellationToken);

            _processes.RemoveThreadSafe(process);
        }

        private static readonly SemaphoreSlim writeCommandLogSemaphore = new SemaphoreSlim(1, 1);

        private static async Task writeCommandLogAsync(string filePath, string logText, bool log, CancellationToken cancellationToken)
        {
            if (!log)
            {
                return;
            }
            await writeCommandLogSemaphore.WaitAsync(cancellationToken);
            try
            {
                await File.AppendAllTextAsync(filePath, $"{logText}{Environment.NewLine}", cancellationToken);
            }
            finally
            {
                writeCommandLogSemaphore.Release();
            }
        }
    }

    public class CommandInfo
    {
        public string FileName { get; set; }
        public string Args { get; set; } = "";

        [SuppressMessage("ReSharper", "UnusedMember.Global")]
        public string OneLineCommand => $"{FileName} {Args}";

        public static CommandInfo DockerCommand(string args) =>
            new CommandInfo
            {
                FileName = @"C:\Program Files\Docker\Docker\Resources\bin\docker.EXE",
                Args = args
            };
    }

    public class ProcessPayload
    {
        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public int? ProcessId { get; set; }

        public bool IsRunning()
        {
            if (!ProcessId.HasValue)
            {
                return false;
            }
            try
            {
                var x = Process.GetProcessById(ProcessId.Value);
                if (x.HasExited)
                    return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (ArgumentException ex) when (ex.Message.ToLower().Contains("is not running"))
            {
                return false;
            }
            return true;
        }

        public void Kill()
        {
            if (!ProcessId.HasValue)
            {
                throw new Exception("process id is empty");
            }
            if (!IsRunning())
            {
                return;
            }
            // ReSharper disable once PossibleInvalidOperationException
            Process.GetProcessById(ProcessId.Value).Kill();
            if (IsRunning())
            {
                throw new Exception($"failed to kill process {ProcessId}");
            }
        }
    }
}
