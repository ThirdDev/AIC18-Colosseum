using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colosseum.Experiment.ScoringPolicies
{
    internal class DamagePolicy : IScoringPolicy
    {
        const int preferredMoneyToSpend = 600;

        public double CalculateTotalScore(SimulationResult result)
        {
            return (result.ReachedToTheEnd > 0 ? (result.ReachedToTheEnd) : 0) * 30.0 +
                -Math.Pow(result.TotalPrice / (preferredMoneyToSpend / 3), 3);
        }
    }
}
