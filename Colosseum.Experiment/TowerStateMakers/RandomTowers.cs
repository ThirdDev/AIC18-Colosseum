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
            var rnd = new Random();
            var towerStates = new List<TowerState>();

            for (var i = minCount; i < maxCount + 1; i++)
            {
                for (var j = 0; j < eachCountSamples; j++)
                {
                    var towerState = GetTowerState(pathLength, i, rnd);

                    towerStates.Add(towerState);
                }
            }


            return towerStates;
        }

        private static TowerState GetTowerState(int pathLength, int towersCount, Random rnd)
        {
            var archers = new List<int>();
            var cannons = new List<int>();

            for (var j = 0; j < towersCount; j++)
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
