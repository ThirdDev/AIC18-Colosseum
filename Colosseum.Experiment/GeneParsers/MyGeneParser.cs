using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Colosseum.GS;

namespace Colosseum.Experiment.GeneParsers
{
    class MyGeneParser : IGeneParser
    {
        public Gene Gene { get; private set; }

        int len;

        public MyGeneParser(Gene gene)
        {
            Gene = gene;

            len = gene.GenomesList.Count / 2;

            var creepGenePart = gene.GenomesList.GetRange(0, len).ToList();
            var heroGenePart = gene.GenomesList.GetRange(0, len).ToList();

            int counter = 0;
            for (int i = 0; i < len; i++)
            {
                if (counter > len)
                    break;
                counter++;

                if (GetSoldierCount(creepGenePart[i]) == 0 && GetSoldierCount(heroGenePart[i]) == 0)
                {
                    double g1 = creepGenePart[i];
                    double g2 = heroGenePart[i];

                    creepGenePart.RemoveAt(i);
                    heroGenePart.RemoveAt(i);
                    i--;

                    creepGenePart.Add(g1);
                    heroGenePart.Add(g2);
                }
            }

            gene.GenomesList.Clear();
            gene.GenomesList.AddRange(creepGenePart);
            gene.GenomesList.AddRange(heroGenePart);
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
                CountOfCreeps = GetSoldierCount(a),
                CountOfHeros = GetSoldierCount(b),
            };
        }

        private static int GetSoldierCount(double a)
        {
            return (int)Math.Max(7, a) - 7;
        }
    }
}
