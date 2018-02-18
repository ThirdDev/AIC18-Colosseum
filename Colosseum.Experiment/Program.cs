using Colosseum.Experiment.GeneParsers;
using Colosseum.Experiment.ScoringPolicies;
using Colosseum.GS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Colosseum.Experiment
{
    class Program
    {

        const int turns = 1000;

        [SuppressMessage("ReSharper", "JoinDeclarationAndInitializer")]
        public static void Main()
        {
            int[] cannons, archers;

            //cannons = new int[] { 1, 3, 5, 7, 9, 11, 13};
            //archers = new int[] { 1, 3, 5, 7, 9, 11, 13};

            //cannons = new int[] { };
            //archers = new int[] { 3, 5, 7, 9, 12 };

            //cannons = new int[] { 2, 2, 3, 3, 4, 4 };
            //archers = new int[] { };

            //cannons = new int[] { 2, 2 };
            //archers = new int[] { 5, 7, 9 };

            //cannons = new int[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6 };
            //archers = new int[] { 7, 7, 8, 8, 9, 9, 10, 10, 11, 11 };

            //cannons = new int[] { 1, 1, 3, 3, 5, 5, 7, 7, 9, 9, 11, 11, 13, 13, 15, 15, 17, 17, 19, 19, 21, 21, 23, 23 };
            //archers = new int[] { 2, 2, 4, 4, 6, 6, 8, 8, 10, 10, 12, 12, 14, 14, 16, 16, 18, 18, 20, 20, 22, 22, 24, 24 };



            var length = 15;
            var preferredMoneyToSpend = 4000;
            var generationPopulation = 500;
            var lengthOfGene = length * 2;
            var maximumCountOfGenerations = 1000;

            var towerCount = 10;
            var archerTowerCount = (int)gaussianRandom(towerCount / 2.0, 1);
            var canonTowerCount = towerCount - archerTowerCount;
            preferredMoneyToSpend = archerTowerCount * 120 + canonTowerCount * 120;

            cannons = randomTowerOrder(canonTowerCount, length);
            archers = randomTowerOrder(archerTowerCount, length);

            var archerString = towerLocationsToString(archers, length, "a");
            var cannonString = towerLocationsToString(cannons, length, "c");

            var simulator = new Simulator(length, cannons, archers);

            IScoringPolicy scoringPolicy;

            //scoringPolicy = new ExplorePolicy();
            scoringPolicy = new DamagePolicy(preferredMoneyToSpend);

            var gg = new GenerationGenerator(generationPopulation, lengthOfGene);

            var generation = gg.RandomGeneration();

            var st = new Stopwatch();
            st.Start();
            for (var i = 0; i < maximumCountOfGenerations; i++)
            {
                foreach (var gene in generation)
                {
                    var result = simulator.Simulate(turns, new MyGeneParser(gene));
                    gene.Score = scoringPolicy.CalculateTotalScore(result);
                }

                /**/
                var sortedGeneration = generation.OrderBy(x => x.Score).ToList();
                var bestGene = sortedGeneration.Last();

                logGenerationStatistics(length, generation, i, sortedGeneration, bestGene, archerString, cannonString);

                if (Console.KeyAvailable)
                {
                    Console.ReadKey();
                    Console.ReadKey();
                    logGeneSimulationResult(simulator, bestGene, scoringPolicy, preferredMoneyToSpend);
                    Console.ReadKey();
                }
                /**/
                if (i != maximumCountOfGenerations - 1)
                {
                    generation = gg.Genetic(generation);
                }
                else
                {
                    logGeneSimulationResult(simulator, bestGene, scoringPolicy, preferredMoneyToSpend);
                }
            }
            st.Stop();
            Console.WriteLine(st.Elapsed);


            Console.ReadLine();
        }

        private static string towerLocationsToString(int[] towerLocations, int length, string identifier = "x")
        {
            var positions = Enumerable.Repeat(0, length).ToArray();
            foreach (var t in towerLocations)
            {
                positions[t]++;
            }
            return string.Join(", ", positions.Select(x => (x == 0) ? "  " : $"{identifier}{x}"));
        }

        private static void logGeneSimulationResult(Simulator simulator, Gene bestGene, IScoringPolicy scoringPolicy, int preferredMoneyToSpend)
        {
            var result = simulator.Simulate(turns, new MyGeneParser(bestGene), print: true);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Results: ");
            Console.WriteLine($"Damages to base: {result.ReachedToTheEnd}");
            Console.WriteLine($"Total money spent: {result.TotalPrice} with {preferredMoneyToSpend} prefered");
            Console.WriteLine($"Score: {scoringPolicy.CalculateTotalScore(result)}");
            Console.WriteLine($"Turns: {result.Turns}");
            Console.WriteLine();
        }

        private static void logGenerationStatistics(int length, List<Gene> generation, int generationNumber,
            List<Gene> sortedGeneration, Gene bestGene, string archersString, string cannonsString)
        {
            Console.WriteLine($"Generation #{generationNumber + 1} finished. Statistics:");
            Console.WriteLine($"\tMin score = {sortedGeneration.First().Score}");
            Console.WriteLine($"\tAverage score = {generation.Average(x => x.Score)}");
            Console.WriteLine($"\tMax score = {sortedGeneration.Last().Score}");
            Console.WriteLine("\tBest gene: ");

            Console.WriteLine($"archers: {archersString}");
            Console.WriteLine($"cannons: {cannonsString}");
            var creepGeneString = string.Join(", ", bestGene.GenomesList.GetRange(0, length).Select(MyGeneParser.GeneToTroopCount));
            var heroGeneString = string.Join(", ", bestGene.GenomesList.GetRange(length, length).Select(MyGeneParser.GeneToTroopCount));

            Console.WriteLine($"creeps: {creepGeneString}");
            Console.WriteLine($"heros:  {heroGeneString}");

            Console.WriteLine();
            Console.WriteLine();
        }

        private static readonly Random _random = new Random();

        private static int[] randomTowerOrder(int count, int exclusiveMaxLocation)
        {
            var towers = new List<int>();
            for (var i = 0; i < count; i++)
            {
                towers.Add(_random.Next(exclusiveMaxLocation));
            }

            return towers.ToArray();
        }

        private static double gaussianRandom(double mean, double standarddeviation)
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
