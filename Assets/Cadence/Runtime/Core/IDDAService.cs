using System.Collections.Generic;

namespace Cadence
{
    public interface IDDAService
    {
        // Session lifecycle
        void BeginSession(string levelId, Dictionary<string, float> levelParameters);
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

        // Player profile
        PlayerSkillProfile PlayerProfile { get; }

        // Debug
        DDADebugData GetDebugSnapshot();
    }
}
