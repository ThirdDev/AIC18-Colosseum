using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colosseum.Experiment.ScoringPolicies
{
    internal class DamagePolicy : IScoringPolicy
    {
        readonly int _preferredMoneyToSpend;

        public DamagePolicy(int preferredMoneyToSpend)
        {
            _preferredMoneyToSpend = preferredMoneyToSpend;
        }

        public double CalculateTotalScore(SimulationResult result)
        {
            //return (result.ReachedToTheEnd > 0 ? (result.ReachedToTheEnd) : 0) * 30.0 +
            //    -Math.Pow(result.TotalPrice / (_preferredMoneyToSpend / 3.0), 4);
            return (result.DamagesToEnemyBase > 0 ? (Math.Min(result.DamagesToEnemyBase, 50)) : 0) * 30.0 +
                -Math.Pow(result.TotalPrice / (_preferredMoneyToSpend / 3.0), 4);
        }

        public int GetPreferredMoneyToSpend()
        {
            return _preferredMoneyToSpend;
        }
    }
}
