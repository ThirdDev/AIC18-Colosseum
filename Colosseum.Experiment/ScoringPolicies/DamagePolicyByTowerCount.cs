using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.ScoringPolicies
{
    internal class DamagePolicyByTowerCount : IScoringPolicy
    {
        public double CalculateTotalScore(SimulationResult result)
        {
            var _preferredMoneyToSpend = (result.ArcherTowersCount + result.CannonTowersCount) * 100;

            return (result.DamagesToEnemyBase > 0 ? (Math.Min(result.DamagesToEnemyBase, 50)) : 0) * 30.0 +
                   -Math.Pow(result.TotalPrice / (_preferredMoneyToSpend / 3.0), 4);
        }

        public int GetPreferredMoneyToSpend()
        {
            return -1;
        }
    }
}
