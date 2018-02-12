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
            await ArenaManager.RunCompetitions(mapFile.FullName, useContainer: false);
        }
    }
}
