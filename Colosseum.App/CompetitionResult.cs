namespace Colosseum.App
{
    public class CompetitionResult
    {
        public CompetitionResultStatus Status { get; set; }
        public int TryCount { get; set; }
    }
    
    public enum CompetitionResultStatus
    {
        Successful,
        Failed,
    }
}