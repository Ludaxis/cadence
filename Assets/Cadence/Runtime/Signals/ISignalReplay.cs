using System;

namespace Cadence
{
    public interface ISignalReplay
    {
        void Load(SignalBatch batch);
        bool IsPlaying { get; }
        float PlaybackTime { get; }
        float TotalDuration { get; }
        int CurrentIndex { get; }
        int TotalEntries { get; }

        void Play(float speed = 1f);
        void Pause();
        void Stop();
        void SeekTo(float sessionTime);

        event Action<SignalEntry> OnSignalReplayed;
        event Action OnPlaybackComplete;
    }
}
