using System;
using System.IO;
using System.Threading.Tasks;

namespace Colosseum.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var mapFile = new FileInfo("maps/map.map");
            await ArenaManager.RunCompetitions(mapFile.FullName);
        }
    }
}
