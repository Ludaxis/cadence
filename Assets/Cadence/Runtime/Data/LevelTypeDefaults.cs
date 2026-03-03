namespace Cadence
{
    public static class LevelTypeDefaults
    {
        public static LevelTypeConfig GetDefaults(LevelType type)
        {
            switch (type)
            {
                case LevelType.MoveLimited:
                    return new LevelTypeConfig(type, "move_limit", 1f, true, true);

                case LevelType.TimeLimited:
                    return new LevelTypeConfig(type, "time_limit", 1f, true, true);

                case LevelType.GoalCollection:
                    return new LevelTypeConfig(type, "goal_count", 0.8f, true, true);

                case LevelType.Boss:
                    return new LevelTypeConfig(type, null, 0.3f, true, true);

                case LevelType.Breather:
                    return new LevelTypeConfig(type, null, 0.5f, false, true);

                case LevelType.Tutorial:
                    return new LevelTypeConfig(type, null, 0f, false, false);

                default: // Standard
                    return new LevelTypeConfig(type, null, 1f, true, true);
            }
        }
    }
}
