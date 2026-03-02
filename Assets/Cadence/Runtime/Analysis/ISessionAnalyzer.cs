namespace Cadence
{
    public interface ISessionAnalyzer
    {
        SessionSummary Analyze(SignalBatch batch);
    }
}
