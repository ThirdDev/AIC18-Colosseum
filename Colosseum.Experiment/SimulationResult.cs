using System;
using System.Linq;

namespace Colosseum.Experiment
{
    public class SimulationResult
    {
        public int ReachedToTheEnd { get; set; }
        public int DamagesToEnemyBase { get; set; }
        public int[] DeadPositions { get; set; }
        public int Length { get; set; }
        public int Turns { get; set; }
        public int TotalPrice { get; set; }
    }
}