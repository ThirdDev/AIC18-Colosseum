using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Colosseum.Services
{
    public static class DockerService
    {
        public static void BuildImage(string path, string name)
        {
            using (var shell = PowerShell.Create())
            {
                shell.AddScript($"docker build -t {name} {path}").Invoke();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="filesDirectory"></param>
        /// <returns>container Id</returns>
        public static string RunArena(string name, string filesDirectory)
        {
            using (var shell = PowerShell.Create())
            {
                var result = shell.AddScript($"docker run -d -it --mount type=bind,source=\"{filesDirectory}\",target=/app/files {name}").Invoke();
                if (result.Count > 1)
                {
                    throw new Exception($"docker run returned more than 1 line:{Environment.NewLine}{string.Join(Environment.NewLine, result.AsEnumerable())}");
                }
                return result.First().ToString();
            }
        }

        public static bool IsContainerRunning(string containerId)
        {
            using (var shell = PowerShell.Create())
            {
                var result = shell.AddScript($"docker ps --filter \"id={containerId}\"").Invoke();
                return result.Count == 2;
            }
        }

        public static string ContainerLog(string containerId)
        {
            using (var shell = PowerShell.Create())
            {
                var result = shell.AddScript($"docker logs -f {containerId}").Invoke();
                return result.ToString();
            }
        }

        public static void StopAndRemoveContainer(string containerId)
        {
            using (var shell = PowerShell.Create())
            {
                shell.AddScript($"docker stop {containerId}").Invoke();
                shell.AddScript($"docker rm {containerId}").Invoke();
            }
        }

        public static void StopAndRemoveAllContainers()
        {
            using (var shell = PowerShell.Create())
            {
                shell.AddScript("docker stop $(docker ps -aq)").Invoke();
                shell.AddScript("docker rm $(docker ps -aq)").Invoke();
            }
        }

    }
}
