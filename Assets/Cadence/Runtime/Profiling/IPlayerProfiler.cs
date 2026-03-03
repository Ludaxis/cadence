namespace Cadence
{
    public interface IPlayerProfiler
    {
        PlayerArchetypeReading Classify(PlayerSkillProfile profile, SessionSummary lastSession);
    }
}
