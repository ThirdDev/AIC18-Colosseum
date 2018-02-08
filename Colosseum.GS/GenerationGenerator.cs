using System;
using System.Collections.Generic;
using System.Linq;

namespace Colosseum.GS
{
    public class GenerationGenerator
    {
        private const double PartToLive = 0.5;
        private const double rangeOfMutaion = 0.05;
        
        //Generates a new Generation based on the last Generation
        //Last Generation should be evaluted beforehead
        public List<Gene> Genetic(List<Gene> lastGeneration)
        {
            lastGeneration = lastGeneration.OrderByDescending(x => x.Score).ToList();
            List<Gene> newGeneration = lastGeneration.GetRange(0, (int)(lastGeneration.Count * PartToLive));
            newGeneration.AddRange(ChildrenMaker(newGeneration,(int)(lastGeneration.Count * (1-PartToLive))));
            return newGeneration;
        }
        
        
        //Generates a list of new genes based on the input generation.
        //The input generation should be a descending sorted list of genes based on their scores.
        public List<Gene> ChildrenMaker(List<Gene> generation, int count)
        {
            List<Gene> children = new List<Gene>(count);
            Random random = new Random();
            foreach (var child in children)
            {
                int indexDad = (int)(Math.Abs(0.5 - GaussianRandom(random)) * generation.Count);
                int indexMom = (int)(Math.Abs(0.5 - GaussianRandom(random)) * generation.Count);
                for (int i = 0; i < Gene.LengthOfGene; i++)
                {
                    double randAns = random.NextDouble();
                    if (randAns < rangeOfMutaion)
                    {
                        child.GenomesList[i] = random.NextDouble();
                    }
                    else
                    {
                        randAns -= rangeOfMutaion/2;
                    }
                    if ( randAns > 0.5)
                    {
                        child.GenomesList[i] = generation[indexDad].GenomesList[i];
                    }
                    else
                    {
                        child.GenomesList[i] = generation[indexMom].GenomesList[i];
                    }
                }
            }

            return children;

        }
        
        
        //returns a normal random in range of (0,1)
        private double GaussianRandom(Random random)
        {
            
            double u1 = 1.0-random.NextDouble(); //uniform(0,1] random doubles
            double u2 = 1.0-random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                   Math.Sin(2.0 * Math.PI * u2); //random normal(0,1)
            return randStdNormal;
        }
    }
}