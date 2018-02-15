using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Colosseum.Services
{
    public class ContainerInfo
    {
        public string Id { get; set; }
        public DirectoryInfo FilesDirectory { get; set; }
        public bool IsAvailable => Semaphore.CurrentCount != 0;
        public SemaphoreSlim Semaphore => new SemaphoreSlim(1);
    }

    public static class ContainerRepository
    {
        static List<ContainerInfo> containers = new List<ContainerInfo>();

        public static async Task InitalizeContainers(int count, string imageName, CancellationToken cancellationToken = default)
        {
            var arenaDir = new DirectoryInfo("arena");
            arenaDir.Create();
            var containerHomeDir = arenaDir.CreateSubdirectory("containers");

            foreach (var subdir in containerHomeDir.GetDirectories())
            {
                subdir.Delete(true);
            }

            for (int i = 0; i < count; i++)
            {
                var containerDir = containerHomeDir.CreateSubdirectory(i.ToString());
                var id = await DockerService.RunArenaAsync(imageName, containerDir.FullName, cancellationToken);
                var containerInfo = new ContainerInfo
                {
                    Id = id,
                    FilesDirectory = containerDir
                };
                containers.Add(containerInfo);
            }
        }

        public static ContainerInfo GetAFreeContainer()
        {
            var container = containers.FirstOrDefault(x => x.IsAvailable) ?? GetAFreeContainer();
            container.FilesDirectory.Delete(true);
            container.FilesDirectory.Create();
            return container;
        }
    }
}
