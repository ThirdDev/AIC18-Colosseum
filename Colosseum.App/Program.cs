using System;
using System.Threading.Tasks;

namespace Colosseum.App
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var mapPath = "map.map";
            await ArenaManager.RunCompetitions(mapPath);
        }
    }
}
