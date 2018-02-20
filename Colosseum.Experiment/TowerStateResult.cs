﻿using Colosseum.Experiment.TowerStateMakers;
using System.Collections.Generic;

namespace Colosseum.Experiment
{
    internal class TowerStateResult
    {
        public TowerState TowerState { get; set; }
        public List<GeneDetailedResult> Genes { get; } = new List<GeneDetailedResult>();
    }
}