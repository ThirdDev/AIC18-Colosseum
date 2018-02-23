using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.ScoringPolicies
{
    internal class DamagePolicyByTowerCount : IScoringPolicy
    {
        public double CalculateTotalScore(SimulationResult result)
        {
            var totalCount = result.ArcherTowersCount + result.CannonTowersCount;
            var _preferredMoneyToSpend = totalCount * 180 + Math.Max(0, (totalCount - 20) * 250);

            _preferredMoneyToSpend = Math.Max(500, _preferredMoneyToSpend);

            return (result.DamagesToEnemyBase > 0 ? (Math.Min(result.DamagesToEnemyBase, 50)) : 0) * 30.0 +
                   -Math.Pow(result.TotalPrice / (_preferredMoneyToSpend / 3.0), 4);
        }

        public int GetPreferredMoneyToSpend()
        {
            return -1;
        }
    }
}
