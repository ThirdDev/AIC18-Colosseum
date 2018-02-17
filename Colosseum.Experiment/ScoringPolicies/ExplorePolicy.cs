using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colosseum.Experiment.ScoringPolicies
{
    internal class ExplorePolicy : IScoringPolicy
    {
        const int preferredMoneyToSpend = 600;

        private double CalculateDeadScore(SimulationResult result)
        {
            double avg = 0.0;
            int w = 0;
            for (int i = 0; i < result.Length; i++)
            {
                int count = result.DeadPositions.Count(x => x == i);
                int c2 = Math.Min(count, 3); // Don't fool me!
                avg += c2 * (double)i; 
                w += c2;
            }

            if (w == 0)
                return 0.0;

            avg /= (double)w;

            return avg;
        }

        public double CalculateTotalScore(SimulationResult result)
        {
            return (result.ReachedToTheEnd > 0 ? (1 + result.ReachedToTheEnd / 20.0) : 0) * 10.0 +
                Math.Pow(CalculateDeadScore(result), 1.1) * 10.0 +
                -Math.Pow(result.TotalPrice / (preferredMoneyToSpend / 3), 3);
        }
    }
}
