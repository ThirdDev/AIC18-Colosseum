using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Colosseum.Experiment.TowerStateMakers
{
    class SingleTower : ITowerStateMaker
    {
        const int maximumCount = 3;

        public List<TowerState> GetTowerStates(int pathLength)
        {
            List<TowerState> states = new List<TowerState>();

            for (int i = 0; i < pathLength; i++)
            {
                for (int j = 0; j < maximumCount; j++)
                {
                    states.Add(GetCannonTower(i, j + 1));
                    states.Add(GetArcherTower(i, j + 1));
                }
            }

            return states;
        }

        private TowerState GetCannonTower(int position, int count)
        {
            return new TowerState
            {
                Archers = new int[] { },
                Cannons = Enumerable.Repeat(position, count).ToArray(),
            };
        }

        private TowerState GetArcherTower(int position, int count)
        {
            return new TowerState
            {
                Archers = Enumerable.Repeat(position, count).ToArray(),
                Cannons = new int[] { },
            };
        }
    }
}
