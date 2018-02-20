namespace Colosseum.Experiment.ScoringPolicies
{
    public interface IScoringPolicy
    {
        double CalculateTotalScore(SimulationResult result);
        int GetPreferredMoneyToSpend();
    }
}