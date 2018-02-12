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
            return runDockerCommandAsync($"build -t {name} \"{path}\"", cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="filesDirectory"></param>
        /// <returns>container Id</returns>
        public static async Task<string> RunArenaAsync(string name, string filesDirectory, CancellationToken cancellationToken = default)
        {
            var result = await runDockerCommandWithOutputAsync($"run -d -it --mount type=bind,source=\"{filesDirectory}\",target=/app/files {name}", cancellationToken);
            if (result.Count > 1)
            {
                throw new Exception($"docker run returned more than 1 line:{Environment.NewLine}{string.Join(Environment.NewLine, result.AsEnumerable())}");
            }
            return result.First().ToString();
        }

        public static async Task<bool> IsContainerRunningAsync(string containerId, CancellationToken cancellationToken = default)
        {
            var result = await runDockerCommandWithOutputAsync($"ps --filter \"id={containerId}\"", cancellationToken);
            return result.Count == 2;
        }

        public static async Task<string> ContainerLogAsync(string containerId, CancellationToken cancellationToken = default)
        {
            var result = await runDockerCommandWithOutputAsync($"logs -f {containerId}", cancellationToken);
            return string.Join(Environment.NewLine, result);
        }

        public static async Task StopAndRemoveContainerAsync(string containerId, CancellationToken cancellationToken = default)
        {
            await runDockerCommandAsync($"stop {containerId}", cancellationToken);
            await runDockerCommandAsync($"rm {containerId}", cancellationToken);

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


        private static Task runDockerCommandAsync(string args, CancellationToken cancellationToken = default)
        {
            var command = CommandInfo.DockerCommand(args);
            return OperationSystemService.RunCommandAsync(command, new ProcessPayload(), tempLogDir(), null, cancellationToken: cancellationToken);
        }

        private static async Task<List<string>> runDockerCommandWithOutputAsync(string args, CancellationToken cancellationToken = default)
        {
            var command = CommandInfo.DockerCommand(args);
            List<string> final = new List<string>();
            await OperationSystemService.RunCommandAsync(command, new ProcessPayload(), tempLogDir(), null,
                outputReceived: line => final.Add(line),
                cancellationToken: cancellationToken);
            return final;
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
