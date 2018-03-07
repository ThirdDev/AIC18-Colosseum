using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colosseum.Experiment.TowerStateMakers
{
    class TwoDoubleTowers : ITowerStateMaker
    {
        const int maximumCount = 3;

        public List<TowerState> GetTowerStates(int pathLength)
        {
            var states = new List<TowerState>();

            for (var i = 0; i < pathLength; i++)
            {
                for (var j = i + 1; j < pathLength; j++)
                {
                    states.Add(new TowerState
                    {
                        Archers = new int[] { i, i, j, j },
                        Cannons = new int[] { },
                    });
                    states.Add(new TowerState
                    {
                        Archers = new int[] { },
                        Cannons = new int[] { i, i, j, j },
                    });
                    states.Add(new TowerState
                    {
                        Archers = new int[] { i, i },
                        Cannons = new int[] { j, j },
                    });
                    states.Add(new TowerState
                    {
                        Archers = new int[] { j, j },
                        Cannons = new int[] { i, i },
                    });
                }
            }

            return states;
        }
    }
}
