using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.TowerStateMakers
{
    class RandomTowers : ITowerStateMaker
    {
        int minCount, maxCount, totalSamples;

        public RandomTowers(int minCount, int maxCount, int totalSamples)
        {
            this.minCount = minCount;
            this.maxCount = maxCount;
            this.totalSamples = totalSamples;
        }

        public List<TowerState> GetTowerStates(int pathLength)
        {
            Random rnd = new Random();
            List<TowerState> towerStates = new List<TowerState>();

            for (int i = 0; i < totalSamples; i++)
            {
                var towersCount = rnd.Next(minCount, maxCount + 1);

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

                towerStates.Add(towerState);
            }

            return towerStates;
        }
    }
}
