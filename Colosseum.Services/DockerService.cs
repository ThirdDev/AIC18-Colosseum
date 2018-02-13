using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Colosseum.Services
{
    public static class DockerService
    {
        public static Task BuildImageAsync(string path, string name, CancellationToken cancellationToken = default)
        {
            return runDockerCommandAsync($"build -t {name} \"{path}\"", false, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="filesDirectory"></param>
        /// <returns>container Id</returns>
        public static async Task<string> RunArenaAsync(string name, string filesDirectory, CancellationToken cancellationToken = default)
        {
            var result = await runDockerCommandWithOutputAsync($"run -d -it --mount type=bind,source=\"{filesDirectory}\",target=/app/files {name}", timeout: int.MaxValue, cancellationToken: cancellationToken);
            if (result.Count != 1)
            {
                throw new Exception($"docker run returned invalid output:{Environment.NewLine}{string.Join(Environment.NewLine, result.AsEnumerable())}");
            }
            return result.First().ToString();
        }

        public static async Task<bool> IsContainerRunningAsync(string containerId, CancellationToken cancellationToken = default)
        {
            var result = await GetContainerInfo(containerId, showAll: false, cancellationToken: cancellationToken);
            return result.Count == 2;
        }

        public static async Task<List<string>> GetContainerInfo(string containerId, bool showAll, CancellationToken cancellationToken = default)
        {
            var result = await runDockerCommandWithOutputAsync($"ps{(showAll ? " -a" : "")} --filter \"id={containerId}\"", cancellationToken: cancellationToken);
            return result;
        }

        public static async Task<string> GetContainerInspect(string containerId, CancellationToken cancellationToken = default)
        {
            var result = await runDockerCommandWithOutputAsync($"inspect {containerId}", cancellationToken: cancellationToken);
            return string.Join(Environment.NewLine, result);
        }

        public static async Task<string> ContainerLogAsync(string containerId, CancellationToken cancellationToken = default)
        {
            var result = await runDockerCommandWithOutputAsync($"logs -f {containerId}", cancellationToken: cancellationToken);
            return string.Join(Environment.NewLine, result);
        }

        public static async Task StopAndRemoveContainerAsync(string containerId, CancellationToken cancellationToken = default)
        {
            await runDockerCommandAsync($"kill {containerId}", true, cancellationToken);
            //await runDockerCommandAsync($"rm {containerId}", false, cancellationToken);

        }

        public static async Task StopAndRemoveAllContainersAsync(CancellationToken cancellationToken = default)
        {
            var running = await runDockerCommandWithOutputAsync("ps -aq");
            var tasks = new List<Task>();
            foreach (var container in running)
            {
                tasks.Add(StopAndRemoveContainerAsync(container, cancellationToken));
            }

            await Task.WhenAll(tasks);
        }


        private static Task runDockerCommandAsync(string args, bool log, CancellationToken cancellationToken = default)
        {
            var command = CommandInfo.DockerCommand(args);
            return OperationSystemService.RunCommandAsync(command, new ProcessPayload(), tempLogDir(), null, log: log, cancellationToken: cancellationToken);
        }

        private static async Task<List<string>> runDockerCommandWithOutputAsync(string args, int timeout = 10000, CancellationToken cancellationToken = default)
        {
            var command = CommandInfo.DockerCommand(args);
            var processPayload = new ProcessPayload();
            List<string> final = new List<string>();

            var processTask = OperationSystemService.RunCommandAsync(command, processPayload, tempLogDir(), null, log: false,
                outputReceived: line => final.Add(line),
                cancellationToken: cancellationToken);

            await Task.WhenAny(processTask, Task.Delay(timeout));

            processPayload.Kill();

            return final.ToListThreadSafe();
        }

        private static DirectoryInfo tempLogDir()
        {
            var arenaDir = new DirectoryInfo("arena");
            arenaDir.Create();

            var logDir = arenaDir.CreateSubdirectory("tmp");
            logDir.Create();

            return logDir;
        }
    }
}
