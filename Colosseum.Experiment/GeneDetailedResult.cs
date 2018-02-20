using Colosseum.GS;

namespace Colosseum.Experiment
{
    public class GeneDetailedResult
    {
        public Gene Gene { get; set; }
        public int[] NormalizedGene { get; set; }
        public SimulationResult Result { get; set; }
    }
}