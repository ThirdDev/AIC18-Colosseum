using Colosseum.GS;

namespace Colosseum.Experiment.GeneParsers
{
    public interface IGeneParser
    {
        Gene Gene { get; }
        AttackAction Parse(int turn);
    }
}