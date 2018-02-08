using System;
using System.IO;
using System.Threading.Tasks;

namespace Colosseum.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var mapFile = new FileInfo("map.map");
            await ArenaManager.RunCompetitions(mapFile.FullName);
        }
    }
}
