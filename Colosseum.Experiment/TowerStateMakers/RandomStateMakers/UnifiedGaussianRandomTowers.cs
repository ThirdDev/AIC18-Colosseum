using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.TowerStateMakers.RandomStateMakers
{
    internal class UnifiedGaussianRandomTowers : UniformRandomTowers
    {
        public UnifiedGaussianRandomTowers(int minCount, int maxCount, int countOfSamplesPerTowerCount) : base(minCount,
            maxCount, countOfSamplesPerTowerCount)
        { }

        protected override int randomTowerCount(int towersCount)
        {
            return Math.Abs((int)gaussianRandom(towersCount / 2.0, 1));
        }

        protected override int[] randomTowerOrder(int count, int exclusiveMaxLocation)
        {
            var towers = new List<int>(count);
            var gaussianTowerCount = (int)gaussianRandom(count / 2.0, 1.2);

            var margin = _random.Next(exclusiveMaxLocation) - (exclusiveMaxLocation / 2);

            for (var i = 0; i < gaussianTowerCount; i++)
            {
                var location = (int)gaussianRandom(exclusiveMaxLocation / 2.0, 1.2);
                location += margin;
                location = Math.Max(0, location);
                location = Math.Min(exclusiveMaxLocation - 1, location);
                towers.Add(location);
            }

            for (var i = 0; i < count - gaussianTowerCount; i++)
            {
                towers.Add(_random.Next(exclusiveMaxLocation));
            }

            return towers.ToArray();
        }
    }
}
