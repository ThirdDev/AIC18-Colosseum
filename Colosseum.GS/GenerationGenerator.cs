using System;
using System.Collections.Generic;
using System.Linq;

namespace Colosseum.GS
{
    public class GenerationGenerator
    {
        private const double PartToLive = 0.15;
        private const double rangeOfMutaion = 0.05;
        private int _generationPopulation;

        public GenerationGenerator(int generationPopulation)
        {
            _generationPopulation = generationPopulation;
        }

        private const double stdDrivationForLives = PartToLive * 2 / 3;


        public List<Gene> RandomGeneration()
        {
            var random = new Random();
            var adamAndEveAndFriends = new List<Gene>(10);

            for (var j = 0; j < _generationPopulation; j++)
            {
                var tmp = new Gene();
                for (var i = 0; i < Gene.LengthOfGene; i++)
                {
                    var weight = (random.NextDouble() - 0.5) * 20;
                    tmp.GenomesList.Add(weight);
                }
                adamAndEveAndFriends.Add(tmp);
            }
            return adamAndEveAndFriends;
        }

        //Generates a new Generation based on the last Generation
        //Last Generation should be evaluted beforehead
        public List<Gene> Genetic(List<Gene> lastGeneration)
        {
            if (lastGeneration == null)
            {
                lastGeneration = RandomGeneration();
            }
            lastGeneration = lastGeneration.OrderByDescending(x => x.Score).ToList();
            var newGeneration = lastGeneration.GetRange(0, (int)(lastGeneration.Count * PartToLive));
            newGeneration.AddRange(ChildrenMaker(lastGeneration, (int)(lastGeneration.Count * (1 - PartToLive))));
            return newGeneration;
        }


        //Generates a list of new genes based on the input generation.
        //The input generation should be a descending sorted list of genes based on their scores.
        public List<Gene> ChildrenMaker(List<Gene> generation, int count)
        {
            var children = new List<Gene>(count);
            var random = new Random();
            for (var j = 0; j < count; j++)
            {
                var indexDad = getRandomParentIndex(random, generation.Count);
                var indexMom = getRandomParentIndex(random, generation.Count);

                var tmp = new Gene();
                for (var i = 0; i < Gene.LengthOfGene; i++)
                {
                    var randAns = random.NextDouble();
                    if (randAns < rangeOfMutaion)
                    {
                        var mean = (generation[indexDad].GenomesList[i] + generation[indexMom].GenomesList[i]) / 2;
                        var derivation = Math.Abs(generation[indexDad].GenomesList[i] - mean);
                        tmp.GenomesList.Add(gaussianRandom(random, mean, derivation));
                    }
                    else
                    {
                        randAns -= rangeOfMutaion / 2;
                        if (randAns > 0.5)
                        {
                            tmp.GenomesList.Add(generation[indexDad].GenomesList[i]);
                        }
                        else
                        {
                            tmp.GenomesList.Add(generation[indexMom].GenomesList[i]);
                        }
                    }
                }
                children.Add(tmp);
            }

            return children;

        }

        private int getRandomParentIndex(Random random, int count)
        {
            double randomIndex;
            do
            {
                randomIndex = gaussianRandom(random, 0, stdDrivationForLives);
            } while (randomIndex >= 1);

            return (int)(randomIndex * count);
        }


        //returns a normal random in range of (0,1)
        private double gaussianRandom(Random random, double mean, double stdDrivation)
        {

            var u1 = 1.0 - random.NextDouble(); //uniform(0,1] random doubles
            var u2 = 1.0 - random.NextDouble();
            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                   Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            return Math.Abs(mean + randStdNormal * stdDrivation);
        }
    }
}