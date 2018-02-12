using Colosseum.Services;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Colosseum.App
{
    class Program
    {
        public static string DockerImageName => "aic-standalone";
        static async Task Main(string[] args)
        {
            var mapFile = new FileInfo("maps/map.map");
            await DockerService.BuildImageAsync(Directory.GetCurrentDirectory(), DockerImageName);
            await DockerService.StopAndRemoveAllContainersAsync();
            await ArenaManager.RunCompetitions(mapFile.FullName);
        }
    }
}
