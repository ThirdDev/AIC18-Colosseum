using Colosseum.Experiment.GeneParsers;
using Colosseum.Experiment.ScoringPolicies;
using Colosseum.GS;
using System;
using System.Diagnostics;
using System.Linq;

namespace Colosseum.Experiment
{
    class Program
    {

        const int turns = 100;

        static void Main(string[] args)
        {
            var simulator = new Simulator(15, new int[] { 4, 4, 5, 6, 7, 8, 9 }, new int[] { 12, 2 });

            IScoringPolicy scoringPolicy;

            //scoringPolicy = new ExplorePolicy();
            scoringPolicy = new DamagePolicy();

            //Stopwatch stopwatch = new Stopwatch();
            //stopwatch.Start();
            //for (int i = 0; i < 1000; i++)
            //{
            //    simulator.Simulate(100, new MyGeneParser(null));
            //}
            //stopwatch.Stop();
            //Console.WriteLine(stopwatch.Elapsed);

            var gg = new GenerationGenerator();

            var generation = gg.randomGeneration();


            for (int i = 0; i < 1000; i++)
            {
                foreach (var gene in generation)
                {
                    var result = simulator.Simulate(turns, new MyGeneParser(gene));
                    gene.Score = scoringPolicy.CalculateTotalScore(result);
                }

                var sortedGeneration = generation.OrderBy(x => x.Score).ToList();

                Console.WriteLine($"Generation #{i + 1} finished. Statistics:");
                Console.WriteLine($"\tMin score = {sortedGeneration.First().Score}");
                Console.WriteLine($"\tAverage score = {generation.Average(x => x.Score)}");
                Console.WriteLine($"\tMax score = {sortedGeneration.Last().Score}");
                Console.WriteLine("\tBest gene: ");
                var bestGene = sortedGeneration.Last();
                int xx = 0;
                foreach (var item in bestGene.GenomesList)
                {
                    Console.Write(((int)Math.Max(7, item) - 7) + ", ");
                    xx++;
                    if (xx == 15)
                        Console.WriteLine();
                }
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
                    Console.WriteLine($"Total money spent: {result.TotalPrice}" );
                    Console.WriteLine($"Score: {scoringPolicy.CalculateTotalScore(result)}");
                    Console.WriteLine();
                    Console.ReadKey();
                }

                generation = gg.Genetic(generation);
            }


            Console.ReadLine();
        }
    }
}
