using System;
using System.Collections.Generic;

namespace Cadence
{
    public sealed class SignalReplay : ISignalReplay
    {
        private List<SignalEntry> _entries;
        private int _currentIndex;
        private float _playbackTime;
        private float _speed = 1f;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;
        public float PlaybackTime => _playbackTime;
        public float TotalDuration { get; private set; }
        public int CurrentIndex => _currentIndex;
        public int TotalEntries => _entries != null ? _entries.Count : 0;

        public event Action<SignalEntry> OnSignalReplayed;
        public event Action OnPlaybackComplete;

        public void Load(SignalBatch batch)
        {
            _entries = new List<SignalEntry>(batch.Entries);
            _currentIndex = 0;
            _playbackTime = 0f;
            _isPlaying = false;

            if (_entries.Count > 0)
                TotalDuration = _entries[_entries.Count - 1].Timestamp.SessionTime;
            else
                TotalDuration = 0f;
        }

        public void Play(float speed = 1f)
        {
            if (_entries == null || _entries.Count == 0) return;
            _speed = speed;
            _isPlaying = true;
        }

        public void Pause()
        {
            _isPlaying = false;
        }

        public void Stop()
        {
            _isPlaying = false;
            _currentIndex = 0;
            _playbackTime = 0f;
        }

        public void SeekTo(float sessionTime)
        {
            _playbackTime = sessionTime;
            _currentIndex = 0;

            if (_entries == null) return;

            // Find the first entry at or after the seek time
            for (int i = 0; i < _entries.Count; i++)
            {
                if (_entries[i].Timestamp.SessionTime >= sessionTime)
                {
                    _currentIndex = i;
                    return;
                }
            }
            _currentIndex = _entries.Count;
        }

        /// <summary>
        /// Call each frame to advance playback. Returns signals that were reached.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_isPlaying || _entries == null) return;

            _playbackTime += deltaTime * _speed;

            while (_currentIndex < _entries.Count &&
                   _entries[_currentIndex].Timestamp.SessionTime <= _playbackTime)
            {
                OnSignalReplayed?.Invoke(_entries[_currentIndex]);
                _currentIndex++;
            }

            if (_currentIndex >= _entries.Count)
            {
                _isPlaying = false;
                OnPlaybackComplete?.Invoke();
            }
        }
    }
}
