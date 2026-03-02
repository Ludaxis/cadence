namespace Cadence
{
    public interface IPlayerModel
    {
        PlayerSkillProfile Profile { get; }
        void UpdateFromSession(SessionSummary summary);
        float PredictWinRate(float levelDifficulty);
        void ApplyTimeDecay(float daysSinceLastSession);
        string Serialize();
        void Deserialize(string json);
    }
}
