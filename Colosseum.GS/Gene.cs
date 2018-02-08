using System.Collections.Generic;

namespace Colosseum.GS
{
    public class Gene
    {
        public const int LengthOfGene = 10;
        public List<double> GenomesList { get; private set; } = new List<double>(LengthOfGene);
        public double Score { get; set; } = -1;
    }
}