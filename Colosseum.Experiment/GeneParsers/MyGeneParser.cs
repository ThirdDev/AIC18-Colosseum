using System;
using System.Collections.Generic;
using System.Text;
using Colosseum.GS;

namespace Colosseum.Experiment.GeneParsers
{
    class MyGeneParser : IGeneParser
    {
        public Gene Gene { get; private set; }

        public MyGeneParser(Gene gene)
        {
            Gene = gene;
        }

        public AttackAction Parse(int turn)
        {
            if (turn >= Gene.GenomesList.Count)
                return new AttackAction
                {
                    CountOfHeros = 0,
                    CountOfCreeps = 0,
                };

            var length = Gene.GenomesList.Count / 2;

            var creepGenome = Gene.GenomesList[turn % length];
            var heroGenome = Gene.GenomesList[(turn % length) + length];

            return new AttackAction
            {
                CountOfCreeps = GeneToTroopCount(creepGenome),
                CountOfHeros = GeneToTroopCount(heroGenome),
            };
        }

        public static int GeneToTroopCount(double a)
        {
            return (int)Math.Max(5, a) - 5;
        }
    }
}
