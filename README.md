# Cadence

Adaptive difficulty toolkit for Unity, currently best suited to single-player, level-based casual puzzle games where you can expose scalar difficulty parameters and record move-by-move quality.

Cadence has a solid technical base: signal collection, session analysis, Glicko-2 profile tracking, real-time flow classification, rule-based proposals, parameter polarity support for scalar puzzle keys, and strong editor tooling. It is not currently a universal "drop this into any game" SDK, and it is not yet safe to market as covering every mobile puzzle subtype without caveats.

## Current Fit

Best fit:
- Match-3, tile tap, merge, word, and similar casual puzzle games with clear level attempts.
- Games that can emit one quality score per move and apply numeric parameter changes between levels.
- Internal studio tooling where engineers can own the final meaning of each difficulty parameter.

Not a strong fit yet:
- Broad "any genre" positioning.
- Puzzle games whose main difficulty comes from board topology, scripted blockers, spatial layouts, or non-scalar content changes.
- Plug-and-play SDK distribution where developers need public rule injection and parameter semantics handled for them.

## Installation

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ludaxis.cadence": "https://github.com/Ludaxis/cadence.git?path=Assets/Cadence"
  }
}
```

Pin a version:

```json
"com.ludaxis.cadence": "https://github.com/Ludaxis/cadence.git?path=Assets/Cadence#v1.2.1"
```

You can also:
- Copy `Assets/Cadence/` into your project.
- Reference `Assets/Cadence` as a local UPM package.
- Open this repo root directly in Unity 2021.3+.

## Quick Start

### CadenceManager

`CadenceManager` is the simplest path. It auto-ticks the service, loads and saves the profile, and survives scene loads.

```csharp
using Cadence;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleGameManager : MonoBehaviour
{
    public void StartLevel(string levelId,
        Dictionary<string, float> levelParams,
        LevelType levelType)
    {
        CadenceManager.Service.BeginSession(levelId, levelParams, levelType);
    }

    public void RecordMove(int moveIndex, bool wasOptimal, float hesitationSeconds)
    {
        var dda = CadenceManager.Service;

        dda.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, moveIndex);
        dda.RecordSignal(SignalKeys.MoveOptimal,
            wasOptimal ? 1f : 0f,
            SignalTier.DecisionQuality,
            moveIndex);

        if (!wasOptimal)
            dda.RecordSignal(SignalKeys.MoveWaste, 1f, SignalTier.DecisionQuality, moveIndex);

        if (hesitationSeconds > 0f)
            dda.RecordSignal(SignalKeys.HesitationTime,
                hesitationSeconds,
                SignalTier.BehavioralTempo,
                moveIndex);
    }

    public AdjustmentProposal EndLevel(
        SessionOutcome outcome,
        Dictionary<string, float> nextLevelParams,
        LevelType nextLevelType,
        int nextLevelIndex)
    {
        var dda = CadenceManager.Service;
        dda.EndSession(outcome);

        return dda.GetProposal(nextLevelParams, nextLevelType, nextLevelIndex);
    }
}
```

### Code-Only

```csharp
using Cadence;

var config = Resources.Load<DDAConfig>("DDAConfig");
IDDAService dda = new DDAService(config);

dda.BeginSession("level_42", levelParams, LevelType.MoveLimited);

dda.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, moveIndex);
dda.RecordSignal(SignalKeys.MoveOptimal, wasOptimal ? 1f : 0f,
    SignalTier.DecisionQuality, moveIndex);

dda.Tick(Time.deltaTime);
FlowReading flow = dda.CurrentFlow;

dda.EndSession(SessionOutcome.Win);
AdjustmentProposal proposal = dda.GetProposal(nextLevelParams, nextLevelType, nextLevelIndex);
```

## How It Works

Cadence is a caller-driven pipeline:

1. `BeginSession(...)` resets the collector and flow detector, clones the current level parameters, and resets the contiguous play-session fatigue counter when the inter-level idle gap exceeds the configured threshold.
2. `RecordSignal(...)` stores each signal in the session batch and the fixed-size ring buffer.
3. `Tick(deltaTime)` advances the flow detector using only new ring-buffer entries since the previous tick.
4. `EndSession(...)` runs `SessionAnalyzer`, builds a `SessionSummary`, and updates the Glicko-2 player profile.
5. `GetProposal(...)` classifies the player archetype, runs the built-in rules, merges duplicate deltas, applies sawtooth scaling, and clamps the result.
6. Your game decides whether and how to apply `proposal.Deltas`.

Important current behavior:
- Real-time flow detection is live during the session.
- Proposal generation is still caller-driven and normally used between levels.
- `AdjustmentProposal.Timing` can be `MidSession`, but Cadence does not automatically push or apply mid-session adjustments for you.

## Inputs

### Required Runtime Calls

| Input | Required | Notes |
|---|---|---|
| `BeginSession(levelId, levelParams, levelType)` | Yes | Use the explicit `levelType` overload unless every level is effectively `Standard`. |
| `RecordSignal(MoveExecuted, ...)` | Yes | Needed for move count and timing. |
| `RecordSignal(MoveOptimal, 1 or 0, ...)` | Yes | Current flow detection expects a value for every move, not just good moves. |
| `Tick(deltaTime)` | Yes for live flow | `CadenceManager` does this automatically. |
| `EndSession(outcome)` | Yes | Produces the session summary and updates the player model. |
| `GetProposal(nextLevelParams, nextLevelType, nextLevelIndex)` | Recommended | Use the explicit overload for mixed level types or sawtooth scheduling. |

### Signals Consumed by the Current Runtime

| Signal | Used By | Effect |
|---|---|---|
| `move.executed` | Flow detector, session analyzer | Move count and fallback timing when no explicit `tempo.interval` is present. |
| `move.optimal` | Flow detector, session analyzer | Real-time efficiency window and session move efficiency. |
| `move.waste` | Session analyzer | Waste ratio and frustration score. |
| `resource.efficiency` | Session analyzer, player model, profiler | Enriches `EffectiveEfficiency01`, skill scoring, Glicko updates, and profiler trends. |
| `progress.delta` | Flow detector, session analyzer | Engagement signal and session progress rate. |
| `tempo.interval` | Flow detector, session analyzer | Authoritative tempo input for the whole session once first seen; non-positive values ignored. |
| `tempo.hesitation` | Session analyzer | Stored as hesitation time in the session summary. |
| `tempo.pause` | Flow detector, session analyzer | Real-time engagement penalty and pause count. |
| `strategy.powerup` | Session analyzer, profiler | Booster use count and booster-dependent archetype signal. |
| `strategy.sequence_match` | Session analyzer | Sequence match rate. |
| `meta.attempt` | Session analyzer | Attempt count only. |
| `meta.session_gap` | Session analyzer, player model, profiler | Session gap context and rating deviation decay. |
| `meta.abandoned` | `DDAService`, session analyzer | Marks explicit abandonment and coerces the eventual outcome to `Abandoned`. |
| `input.accuracy` | Session analyzer | Enriches skill score and frustration scoring. |
| `input.rejected` | Flow detector | Real-time engagement penalty. |
| `session.started`, `session.ended`, `session.outcome` | Recorded by `DDAService` | Session lifecycle bookkeeping. |

### Declared but Still Analytics-Only

These keys still exist in `SignalKeys`, but only `strategy.stored` remains outside the runtime decision path:

- `strategy.stored`

## Outputs

| Output | Type | What You Get |
|---|---|---|
| Current flow | `FlowReading` | `State`, `Confidence`, `TempoScore`, `EfficiencyScore`, `EngagementScore`, `SessionTime` |
| Next-level proposal | `AdjustmentProposal` | `Deltas`, `Confidence`, `Reason`, `DetectedState`, `Timing` |
| Player profile | `PlayerSkillProfile` | Rating, deviation, volatility, averages, recent history |
| Archetype reading | `PlayerArchetypeReading` | Primary and secondary archetype scores |
| Debug snapshot | `DDADebugData` | Combined runtime state for tools and debugging |

## Current Rule Set

Built in by default:
- `FlowChannelRule`
- `StreakDamperRule`
- `FrustrationReliefRule`
- `NewPlayerRule`
- `SessionFatigueRule`
- `CooldownRule`

Additional rules can be registered during setup:

```csharp
dda.RegisterRule(new MyCustomRule());
```

Rule packs and level-type semantics overrides can also be registered during setup:

```csharp
dda.RegisterRuleProvider(new MyRulePack());
dda.RegisterLevelTypeConfigProvider(new MyLevelTypeConfigProvider());
```

Current rule semantics:
- Duplicate parameter deltas are not summed. Cadence keeps the single largest absolute delta per parameter key.
- Sawtooth scaling multiplies the proposed change, not the absolute value.
- Clamping is percentage based, relative to the current numeric value.

## Level Types and Scheduling

Current built-in level types:
- `Standard`
- `MoveLimited`
- `TimeLimited`
- `GoalCollection`
- `Boss`
- `Breather`
- `Tutorial`

Use the explicit overload as the supported path:

```csharp
var proposal = dda.GetProposal(nextLevelParams, nextLevelType, nextLevelIndex);
```

Cadence now requires the explicit `GetProposal(nextLevelParams, nextLevelType, nextLevelIndex)` path so the next level type is always unambiguous.

## Known Limitations

1. Cadence now supports scalar parameter polarity through `LevelTypeConfig.ParameterSemanticsEntries`.
   Built-in move-limited and time-limited defaults use this, but the system still does not understand board topology, blockers, or other authored puzzle semantics.

2. The runtime is not currently a board-aware puzzle balancer.
   It adjusts scalar parameters; it does not understand blockers, board topology, spawn tables, color distribution, or scripted content.

3. The extension surface is stronger, but still not full SDK/plugin architecture.
   `IDDAService` now supports single rules, rule-pack providers, and level-type config providers. It still does not provide packaged plugin discovery or live config injection.

4. Custom scalar keys still need explicit semantics metadata.
   Built-in puzzle defaults are wired, but custom parameters still need `ParameterSemanticsEntries` if their numeric direction is not "higher = harder."

5. Mid-session adaptation is observability-first.
   The flow detector updates live, but Cadence does not independently issue or apply interventions during play.

## Editor Tools

Available editor tooling:
- Setup Wizard
- Create Manager in Scene
- Debug Window
- Signal Replay
- Difficulty Curve Preview
- Sandbox Dashboard
- Scenario Simulator
- Flow State Visualizer

These tools are a real strength of the project, but `ScenarioSimulator`, `SandboxDashboard`, and `CadenceUpdateChecker` are large monoliths and are good refactor targets.

## Docs

- [Package README](./Assets/Cadence/README.md)
- [Current-State PRD](./docs/cadence-prd.md)
- [Event Mapping](./docs/dda-event-mapping.md)
- [Casual-Tile Audit](./docs/cadence-4-agent-mobile-pop-audit-2026-03-04.md)

## Bottom Line

Cadence is now in a workable state for a first internal scalar-puzzle project. It is still not world-class for mobile puzzle production, and it is still not ready to publish as a broad "any puzzle game" SDK without broader puzzle-content semantics and more polished external packaging.
