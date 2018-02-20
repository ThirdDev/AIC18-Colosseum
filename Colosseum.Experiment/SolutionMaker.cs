using Colosseum.Experiment.GeneParsers;
using Colosseum.Experiment.ScoringPolicies;
using Colosseum.Experiment.TowerStateMakers;
using Colosseum.GS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Colosseum.Experiment
{
    public class SolutionMaker
    {
        const int maximumTurns = 100;
        const int generationCount = 100;
        const int countOfBestGenesToSave = 3;

        private readonly ITowerStateMaker towerStateMaker;
        private readonly IScoringPolicy scoringPolicy;

        public SolutionMaker(ITowerStateMaker towerStateMaker, IScoringPolicy scoringPolicy)
        {
            this.towerStateMaker = towerStateMaker;
            this.scoringPolicy = scoringPolicy;
        }

        public void Make(int pathLength)
        {
            string outputFile = $"{towerStateMaker.GetType().Name}-{scoringPolicy.GetType().Name} {scoringPolicy.GetPreferredMoneyToSpend()}-pathLength {pathLength}-{DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss")}.json";
            string outputDirectory = "output";

            var towerStates = towerStateMaker.GetTowerStates(pathLength);

            List<TowerStateResult> results = new List<TowerStateResult>();

            Console.WriteLine($"{towerStateMaker.GetType().Name}\r\n{scoringPolicy.GetType().Name} with preferred amount of {scoringPolicy.GetPreferredMoneyToSpend()}\r\npathLength: {pathLength}");
            Console.WriteLine();

            Console.WriteLine($"Generated {towerStates.Count} states.");
            Console.WriteLine();

            for (int i = 0; i < towerStates.Count; i++)
            {
                List<List<Gene>> bestGenes = new List<List<Gene>>();
                for (int j = 0; j < countOfBestGenesToSave; j++)
                {
                    bestGenes.Add(FindBestGenes(towerStates[i], pathLength).OrderByDescending(x => x.Score).ToList());
                }

                results.Add(GetTowerStateResult(towerStates[i], bestGenes, pathLength));

                Console.Write("\r");
                Console.Write($"{i + 1} / {towerStates.Count}");
            }

            Console.WriteLine();
            Console.WriteLine($"Writing output file '{outputFile}'...");

            var outputString = JsonConvert.SerializeObject(results, Formatting.Indented);

            if (!Directory.Exists(outputDirectory))
                Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(Path.Combine(outputDirectory, outputFile), outputString);

            Console.WriteLine("Done.");
            Console.ReadKey();
        }

        private TowerStateResult GetTowerStateResult(TowerState towerState, List<List<Gene>> bestGenes, int pathLength)
        {
            var result = new TowerStateResult
            {
                TowerState = towerState,
            };

            foreach (var item in bestGenes)
            {
                result.Genes.Add(GetGeneResult(towerState, item[0], pathLength));
            }

            return result;
        }

        private GeneDetailedResult GetGeneResult(TowerState towerState, Gene gene, int pathLength)
        {
            var simulator = new Simulator(pathLength, maximumTurns, towerState.Cannons, towerState.Archers);
            var result = simulator.Simulate(new MyGeneParser(gene, pathLength));

            return new GeneDetailedResult
            {
                Gene = gene,
                NormalizedGene = gene.GenomesList.Select(x => MyGeneParser.GeneToTroopCount(x)).ToArray(),
                Result = result,
            };
        }


        private List<Gene> FindBestGenes(TowerState state, int pathLength)
        {
            var gg = new GenerationGenerator(generationCount, pathLength * 2);
            var generation = gg.RandomGeneration();

            var simulator = new Simulator(pathLength, maximumTurns, state.Cannons, state.Archers);

            List<double> bestScores = new List<double>();

            while (!EnoughGenerations(bestScores))
            {
                foreach (var gene in generation)
                {
                    var result = simulator.Simulate(new MyGeneParser(gene, pathLength));
                    gene.Score = scoringPolicy.CalculateTotalScore(result);
                }

                bestScores.Add((double)generation.Select(x => x.Score).Max());

                generation = gg.Genetic(generation);
            }

            return generation;
        }

        private bool EnoughGenerations(List<double> bestScores)
        {
            if (bestScores.Count < 11)
                return false;

            if ((bestScores[bestScores.Count - 1] == bestScores[bestScores.Count - 10]) && (bestScores[bestScores.Count - 1] != 0))
                return true;

            return false;
        }
    }
}
