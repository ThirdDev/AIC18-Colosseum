using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Colosseum.Services
{


    public class OperationSystemServices : IDisposable
    {
        private readonly int initTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        private readonly List<Process> _processes = new List<Process>();

        public OperationSystemServices(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger(nameof(OperationSystemServices));
        }

        public async Task RunCommandAsync(CommandInfo commandInfo,
            CancellationToken cancellationToken,
            ProcessPayload processPayload = null,
            Action<string> outputReceived = null,
            Action<string> errorReveived = null,
            Action exited = null,
            bool log = false)
        {
            cancellationToken.ThrowIfCancellationRequested();

            outputReceived = outputReceived ?? (x => { });
            errorReveived = errorReveived ?? (x => { });
            exited = exited ?? (() => { });

            processPayload = processPayload ?? new ProcessPayload();

            var process = new Process();

            process = new Process();
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            if (commandInfo.RequiresBash)
            {
                process.StartInfo.FileName = Environment.GetEnvironmentVariable("SHELL");
                process.StartInfo.Arguments = "-s";
            }
            else
            {
                process.StartInfo.FileName = commandInfo.FileName;
                process.StartInfo.Arguments = commandInfo.Args;
            }

            var logCommandInfoSemaphore = new SemaphoreSlim(1, 1);
            async Task<(string stdOut, string stdErr)> logFilePathAsync()
            {
                var commandsLogDir = new DirectoryInfo($"logs/commands/{initTime}");
                commandsLogDir.Create();
                var commandLogDir = commandsLogDir.CreateSubdirectory($"{process.Id}_{commandInfo.OneLineCommand.ToValidFileName().Substring(0, 10)}");
                commandLogDir.Create();
                var commandInfoFilePath = Path.Combine(commandLogDir.FullName, "command.info");
                await logCommandInfoSemaphore.WaitAsync();
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

                var stdOutFile = Path.Combine(commandLogDir.FullName, "out.txt");
                var stdErrFile = Path.Combine(commandLogDir.FullName, "err.txt");

                return (stdOutFile, stdErrFile);
            }

            HashSet<object> processEndLocks = new HashSet<object>();

            process.OutputDataReceived += async (sender, data) =>
            {
                var endLock = new object();
                if (processEndLocks != null)
                {
                    processEndLocks.AddThreadSafe(endLock);
                }
                else
                {
                    _logger.LogError($"processEndLocks for process {process.Id} has been disposed but output event is still firing. command: {commandInfo.OneLineCommand}");
                }
                try
                {
                    if (data.Data.IsNullOrWhiteSpace().Not())
                    {
                        if (log)
                        {
                            await writeCommandLogAsync((await logFilePathAsync()).stdOut, $"{DateTime.Now}: {data.Data}", cancellationToken);
                        }
                        outputReceived(data.Data);
                    }
                }
                finally
                {
                    if (processEndLocks != null)
                    {
                        processEndLocks.RemoveThreadSafe(endLock);
                    }
                }
            };

            process.ErrorDataReceived += async (sender, data) =>
            {
                var endLock = new object();
                if (processEndLocks != null)
                {
                    processEndLocks.AddThreadSafe(endLock);
                }
                else
                {
                    _logger.LogError($"processEndLocks for process {process.Id} has been disposed but error event is still firing. command: {commandInfo.OneLineCommand}");
                }
                try
                {
                    if (data.Data.IsNullOrWhiteSpace().Not())
                    {
                        if (log)
                        {
                            await writeCommandLogAsync((await logFilePathAsync()).stdErr, $"{DateTime.Now}: {data.Data}", cancellationToken);
                        }
                        errorReveived(data.Data);
                    }
                }
                finally
                {
                    if (processEndLocks != null)
                    {
                        processEndLocks.RemoveThreadSafe(endLock);
                    }
                }

            };

            process.Exited += async (_, __) =>
            {
                var endLock = new object();
                if (processEndLocks != null)
                {
                    processEndLocks.AddThreadSafe(endLock);
                }
                else
                {
                    _logger.LogError($"processEndLocks for process {process.Id} has been disposed but exit event is still firing. command: {commandInfo.OneLineCommand}");
                }
                try
                {
                    if (log)
                    {
                        await writeCommandLogAsync((await logFilePathAsync()).stdOut, $"{DateTime.Now}: exited.", cancellationToken);
                    }
                    exited();
                }
                finally
                {
                    if (processEndLocks != null)
                    {
                        processEndLocks.RemoveThreadSafe(endLock);
                    }
                }
            };

            cancellationToken.Register(() =>
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            });

            process.EnableRaisingEvents = true;

            _processes.AddThreadSafe(process);

            if (process.Start())
            {
                processPayload.ProcessId = process.Id;
                if (log)
                {
                    await writeCommandLogAsync((await logFilePathAsync()).stdOut, $"{DateTime.Now}: started.", cancellationToken);
                }
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                if (commandInfo.RequiresBash)
                {
                    await process.StandardInput.WriteLineAsync(commandInfo.OneLineCommand);
                    if (commandInfo.HasStandardInput)
                    {
                        await process.StandardInput.WriteLineAsync(commandInfo.GetStandardInput());
                    }
                    await process.StandardInput.WriteLineAsync("exit");
                }
                else
                {
                    if (commandInfo.HasStandardInput)
                    {
                        await process.StandardInput.WriteLineAsync(commandInfo.GetStandardInput());
                    }
                }
            }
            else
            {
                throw new Exception("could not start process");
            }

            await process.WaitForExitAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                process.Kill();

            await Task.Run(async () => { while (processPayload.IsRunning()) { await Task.Delay(100); } }, cancellationToken);
            await Task.Run(async () => { while (processEndLocks.ToList().Any()) { await Task.Delay(100); } }, cancellationToken);
            await Task.Delay(commandInfo.WaitAfter, cancellationToken);

            _processes.RemoveThreadSafe(process);
            Random r = new Random();

        }

        private readonly SemaphoreSlim writeCommandLogSemaphore = new SemaphoreSlim(1, 1);

        private async Task writeCommandLogAsync(string filePath, string log, CancellationToken cancellationToken)
        {
            await writeCommandLogSemaphore.WaitAsync();
            try
            {
                await File.AppendAllTextAsync(filePath, $"{log}{Environment.NewLine}", cancellationToken);
            }
            finally
            {
                writeCommandLogSemaphore.Release();
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private readonly ILogger _logger;

        protected virtual void Dispose(bool disposing)
        {
            Console.WriteLine($"disposing... $disposing: {disposing}");
            if (!disposedValue)
            {
                if (disposing)
                {
                    foreach (var process in _processes)
                    {
                        process.Kill();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~OSService() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }

    public class CommandInfo
    {
        public string FileName { get; set; }
        public string Args { get; set; } = "";
        public int WaitAfter { get; set; } = 0;
        public bool RequiresBash { get; set; } = false;
        public bool HasStandardInput { get; set; }
        [JsonIgnore]
        public Func<string> GetStandardInput { get; set; }

        public string OneLineCommand => $"{FileName} {Args}";
    }

    public class ProcessPayload
    {
        public int? ProcessId { get; set; }

        public bool IsRunning()
        {
            if (!ProcessId.HasValue)
            {
                return false;
            }
            try
            {
                Process.GetProcessById(ProcessId.Value);
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
            Process.GetProcessById(ProcessId.Value).Kill();
            if (IsRunning())
            {
                throw new Exception($"failed to kill process {ProcessId}");
            }
        }
    }
}
