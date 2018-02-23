using System;
using System.Collections.Generic;
using System.Text;

namespace Colosseum.Experiment.TowerStateMakers.RandomStateMakers
{
    internal class UnifiedGaussianRandomTowers : UniformRandomTowers
    {
        public UnifiedGaussianRandomTowers(int minCount, int maxCount, int countOfSamplesPerTowerCoefficient) : base(minCount,
            maxCount, countOfSamplesPerTowerCoefficient)
        { }

        protected override int randomTowerCount(int towersCount)
        {
            return randomGaussianNumber(towersCount + 1);
        }

        protected override int[] randomTowerOrder(int count, int exclusiveMaxLocation)
        {
            var towers = new List<int>(count);
            var gaussianTowerCount = randomGaussianNumber(count + 1);

            var margin = _random.Next(exclusiveMaxLocation) - (exclusiveMaxLocation / 2);

            for (var i = 0; i < gaussianTowerCount; i++)
            {
                var location = randomGaussianNumber(exclusiveMaxLocation);
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

        private int randomGaussianNumber(int exclusiveMax)
        {
            var num = (int)gaussianRandom(exclusiveMax / 2.0, 1);
            num = Math.Max(0, num);
            num = Math.Min(exclusiveMax - 1, num);
            return num;
        }
    }
}
