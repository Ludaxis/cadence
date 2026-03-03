# Cadence — Dynamic Difficulty Adjustment SDK

Game-agnostic DDA SDK for Unity. Signal-based player modeling, real-time flow detection, Glicko-2 skill tracking, and adaptive difficulty proposals.

See the [full documentation](https://github.com/Ludaxis/cadence#readme) for installation, integration guide, API reference, and examples.

## Quick Start

```csharp
// 1. Add CadenceManager to your scene: Cadence > Create Manager in Scene
// 2. Record signals during gameplay:
CadenceManager.Service.BeginSession("level_1", levelParams);
CadenceManager.Service.RecordSignal(SignalKeys.MoveExecuted);
CadenceManager.Service.RecordSignal(SignalKeys.MoveOptimal);

// 3. Get adjustment proposals between levels:
CadenceManager.Service.EndSession(SessionOutcome.Win);
var proposal = CadenceManager.Service.GetProposal(nextLevelParams);
```

CadenceManager handles `Tick()`, profile persistence, and editor tool connections automatically.

## Modules

| Module | Description |
|--------|-------------|
| **Signals** | Collect, buffer, persist, and replay gameplay signals via a ring buffer |
| **Analysis** | Session-level statistics with exponential running averages |
| **Player Model** | Glicko-2 skill rating with confidence intervals and time-decay |
| **Flow Detection** | Real-time state classification: Flow, Boredom, Anxiety, Frustration |
| **Adjustment** | Rule-based difficulty proposals with cooldown and damping |
| **Scheduling** | Sawtooth difficulty curves for periodic challenge waves |
| **Profiling** | Player archetype classification (SpeedRunner, CarefulThinker, etc.) |

## Editor Tools

| Tool | Menu |
|------|------|
| Setup Wizard | `Cadence > Setup Wizard` |
| Create Manager | `Cadence > Create Manager in Scene` |
| Debug Window | `Cadence > Debug Window` |
| Signal Replay | `Cadence > Signal Replay` |
| Difficulty Curve | `Cadence > Difficulty Curve Preview` |
| Sandbox | `Cadence > Sandbox Dashboard` |

## Requirements

- Unity 2021.3+
- No external dependencies
- Optional: [Odin Inspector](https://odininspector.com/) for enhanced config inspectors
