namespace Cadence
{
    public interface ISignalStorage
    {
        void Save(string levelId, SignalBatch batch);
        SignalBatch Load(string levelId, int sessionIndex = -1);
        int GetSessionCount(string levelId);
        void Prune(int maxSessionsPerLevel);
    }
}
