using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private static readonly List<ContainerInfo> _containers = new List<ContainerInfo>();

        public static async Task InitalizeContainers(int count, string imageName, CancellationToken cancellationToken = default)
        {
            var arenaDir = new DirectoryInfo("arena");
            arenaDir.Create();
            var containerHomeDir = arenaDir.CreateSubdirectory("containers");

            foreach (var subdir in containerHomeDir.GetDirectories())
            {
                subdir.Delete(true);
            }

            for (var i = 0; i < count; i++)
            {
                var containerDir = containerHomeDir.CreateSubdirectory(i.ToString());
                var id = await DockerService.RunArenaAsync(imageName, containerDir.FullName, cancellationToken);
                var containerInfo = new ContainerInfo
                {
                    Id = id,
                    FilesDirectory = containerDir
                };
                _containers.Add(containerInfo);
            }
        }

        public static ContainerInfo GetAFreeContainer()
        {
            var container = _containers.FirstOrDefault(x => x.IsAvailable) ?? GetAFreeContainer();
            container.FilesDirectory.Delete(true);
            container.FilesDirectory.Create();
            return container;
        }
    }
}
