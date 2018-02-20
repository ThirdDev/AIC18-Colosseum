using Colosseum.Experiment.GeneParsers;
using Colosseum.Experiment.ScoringPolicies;
using Colosseum.Experiment.TowerStateMakers;
using Colosseum.GS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using Colosseum.Tools.SystemExtensions.Collection.Generic;

namespace Colosseum.Experiment
{
    class Program
    {

        const int turns = 1000;

        [SuppressMessage("ReSharper", "JoinDeclarationAndInitializer")]
        public static async Task Main()
        {
            /**
            SolutionMaker solutionMaker = new SolutionMaker(new ThreeTowers(), new DamagePolicy(600));
            solutionMaker.Make(20);
            return;
            /**/

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

            //cannons = new int[] { 3, 9 };
            //archers = new int[] { 6, 9 };



            var length = 15;
            var preferredMoneyToSpend = 400;
            var generationPopulation = 100;
            var lengthOfGene = length * 2;
            var maximumCountOfGenerations = 1000;
            var geneToTroopMean = 0;

            //var towerCount = 20;
            //var archerTowerCount = (int)gaussianRandom(towerCount / 2.0, 1);
            //var canonTowerCount = towerCount - archerTowerCount;
            //preferredMoneyToSpend = archerTowerCount * 100 + canonTowerCount * 1000;

            //cannons = randomTowerOrder(canonTowerCount, length);
            //archers = randomTowerOrder(archerTowerCount, length);

            preferredMoneyToSpend = 1200;
            length = 20;

            IScoringPolicy scoringPolicy;

            //scoringPolicy = new ExplorePolicy(preferredMoneyToSpend);
            scoringPolicy = new DamagePolicy(preferredMoneyToSpend);

            var towerStates = JsonConvert.DeserializeObject<List<TowerStateResult>>(
                await File.ReadAllTextAsync(
                    @"C:\Projects\AIC2018\BuildingBlocks\output\ThreeTowers-DamagePolicy 600-pathLength 20-2018-20-2--20-38-47.json"));
            var firstState = towerStates.RandomElement();
            var secondState = towerStates.RandomElement();

            cannons = firstState.TowerState.Cannons.Concat(secondState.TowerState.Cannons).ToArray();
            archers = firstState.TowerState.Archers.Concat(secondState.TowerState.Archers).ToArray();

            var archersString = towerLocationsToString(archers, length, "a");
            var cannonsString = towerLocationsToString(cannons, length, "c");
            Console.WriteLine($"archers: {archersString}");
            Console.WriteLine($"cannons: {cannonsString}");

            Console.WriteLine();

            var firstGene = combineGenes(length, firstState.Genes.Select(x => x.Gene).ToArray());
            var secondGenes = combineGenes(length, secondState.Genes.Select(x => x.Gene).ToArray());

            var gene = combineGenes(length, firstGene, secondGenes);

            var testGenesCount = 5;

            var simulator = new Simulator(length, turns, cannons, archers);

            Console.WriteLine($"finding {testGenesCount} best genes for these towers");
            Console.WriteLine();

            var bestGenes = new List<Gene>
            {
                gene
            };

            for (var i = 0; i < testGenesCount; i++)
            {
                var testGene = findBestGeneForTowerPattern(cannons, archers, length, preferredMoneyToSpend,
                    generationPopulation, maximumCountOfGenerations, geneToTroopMean, scoringPolicy, printEvaluationLog: false);

                bestGenes.Add(testGene);
                Console.WriteLine($"gene number {i + 1}");
                logGeneSimulationResultWithGeneAndTowerInfo(testGene, simulator, length, scoringPolicy, preferredMoneyToSpend, geneToTroopMean, archersString, cannonsString, false);
                Console.WriteLine("=======================================================================================");
            }

            Console.WriteLine("testing combined gene");
            Console.WriteLine();

            logGeneSimulationResultWithGeneAndTowerInfo(gene, simulator, length, scoringPolicy, preferredMoneyToSpend, geneToTroopMean, archersString, cannonsString, false);

            Console.WriteLine();
            Console.WriteLine("enter the gene number to see game play or 0 the see the combined gene's play");

            var command = Console.ReadLine();

            while (int.TryParse(command, out int number))
            {
                logGeneSimulationResultWithGeneAndTowerInfo(bestGenes[number], simulator, length, scoringPolicy, preferredMoneyToSpend, geneToTroopMean, archersString, cannonsString, true);

                command = Console.ReadLine();
            }

        }

        private static void logGeneSimulationResultWithGeneAndTowerInfo(Gene gene, Simulator simulator, int length,
            IScoringPolicy scoringPolicy, int preferredMoneyToSpend, double geneToTroopMean, string archersString,
            string cannonsString, bool logGame)
        {
            Console.WriteLine($"archers: {archersString}");
            Console.WriteLine($"cannons: {cannonsString}");
            var creepGeneString = string.Join(", ", gene.GenomesList.GetRange(0, length).Select(x => MyGeneParser.GeneToTroopCount(x)));
            var heroGeneString = string.Join(", ", gene.GenomesList.GetRange(length, length).Select(x => MyGeneParser.GeneToTroopCount(x)));

            Console.WriteLine($"creeps: {creepGeneString}");
            Console.WriteLine($"heros:  {heroGeneString}");

            logGeneSimulationResult(simulator, gene, scoringPolicy, preferredMoneyToSpend, archersString, cannonsString, geneToTroopMean, length, logGame);
        }

        private static Gene findBestGeneForTowerPattern(
            int[] cannons,
            int[] archers,
            int length,
            int preferredMoneyToSpend,
            int generationPopulation,
            int maximumCountOfGenerations,
            int geneToTroopMean,
            IScoringPolicy scoringPolicy,
            bool printEvaluationLog)
        {
            Gene bestGene = null;

            var simulator = new Simulator(length, turns, cannons, archers);

            var lengthOfGene = length * 2;

            var gg = new GenerationGenerator(generationPopulation, lengthOfGene);

            var generation = gg.RandomGeneration();


            var archerString = towerLocationsToString(archers, length, "a");
            var cannonString = towerLocationsToString(cannons, length, "c");

            double lastBestScore = default;
            int bestScoreCount = 0;
            int bestScoreCountLimit = 30;

            var st = new Stopwatch();
            st.Start();
            for (var i = 0; i < maximumCountOfGenerations; i++)
            {
                foreach (var gene in generation)
                {
                    var result = simulator.Simulate(new MyGeneParser(gene, length));
                    gene.Score = scoringPolicy.CalculateTotalScore(result);
                }

                bestGene = Program.bestGene(generation);

                if (printEvaluationLog)
                {
                    var sortedGeneration = generation.OrderBy(x => x.Score).ToList();

                    logGenerationStatistics(length, generation, i, sortedGeneration, bestGene, archerString, cannonString, geneToTroopMean);

                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey();
                        Console.ReadKey();
                        logGeneSimulationResult(simulator, bestGene, scoringPolicy, preferredMoneyToSpend, archerString, cannonString, geneToTroopMean, length, true);
                        Console.ReadKey();
                    }
                }

                if (bestGene.Score != null)
                {
                    if (i == 0)
                    {
                        lastBestScore = bestGene.Score.Value;
                        bestScoreCount++;
                    }
                    else
                    {
                        if (bestGene.Score.Value.Equals(lastBestScore))
                        {
                            bestScoreCount++;
                        }
                        else
                        {
                            lastBestScore = bestGene.Score.Value;
                            bestScoreCount = 1;
                        }
                    }
                }

                if (bestScoreCount >= bestScoreCountLimit)
                {
                    if (printEvaluationLog)
                    {
                        logGeneSimulationResult(simulator, bestGene, scoringPolicy, preferredMoneyToSpend, archerString, cannonString, geneToTroopMean, length, true);
                    }
                    break;
                }

                if (i != maximumCountOfGenerations - 1)
                {
                    generation = gg.Genetic(generation);
                }
                else
                {
                    if (printEvaluationLog)
                    {
                        logGeneSimulationResult(simulator, bestGene, scoringPolicy, preferredMoneyToSpend, archerString, cannonString, geneToTroopMean, length, true);
                    }
                }
            }
            st.Stop();
            if (printEvaluationLog)
            {
                Console.WriteLine(st.Elapsed);
            }

            return bestGene;
        }

        private static Gene combineGenes(int length, params Gene[] genes)
        {
            var finalGene = new Gene();
            finalGene.GenomesList.AddRange(Enumerable.Repeat(0.0, length * 2));
            foreach (var gene in genes)
            {
                for (var i = 0; i < gene.GenomesList.Count; i++)
                {
                    finalGene.GenomesList[i] = Math.Max(finalGene.GenomesList[i], gene.GenomesList[i]);
                }
            }

            return finalGene;
        }

        private static Gene bestGene(List<Gene> generation)
        {
            return generation.OrderBy(x => x.Score).Last();
        }

        private static string towerLocationsToString(int[] towerLocations, int length, string identifier = "x")
        {
            var positions = Enumerable.Repeat(0, length).ToArray();
            foreach (var t in towerLocations)
            {
                positions[t]++;
            }
            return string.Join(", ", positions.Select(x => ((x == 0) ? "" : $"{x}{identifier}").PadLeft(3)));
        }

        private static void logGeneSimulationResult(Simulator simulator, Gene bestGene, IScoringPolicy scoringPolicy,
            int preferredMoneyToSpend, string archersString, string cannonsString, double geneToTroopMean, int length,
            bool logGame)
        {
            var result = simulator.Simulate(new MyGeneParser(bestGene, length), logGame, archersString, cannonsString);
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Results: ");
            Console.WriteLine($"Damages to base: {result.DamagesToEnemyBase}");
            Console.WriteLine($"Total money spent: {result.TotalPrice} with {preferredMoneyToSpend} prefered");
            Console.WriteLine($"Score: {scoringPolicy.CalculateTotalScore(result)}");
            Console.WriteLine($"Turns: {result.Turns}");
            Console.WriteLine();
        }

        private static void logGenerationStatistics(int length, List<Gene> generation, int generationNumber,
            List<Gene> sortedGeneration, Gene bestGene, string archersString, string cannonsString, double geneToTroopMean)
        {
            Console.WriteLine($"Generation #{generationNumber + 1} finished. Statistics:");
            Console.WriteLine($"\tMin score = {sortedGeneration.First().Score}");
            Console.WriteLine($"\tAverage score = {generation.Average(x => x.Score)}");
            Console.WriteLine($"\tMax score = {sortedGeneration.Last().Score}");
            Console.WriteLine("\tBest gene: ");

            Console.WriteLine($"archers: {archersString}");
            Console.WriteLine($"cannons: {cannonsString}");
            var creepGeneString = string.Join(", ", bestGene.GenomesList.GetRange(0, length).Select(x => MyGeneParser.GeneToTroopCount(x)));
            var heroGeneString = string.Join(", ", bestGene.GenomesList.GetRange(length, length).Select(x => MyGeneParser.GeneToTroopCount(x)));

            Console.WriteLine($"creeps: {creepGeneString}");
            Console.WriteLine($"heros:  {heroGeneString}");

            Console.WriteLine();
            Console.WriteLine();
        }

        private static readonly Random _random = new Random();

        private static int[] randomTowerOrder(int count, int exclusiveMaxLocation)
        {
            var towers = new List<int>(count);
            var gaussianTowerCount = (int)gaussianRandom(count / 2.0, 1.2);
            for (var i = 0; i < gaussianTowerCount; i++)
            {
                towers.Add((int)gaussianRandom(exclusiveMaxLocation / 2.0, 1.2));
            }

            for (var i = 0; i < count - gaussianTowerCount; i++)
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
