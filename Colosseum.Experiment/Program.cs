using Colosseum.Experiment.GeneParsers;
using Colosseum.GS;
using System;
using System.Diagnostics;
using System.Linq;

namespace Colosseum.Experiment
{
    class Program
    {

        const int turns = 50;

        static void Main(string[] args)
        {
            var simulator = new Simulator(15, new int[] { 4, 4, 5, 6, 7, 8, 9 }, new int[] { 12, 2 });

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
                    gene.Score = result.CalculateTotalScore();
                }

                Console.WriteLine($"Generation #{i + 1} finished. Statistics:");
                Console.WriteLine($"\tMin score = {generation.Min(x => x.Score)}");
                Console.WriteLine($"\tAverage score = {generation.Average(x => x.Score)}");
                Console.WriteLine($"\tMax score = {generation.Max(x => x.Score)}");
                Console.WriteLine("\tBest gene: ");
                var bestGene = generation.OrderBy(x => x.Score).Last();
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
                    simulator.Simulate(turns, new MyGeneParser(bestGene), print: true);
                    Console.ReadKey();
                }



                generation = gg.ChildrenMaker(generation, generation.Count);
            }


            Console.ReadLine();
        }
    }
}
