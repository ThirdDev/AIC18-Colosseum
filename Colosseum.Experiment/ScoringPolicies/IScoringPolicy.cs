namespace Colosseum.Experiment.ScoringPolicies
{
    internal interface IScoringPolicy
    {
        double CalculateTotalScore(SimulationResult result);
    }
}