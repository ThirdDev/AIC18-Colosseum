using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.TowerStateMakers
{
    public interface ITowerStateMaker
    {
        List<TowerState> GetTowerStates(int pathLength);
    }
}
