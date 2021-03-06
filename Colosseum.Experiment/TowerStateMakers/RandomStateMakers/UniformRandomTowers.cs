﻿using System;
using System.Collections.Generic;

namespace Colosseum.Experiment.TowerStateMakers.RandomStateMakers
{
    internal class UniformRandomTowers : ITowerStateMaker
    {
        private readonly int _minCount;
        private readonly int _maxCount;
        private readonly int _countOfSamplesPerTower;

        internal UniformRandomTowers(int minCount, int maxCount, int countOfSamplesPerTower)
        {
            _minCount = minCount;
            _maxCount = maxCount;
            _countOfSamplesPerTower = countOfSamplesPerTower;
        }

        public List<TowerState> GetTowerStates(int pathLength)
        {
            var towerStates = new List<TowerState>();

            for (var towersCount = _minCount; towersCount < _maxCount + 1; towersCount++)
            {
                for (var archerTowerCount = 0; archerTowerCount <= towersCount; archerTowerCount++) 
                {
                    var cannonTowerCount = towersCount - archerTowerCount;

                    for (var i = 0; i < _countOfSamplesPerTower; i++)
                    {
                        towerStates.Add(randomTowerState(archerTowerCount, cannonTowerCount, pathLength));
                    }
                }
            }

            return towerStates;
        }

        protected static readonly Random _random = new Random();

        private TowerState randomTowerState(int archerTowerCount, int cannonTowerCount, int pathLength)
        {
            return new TowerState
            {
                Archers = randomTowerOrder(cannonTowerCount, pathLength),
                Cannons = randomTowerOrder(archerTowerCount, pathLength),
            };
        }

        protected virtual int randomTowerCount(int towersCount)
        {
            return _random.Next(towersCount);
        }

        protected virtual int[] randomTowerOrder(int count, int exclusiveMaxLocation)
        {
            var towers = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                towers.Add(_random.Next(exclusiveMaxLocation));
            }

            return towers.ToArray();
        }

        protected double gaussianRandom(double mean, double standarddeviation)
        {
            var u1 = 1.0 - _random.NextDouble(); //uniform(0,1] random doubles
            var u2 = 1.0 - _random.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            var randNormal =
                mean + standarddeviation * randStdNormal; //random normal(mean,stdDev^2)

            return randNormal;
        }
    }
}
