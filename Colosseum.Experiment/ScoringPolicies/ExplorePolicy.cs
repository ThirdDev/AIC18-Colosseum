using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colosseum.Experiment.ScoringPolicies
{
    internal class ExplorePolicy : IScoringPolicy
    {
        private double CalculateDeadScore(SimulationResult result)
        {
            double avg = 0.0;
            int w = 0;
            for (int i = 0; i < result.Length; i++)
            {
                int count = result.DeadPositions.Count(x => x == i);
                avg += count * (double)i;
                w += count;
            }

            if (w == 0)
                return 0.0;

            avg /= (double)w;

            return avg;
        }

        public double CalculateTotalScore(SimulationResult result)
        {
            return (result.ReachedToTheEnd > 0 ? (1 + result.ReachedToTheEnd / 20.0) : 0) * 30.0 +
                CalculateDeadScore(result) / result.Length * 10.0 +
                -Math.Pow(result.TotalPrice / 300.0, 3);
        }
    }
}
