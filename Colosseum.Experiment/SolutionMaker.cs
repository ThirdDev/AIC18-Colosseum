﻿using Colosseum.Experiment.GeneParsers;
using Colosseum.Experiment.ScoringPolicies;
using Colosseum.Experiment.TowerStateMakers;
using Colosseum.GS;
using Colosseum.Tools.SystemExtensions.Collection.Generic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Colosseum.Experiment
{
    public class SolutionMaker
    {
        const int generationCount = 100;
        const int countOfBestGenesToSave = 1;
        const int maximumGenerations = 500;

        private readonly ITowerStateMaker towerStateMaker;
        private readonly IScoringPolicy scoringPolicy;

        public SolutionMaker(ITowerStateMaker towerStateMaker, IScoringPolicy scoringPolicy)
        {
            this.towerStateMaker = towerStateMaker;
            this.scoringPolicy = scoringPolicy;
        }

        public void Make(int pathLength, int geneLength, int maximumTurns)
        {
            var outputFile = $"{towerStateMaker.GetType().Name}-{scoringPolicy.GetType().Name} {scoringPolicy.GetPreferredMoneyToSpend()}-pathLength {pathLength}-{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}.json";
            var outputDirectory = "output";

            var towerStates = towerStateMaker.GetTowerStates(pathLength);

            var results = new List<TowerStateResult>();
            var resultsLock = new Object();

            Console.WriteLine($"{towerStateMaker.GetType().Name}\r\n{scoringPolicy.GetType().Name} with preferred amount of {scoringPolicy.GetPreferredMoneyToSpend()}\r\npathLength: {pathLength}");
            Console.WriteLine();

            Console.WriteLine($"Generated {towerStates.Count} states.");
            Console.WriteLine();


            var reportPeriod = TimeSpan.FromSeconds(2);
            var beginTime = DateTime.Now;
            var lastSaveTime = DateTime.Now;
            var progress = 0;

            using (new Timer(
                _ => WriteStatus2(progress + 1, towerStates.Count, DateTime.Now - beginTime),
                null, reportPeriod, reportPeriod))
            {
                Parallel.For(0, towerStates.Count, new ParallelOptions { MaxDegreeOfParallelism = 1500 }, (long i) =>
                {
                    var bestGenes = new List<List<Gene>>();
                    Parallel.For(0, countOfBestGenesToSave, (j) =>
                    {
                        bestGenes.AddThreadSafe(FindBestGenes(towerStates[(int)i], pathLength, geneLength, maximumTurns).OrderByDescending(x => x.Score).ToList());
                    });

                    var result = GetTowerStateResult(towerStates[(int)i], bestGenes, pathLength, maximumTurns);
                    lock (resultsLock)
                    {
                        results.Add(result);

                        lastSaveTime = SaveIfNecessary(results, lastSaveTime, outputDirectory, outputFile);
                    }

                    Interlocked.Increment(ref progress);
                });
            }

            Console.Write($"\r{towerStates.Count} / {towerStates.Count}                                                                         ");

            Console.WriteLine();
            Console.WriteLine($"Writing output file '{outputFile}'...");

            var outputString = JsonConvert.SerializeObject(results, Formatting.Indented);

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(Path.Combine(outputDirectory, outputFile), outputString);

            Console.WriteLine("Done.");
            Console.ReadKey();
        }

        private DateTime SaveIfNecessary(List<TowerStateResult> results, DateTime lastSaveTime, string outputDirectory, string outputFile)
        {
            if ((DateTime.Now - lastSaveTime) > TimeSpan.FromMinutes(10))
            {
                var outputString = JsonConvert.SerializeObject(results, Formatting.Indented);

                if (!Directory.Exists(outputDirectory))
                    Directory.CreateDirectory(outputDirectory);
                File.WriteAllText(Path.Combine(outputDirectory, outputFile), outputString);

                return DateTime.Now;
            }

            return lastSaveTime;
        }


        int prevProg = 0;
        private void WriteStatus(int progress, int totalCount, TimeSpan period)
        {
            var spm = 60.0 * (progress - prevProg) / period.TotalSeconds;

            Console.Write($"\r{progress} / {totalCount} - SPM: {spm.ToString("F1")}");
            prevProg = progress;
        }

        private void WriteStatus2(int progress, int totalCount, TimeSpan elapsed)
        {
            var spm = 60.0 * (progress) / elapsed.TotalSeconds;

            Console.Write($"\r{progress} / {totalCount} - SPM: {spm.ToString("F1")}");
            prevProg = progress;
        }

        private TowerStateResult GetTowerStateResult(TowerState towerState, List<List<Gene>> bestGenes, int pathLength, int maximumTurns)
        {
            var result = new TowerStateResult
            {
                TowerState = towerState,
            };

            foreach (var item in bestGenes)
            {
                result.Genes.Add(GetGeneResult(towerState, item[0], pathLength, maximumTurns));
            }

            return result;
        }

        private GeneDetailedResult GetGeneResult(TowerState towerState, Gene gene, int pathLength, int maximumTurns)
        {
            var simulator = new Simulator(pathLength, maximumTurns, towerState.Cannons, towerState.Archers);
            var result = simulator.Simulate(new MyGeneParser(gene));

            return new GeneDetailedResult
            {
                Gene = gene,
                NormalizedGene = gene.GenomesList.Select(x => MyGeneParser.GeneToTroopCount(x)).ToArray(),
                Result = result,
            };
        }


        private List<Gene> FindBestGenes(TowerState state, int pathLength, int geneLength, int maximumTurns)
        {
            var gg = new GenerationGenerator(generationCount, geneLength);
            var generation = gg.RandomGeneration();

            var simulator = new Simulator(pathLength, maximumTurns, state.Cannons, state.Archers);

            var bestScores = new List<double>();

            while (!EnoughGenerations(bestScores, state))
            {
                Parallel.ForEach(generation, (gene) =>
                {
                    var result = simulator.Simulate(new MyGeneParser(gene));
                    gene.Score = scoringPolicy.CalculateTotalScore(result);
                });

                bestScores.Add((double)generation.Select(x => x.Score).Max());

                generation = gg.Genetic(generation);
            }

            return generation;
        }

        private bool EnoughGenerations(List<double> bestScores, TowerState state)
        {
            if (bestScores.Count < 11)
                return false;

            if (bestScores.Count > maximumGenerations)
            {
                Console.WriteLine("\rA gene has failed to converge for: ");
                Console.WriteLine("Cannons: <" + String.Join(",", state.Cannons) + ">");
                Console.WriteLine("Archers: <" + String.Join(",", state.Archers) + ">");
                return true;
            }

            if ((bestScores[bestScores.Count - 1] == bestScores[bestScores.Count - 10]) && (bestScores[bestScores.Count - 1] != 0))
                return true;

            return false;
        }
    }
}
