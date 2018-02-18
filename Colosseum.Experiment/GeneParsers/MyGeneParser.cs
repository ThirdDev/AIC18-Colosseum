using System;
using System.Collections.Generic;
using System.Text;
using Colosseum.GS;

namespace Colosseum.Experiment.GeneParsers
{
    class MyGeneParser : IGeneParser
    {
        public Gene Gene { get; private set; }

        private readonly double _mean;

        public MyGeneParser(Gene gene, double mean)
        {
            Gene = gene;
            _mean = mean;
        }

        const int len = 15;

        public AttackAction Parse(int turn)
        {
            if (turn >= len)
                return new AttackAction
                {
                    CountOfHeros = 0,
                    CountOfCreeps = 0,
                };

            var a = Gene.GenomesList[turn % len];
            var b = Gene.GenomesList[(turn % len) + len];

            return new AttackAction
            {
                CountOfCreeps = GeneToTroopCount(a, _mean),
                CountOfHeros = GeneToTroopCount(b, _mean),
            };
        }

        public static int GeneToTroopCount(double a, double mean)
        {
            return (int)(Math.Max(-mean, a) + mean);
        }
    }
}
