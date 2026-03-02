using System;

namespace Cadence
{
    public interface IFlowDetector
    {
        FlowReading CurrentReading { get; }
        void Tick(float deltaTime, SignalRingBuffer recentSignals);
        void Reset();
        event Action<FlowReading> OnFlowStateChanged;
    }
}
