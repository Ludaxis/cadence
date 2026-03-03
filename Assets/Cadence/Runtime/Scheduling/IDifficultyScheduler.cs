namespace Cadence
{
    public interface IDifficultyScheduler
    {
        float GetTargetMultiplier(int levelIndex);
        LevelType GetSuggestedLevelType(int levelIndex);
        DifficultyPoint[] GetCurvePreview(int start, int count);
    }
}
