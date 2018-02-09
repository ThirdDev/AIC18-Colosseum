using System;
using System.Collections.Generic;

namespace Colosseum.GS
{
    public class Gene
    {
        public int Id { get; } => GetHashCode();
        public const int LengthOfGene = 200;
        public List<double> GenomesList { get; private set; } = new List<double>(LengthOfGene);
        public double Score { get; set; } = -1;

        public override string ToString()
        {
            return string.Join(Environment.NewLine, GenomesList.Count, string.Join(Environment.NewLine, GenomesList.ToArray()));
        }

        //public override int GetHashCode()
        //{
        //    unchecked
        //    {
        //        int hash = 19;
        //        foreach (var genome in GenomesList)
        //        {
        //            hash = hash * 31 + genome.GetHashCode();
        //        }
        //        return hash;
        //    }
        //}
    }
}