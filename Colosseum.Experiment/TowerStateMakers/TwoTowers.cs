using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colosseum.Experiment.TowerStateMakers
{
    class TwoTowers : ITowerStateMaker
    {
        public List<TowerState> GetTowerStates(int pathLength)
        {
            List<TowerState> states = new List<TowerState>();

            for (int i = 0; i < pathLength; i++)
            {
                for (int j = i; j < pathLength; j++)
                {
                    states.Add(new TowerState
                    {
                        Archers = new int[] { i, j },
                        Cannons = new int[] { },
                    });
                    states.Add(new TowerState
                    {
                        Archers = new int[] { },
                        Cannons = new int[] { i, j },
                    });
                    states.Add(new TowerState
                    {
                        Archers = new int[] { i },
                        Cannons = new int[] { j },
                    });
                    states.Add(new TowerState
                    {
                        Archers = new int[] { j },
                        Cannons = new int[] { i },
                    });
                }
            }

            return states;
        }
    }
}
