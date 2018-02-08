using System.Collections.Generic;

namespace Colosseum.GS
{
    public class Gene
    {
        public const int LengthOfGene = 20;
        public List<double> GenomesList { get; private set; } = new List<double>(LengthOfGene);
        public double Score { get; set; } = -1;
    }
}