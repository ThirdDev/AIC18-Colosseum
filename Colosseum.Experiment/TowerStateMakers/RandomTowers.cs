using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.TowerStateMakers
{
    class RandomTowers : ITowerStateMaker
    {
        int minCount, maxCount, eachCountSamples;

        public RandomTowers(int minCount, int maxCount, int eachCountSamples)
        {
            this.minCount = minCount;
            this.maxCount = maxCount;
            this.eachCountSamples = eachCountSamples;
        }

        public List<TowerState> GetTowerStates(int pathLength)
        {
            Random rnd = new Random();
            List<TowerState> towerStates = new List<TowerState>();

            for (int i = minCount; i < maxCount + 1; i++)
            {
                for (int j = 0; j < eachCountSamples; j++)
                {
                    TowerState towerState = GetTowerState(pathLength, i, rnd);

                    towerStates.Add(towerState);
                }
            }


            return towerStates;
        }

        private static TowerState GetTowerState(int pathLength, int towersCount, Random rnd)
        {
            List<int> archers = new List<int>();
            List<int> cannons = new List<int>();

            for (int j = 0; j < towersCount; j++)
            {
                if (rnd.NextDouble() < 0.5)
                {
                    archers.Add(rnd.Next(pathLength));
                }
                else
                {
                    cannons.Add(rnd.Next(pathLength));
                }
            }

            var towerState = new TowerState
            {
                Archers = archers.ToArray(),
                Cannons = cannons.ToArray(),
            };
            return towerState;
        }
    }
}
