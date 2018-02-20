using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colosseum.Experiment.ScoringPolicies
{
    internal class ExplorePolicy : IScoringPolicy
    {
        readonly int _preferredMoneyToSpend;

        public ExplorePolicy(int preferredMoneyToSpend)
        {
            _preferredMoneyToSpend = preferredMoneyToSpend;
        }

        private double CalculateDeadScore(SimulationResult result)
        {
            var avg = 0.0;
            var w = 0;
            for (var i = 0; i < result.Length; i++)
            {
                var count = result.DeadPositions.Count(x => x == i);
                var c2 = Math.Min(count, 3); // Don't fool me!
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
            return (result.ReachedToTheEnd > 0 ? 1 : 0) * 100.0 +
                Math.Pow(CalculateDeadScore(result), 1.1) * 10.0 +
                -Math.Pow(result.TotalPrice / (_preferredMoneyToSpend / 3.0), 4);
        }

        public int GetPreferredMoneyToSpend()
        {
            return _preferredMoneyToSpend;
        }
    }
}
