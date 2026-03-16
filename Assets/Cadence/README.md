# Cadence

Unity adaptive difficulty toolkit for single-player, level-based games. In its current state it is best suited to casual puzzle integrations where the developer can provide per-move quality signals and owns the meaning of each difficulty parameter.

Cadence is not currently a universal drop-in SDK for every genre or every mobile puzzle subtype.

See the root repository README for the full integration guide, limitations, and audit:
- [Root README](https://github.com/Ludaxis/cadence#readme)

## Quick Start

```csharp
CadenceManager.Service.BeginSession("level_1", levelParams, LevelType.MoveLimited);

CadenceManager.Service.RecordSignal(
    SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, moveIndex);

CadenceManager.Service.RecordSignal(
    SignalKeys.MoveOptimal,
    wasOptimal ? 1f : 0f,
    SignalTier.DecisionQuality,
    moveIndex);

if (!wasOptimal)
{
    CadenceManager.Service.RecordSignal(
        SignalKeys.MoveWaste, 1f, SignalTier.DecisionQuality, moveIndex);
}

CadenceManager.Service.EndSession(SessionOutcome.Win);

var proposal = CadenceManager.Service.GetProposal(
    nextLevelParams,
    nextLevelType,
    nextLevelIndex);
```

## Actual Runtime Contract

Required:
- `BeginSession(...)`
- `MoveExecuted`
- `MoveOptimal` with `1` for good moves and `0` for bad moves
- `Tick(...)` each frame unless you use `CadenceManager`
- `EndSession(...)`

Consumed now:
- `move.executed`
- `move.optimal`
- `move.waste`
- `resource.efficiency`
- `progress.delta`
- `tempo.interval`
- `tempo.hesitation`
- `tempo.pause`
- `strategy.powerup`
- `strategy.sequence_match`
- `meta.attempt`
- `meta.session_gap`
- `meta.abandoned`
- `input.accuracy`
- `input.rejected`

Declared but not consumed now:
- `strategy.stored`

## Main Limitations

- Default built-in rules now include `SessionFatigueRule`, which gently eases long contiguous play sessions. Treat that as part of the baseline behavior, not an optional add-on.
- Scalar parameter polarity is supported, but Cadence still does not understand blockers, board topology, or other authored puzzle semantics.
- `IDDAService` now supports `RegisterRule(...)`, `RegisterRuleProvider(...)`, and `RegisterLevelTypeConfigProvider(...)`, but there is still no polished plugin/discovery surface for external SDK consumers.
- Use the explicit `GetProposal(nextLevelParams, nextLevelType, nextLevelIndex)` overload. It is now the only supported proposal path.
- Real-time flow detection is live, but Cadence does not automatically apply mid-session interventions.

## Modules

| Module | Description |
|---|---|
| Signals | Collects and buffers gameplay signals |
| Analysis | Builds a `SessionSummary` at session end |
| Player Model | Tracks rating, deviation, volatility, and recent history |
| Flow Detection | Exposes real-time `FlowReading` values |
| Adjustment | Produces `AdjustmentProposal` deltas through built-in rules |
| Scheduling | Applies sawtooth multiplier when a level index is provided |
| Profiling | Classifies player archetypes from recent history |

## Editor Tools

| Tool | Menu |
|---|---|
| Setup Wizard | `Cadence > Setup Wizard` |
| Create Manager | `Cadence > Create Manager in Scene` |
| Debug Window | `Cadence > Debug Window` |
| Signal Replay | `Cadence > Signal Replay` |
| Difficulty Curve Preview | `Cadence > Difficulty Curve Preview` |
| Sandbox Dashboard | `Cadence > Sandbox Dashboard` |
| Scenario Simulator | `Cadence > Scenario Simulator` |

## Requirements

- Unity 2021.3+
- No external runtime dependencies
- Optional: Odin Inspector for enhanced editor inspectors
