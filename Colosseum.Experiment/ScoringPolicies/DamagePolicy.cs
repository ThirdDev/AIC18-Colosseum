﻿using System;
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
            return (result.DamagesToEnemyBase > 0 ? (result.DamagesToEnemyBase) : 0) * 30.0 +
                -Math.Pow(result.TotalPrice / ((double)_preferredMoneyToSpend / 4), 4);
        }
    }
}
