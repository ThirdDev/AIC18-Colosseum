using System;
using System.Linq;

namespace Colosseum.Experiment
{
    public class SimulationResult
    {
        public int ReachedToTheEnd { get; set; }
        public int[] DeadPositions { get; set; }
        public int Length { get; set; }
        public int Turns { get; set; }
        public int TotalPrice { get; set; }

        public double CalculateDeadScore()
        {
            double avg = 0.0;
            int w = 0;
            for (int i = 0; i < Length; i++)
            {
                int count = DeadPositions.Count(x => x == i);
                avg += count * (double)i;
                w += count;
            }

            if (w == 0)
                return 0.0;

            avg /= (double)w;

            return avg;
        }

        public double CalculateTotalScore()
        {
            return (ReachedToTheEnd > 0 ? (1 + ReachedToTheEnd / 10.0) : 0) * 30.0 + 
                Math.Pow(CalculateDeadScore(), 2) / Length * 10.0 +
                -Math.Pow(TotalPrice / 3000.0, 3);
        }
    }
}