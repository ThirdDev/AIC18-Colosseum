using System;
using System.Collections.Generic;
using System.Linq;

namespace Colosseum.GS
{
    public class GenerationGenerator
    {
        private const double PartToLive = 0.5;
        private const double rangeOfMutaion = 0.05;
        
        
        public List<Gene> randomGeneration ()
        {
            Random random = new Random();
            List<Gene> adamAndEveAndFriends = new List<Gene>(10);
            
            for (int j = 0; j < 20000; j++)
            {
                var tmp = new Gene();
                for (int i = 0; i < Gene.LengthOfGene; i++)
                {
                    tmp.GenomesList.Add(random.NextDouble());
                    tmp.Score = random.NextDouble();
                }
                adamAndEveAndFriends.Add(tmp);
            }
            var nextGen = Genetic(adamAndEveAndFriends);
            return nextGen;
        }

        //Generates a new Generation based on the last Generation
        //Last Generation should be evaluted beforehead
        public List<Gene> Genetic(List<Gene> lastGeneration)
        {
            lastGeneration = lastGeneration.OrderByDescending(x => x.Score).ToList();
            List<Gene> newGeneration = lastGeneration.GetRange(0, (int)(lastGeneration.Count * PartToLive));
            newGeneration.AddRange(ChildrenMaker(lastGeneration,(int)(lastGeneration.Count * (1-PartToLive))));
            return newGeneration;
        }
        
        
        //Generates a list of new genes based on the input generation.
        //The input generation should be a descending sorted list of genes based on their scores.
        public List<Gene> ChildrenMaker(List<Gene> generation, int count)
        {
            List<Gene> children = new List<Gene>(count);
            Random random = new Random();
            for(int j = 0 ; j < count ; j++)
            {
                //int indexDad = (int)(random.NextDouble() * generation.Count);
                //int indexMom = (int)(random.NextDouble() * generation.Count);
                double randomIndex = GaussianRandom(random , 0 , 0.3);
                while (randomIndex >= 1) randomIndex = GaussianRandom(random, 0 , 0.3);
                int indexDad = (int)(randomIndex * generation.Count);
                while (randomIndex >= 1) randomIndex = GaussianRandom(random, 0 , 0.3);
                int indexMom = (int)(randomIndex * generation.Count);
                var tmp = new Gene();
                for (int i = 0; i < Gene.LengthOfGene; i++)
                {
                    double randAns = random.NextDouble();
                    if (randAns < rangeOfMutaion)
                    {
                        double mean = (generation[indexDad].GenomesList[i] + generation[indexMom].GenomesList[i]) / 2;
                        double derivation = Math.Abs(generation[indexDad].GenomesList[i] - mean);
                        tmp.GenomesList.Add(GaussianRandom(random, mean , derivation));
                    }
                    else
                    {
                        randAns -= rangeOfMutaion/2;
                    }
                    
                    if (randAns > 0.5)
                    {
                        tmp.GenomesList.Add(generation[indexDad].GenomesList[i]);
                    }
                    else
                    {
                        tmp.GenomesList.Add(generation[indexMom].GenomesList[i]);
                    }
                }
                children.Add(tmp);
            }

            return children;

        }
        
        
        //returns a normal random in range of (0,1)
        private double GaussianRandom(Random random , double mean , double stdDrivation)
        {
            
            double u1 = 1.0-random.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0-random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                   Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            return Math.Abs(mean + randStdNormal * stdDrivation);
        }
    }
}