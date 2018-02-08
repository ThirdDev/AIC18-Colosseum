using System;
using System.Collections.Generic;

namespace Colosseum.GS
{
    public class Gene
    {
        public const int LengthOfGene = 10;
        public List<double> GenomesList { get; private set; } = new List<double>(LengthOfGene);
        public double Score { get; set; } = -1;

        public override string ToString()
        {
            return string.Join(Environment.NewLine, GenomesList.Count, string.Join(Environment.NewLine, GenomesList.ToArray()));
        }
    }
}