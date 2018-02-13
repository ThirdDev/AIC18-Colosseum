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
            // if you set useSwarm to true, the swarm will be used regardless of useContainer value
            await ArenaManager.RunCompetitions(mapFile.FullName, useContainer: true, useSwarm: true);
        }
    }
}
