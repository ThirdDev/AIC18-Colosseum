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
                CountOfCreeps = (int)Math.Max(7, a) - 7,
                CountOfHeros = (int)Math.Max(7, b) - 7,
            };
        }
    }
}
