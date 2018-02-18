using Colosseum.Experiment.GeneParsers;
using Colosseum.Experiment.ScoringPolicies;
using Colosseum.GS;
using System;
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

            cannons = new int[] { 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6 };
            archers = new int[] { 7, 7, 8, 8, 9, 9, 10, 10, 11, 11 };

            //cannons = new int[] { 1, 1, 3, 3, 5, 5, 7, 7, 9, 9, 11, 11, 13, 13, 15, 15, 17, 17, 19, 19, 21, 21, 23, 23 };
            //archers = new int[] { 2, 2, 4, 4, 6, 6, 8, 8, 10, 10, 12, 12, 14, 14, 16, 16, 18, 18, 20, 20, 22, 22, 24, 24 };

            var length = 15;
            var preferredMoneyToSpend = 4000;
            var generationPopulation = 500;
            var lengthOfGene = length * 2;

            var simulator = new Simulator(length, cannons, archers);

            IScoringPolicy scoringPolicy;

            //scoringPolicy = new ExplorePolicy();
            scoringPolicy = new DamagePolicy(preferredMoneyToSpend);

            var gg = new GenerationGenerator(generationPopulation, lengthOfGene);

            var generation = gg.RandomGeneration();

            var st = new Stopwatch();
            st.Start();
            for (var i = 0; i < 100; i++)
            {
                foreach (var gene in generation)
                {
                    var result = simulator.Simulate(turns, new MyGeneParser(gene));
                    gene.Score = scoringPolicy.CalculateTotalScore(result);
                }

                /**/
                var sortedGeneration = generation.OrderBy(x => x.Score).ToList();

                Console.WriteLine($"Generation #{i + 1} finished. Statistics:");
                Console.WriteLine($"\tMin score = {sortedGeneration.First().Score}");
                Console.WriteLine($"\tAverage score = {generation.Average(x => x.Score)}");
                Console.WriteLine($"\tMax score = {sortedGeneration.Last().Score}");
                Console.WriteLine("\tBest gene: ");
                var bestGene = sortedGeneration.Last();

                var creepGeneString = string.Join(", ", bestGene.GenomesList.GetRange(0, length).Select(MyGeneParser.GeneToTroopCount));
                var heroGeneString = string.Join(", ", bestGene.GenomesList.GetRange(length, length).Select(MyGeneParser.GeneToTroopCount));
                Console.WriteLine(creepGeneString);
                Console.WriteLine(heroGeneString);

                Console.WriteLine();
                Console.WriteLine();

                if (Console.KeyAvailable)
                {
                    Console.ReadKey();
                    Console.ReadKey();
                    var result = simulator.Simulate(turns, new MyGeneParser(bestGene), print: true);
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Results: ");
                    Console.WriteLine($"Damages to base: {result.ReachedToTheEnd}");
                    Console.WriteLine($"Total money spent: {result.TotalPrice}");
                    Console.WriteLine($"Score: {scoringPolicy.CalculateTotalScore(result)}");
                    Console.WriteLine($"Turns: {result.Turns}");
                    Console.WriteLine();
                    Console.ReadKey();
                }
                /**/
                generation = gg.Genetic(generation);
            }
            st.Stop();
            Console.WriteLine(st.Elapsed);


            Console.ReadLine();
        }
    }
}
