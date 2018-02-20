using System;
using System.Collections.Generic;
using System.Text;
using Colosseum.GS;

namespace Colosseum.Experiment.GeneParsers
{
    class MyGeneParser : IGeneParser
    {
        public Gene Gene { get; private set; }
        int len;

        public MyGeneParser(Gene gene, int _len)
        {
            Gene = gene;
            len = _len;
        }

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
                CountOfCreeps = GeneToTroopCount(a),
                CountOfHeros = GeneToTroopCount(b),
            };
        }

        public static int GeneToTroopCount(double a)
        {
            return (int)Math.Max(7, a) - 7;
        }
    }
}
