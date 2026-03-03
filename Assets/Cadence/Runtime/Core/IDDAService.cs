using System.Collections.Generic;

namespace Cadence
{
    public interface IDDAService
    {
        // Session lifecycle
        void BeginSession(string levelId, Dictionary<string, float> levelParameters);
        void BeginSession(string levelId, Dictionary<string, float> levelParameters,
            LevelType type);
        void EndSession(SessionOutcome outcome);
        bool IsSessionActive { get; }

        // Signal recording
        void RecordSignal(string key, float value = 1f,
            SignalTier tier = SignalTier.DecisionQuality, int moveIndex = -1);

        // Real-time flow (call each frame during gameplay)
        void Tick(float deltaTime);
        FlowReading CurrentFlow { get; }

        // Between-session adjustment
        AdjustmentProposal GetProposal(Dictionary<string, float> nextLevelParameters);
        AdjustmentProposal GetProposal(Dictionary<string, float> nextLevelParameters,
            LevelType nextLevelType, int nextLevelIndex = -1);

        // Player profile
        PlayerSkillProfile PlayerProfile { get; }
        PlayerArchetypeReading CurrentArchetype { get; }

        // Profile serialization
        string SaveProfile();
        void LoadProfile(string json);

        // Difficulty scheduling
        float GetTargetMultiplier(int levelIndex);

        // Debug
        DDADebugData GetDebugSnapshot();
    }
}
