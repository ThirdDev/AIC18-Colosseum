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

            var simulator = new Simulator(15, cannons, archers);

            IScoringPolicy scoringPolicy;

            //scoringPolicy = new ExplorePolicy();
            scoringPolicy = new DamagePolicy();
            
            var gg = new GenerationGenerator();

            var generation = gg.randomGeneration();

            Stopwatch st = new Stopwatch();
            st.Start();
            for (int i = 0; i < 100; i++)
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
                int xx = 0;
                foreach (var item in bestGene.GenomesList)
                {
                    Console.Write(MyGeneParser.GeneToTroopCount(item) + ", ");
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
