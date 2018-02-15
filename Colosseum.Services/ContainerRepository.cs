using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Colosseum.Tools.SystemExtensions.IO;

namespace Colosseum.Services
{
    public class ContainerInfo
    {
        public string Id { get; set; }
        public DirectoryInfo FilesDirectory { get; set; }
        public bool IsAvailable => _semaphore.CurrentCount != 0;
        private SemaphoreSlim _semaphore => new SemaphoreSlim(1);
        public void Release() => _semaphore.Release();
        public Task WaitAsync(CancellationToken cancellationToken = default) => _semaphore.WaitAsync();
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

        private static readonly SemaphoreSlim _getAFreeDeviceSemaphoreSlim = new SemaphoreSlim(1);

        public static async Task<ContainerInfo> GetAFreeContainer(CancellationToken cancellationToken = default)
        {
            await _getAFreeDeviceSemaphoreSlim.WaitAsync(cancellationToken);
            try
            {
                while (true)
                {
                    var container = _containers.FirstOrDefault(x => x.IsAvailable);
                    if (container == null)
                    {
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }

                    await container.WaitAsync(cancellationToken);
                    container.FilesDirectory.DeleteForce();
                    container.FilesDirectory.Create();
                    return container;
                }
            }
            finally
            {
                _getAFreeDeviceSemaphoreSlim.Release();
            }
        }
    }
}
