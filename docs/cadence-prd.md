# Cadence - Current-State PRD

| | |
|---|---|
| Version | 1.2.1 |
| Status | Current-state technical PRD |
| Date | 2026-03-11 |
| Scope | Reflects the implementation that exists in this repository today |

## Product Definition

Cadence is a client-side Unity DDA runtime that turns session events and gameplay signals into:
- a live `FlowReading`
- an updated `PlayerSkillProfile`
- a caller-applied `AdjustmentProposal`

The current implementation is strongest for single-player, level-based casual puzzle games. It should not be described as a universal any-genre or any-puzzle SDK in its present form.

## Supported Use Case

Best current fit:
- Single-player mobile puzzle or casual games
- Clear level/session boundaries
- Numeric level parameters that can be changed between attempts
- Per-move quality signals that can be emitted as `MoveOptimal = 1` or `0`

Weak current fit:
- Games whose main difficulty comes from board topology, blocker interactions, spawn tables, or authored content rather than scalar values
- Studio consumers who need a public plugin surface for custom rules
- Integrations that expect custom scalar keys to work safely without configuring `ParameterSemanticsEntries`

## Non-Goals

Current non-goals:
- Backend services or remote config
- Automatic application of difficulty changes
- Automatic mid-session interventions
- Multiplayer matchmaking
- Content generation or board layout synthesis

## Runtime Pipeline

1. `BeginSession(...)`
   Resets the collector and flow detector, stores the current level type, and records `session.started`.

2. `RecordSignal(...)`
   Appends signals to the active `SignalBatch` and the recent-signal ring buffer.

3. `Tick(deltaTime)`
   Processes new ring-buffer entries only. Current real-time flow uses:
   - `move.executed`
   - `move.optimal`
   - `tempo.interval` when present, otherwise timestamp-derived move timing
   - `progress.delta`
   - `tempo.pause`
   - `input.rejected`

4. `EndSession(outcome)`
   Records `session.outcome` and `session.ended`, analyzes the batch, updates the Glicko-2 profile, and advances the contiguous play-session fatigue counter.

5. `GetProposal(...)`
   Builds an `AdjustmentContext`, classifies the player archetype, runs built-in rules, merges duplicate deltas by largest absolute change, applies sawtooth scaling, and clamps deltas.

6. Game applies proposal
   Cadence proposes. The host game owns final application.

## Inputs

### Required

| Input | Why It Exists |
|---|---|
| `BeginSession(levelId, levelParams, levelType)` | Starts a tracked attempt |
| `MoveExecuted` | Move count and derived timing |
| `MoveOptimal` with `1` or `0` every move | Real-time efficiency and session move efficiency |
| `Tick(deltaTime)` | Live flow state updates |
| `EndSession(outcome)` | Session summary and profile update |
| `GetProposal(nextLevelParams, nextLevelType, nextLevelIndex)` | Next-level adjustment generation |

### Consumed Signals

| Signal | Current Consumer |
|---|---|
| `move.executed` | Flow detector, session analyzer |
| `move.optimal` | Flow detector, session analyzer |
| `move.waste` | Session analyzer |
| `resource.efficiency` | Session analyzer, player model, profiler |
| `progress.delta` | Flow detector, session analyzer |
| `tempo.interval` | Flow detector, session analyzer |
| `tempo.hesitation` | Session analyzer |
| `tempo.pause` | Flow detector, session analyzer |
| `strategy.powerup` | Session analyzer, profiler |
| `strategy.sequence_match` | Session analyzer |
| `meta.attempt` | Session analyzer |
| `meta.session_gap` | Session analyzer, player model, profiler |
| `meta.abandoned` | `DDAService`, session analyzer |
| `input.accuracy` | Session analyzer |
| `input.rejected` | Flow detector |
| `session.started`, `session.ended`, `session.outcome` | DDAService infrastructure |

### Declared but Not Consumed

The following key exists in `SignalKeys` but is not part of the current runtime decision path:
- `strategy.stored`

## Outputs

| Output | Notes |
|---|---|
| `FlowReading` | Real-time state, confidence, tempo, efficiency, engagement |
| `AdjustmentProposal` | Proposed deltas, confidence, reason, detected state, timing |
| `PlayerSkillProfile` | Glicko-2 rating state plus recent history and averages |
| `PlayerArchetypeReading` | Primary and secondary archetype labels and confidence |
| `DDADebugData` | Snapshot used by editor tooling |

## Current Adjustment Model

Built-in rules:
- `FlowChannelRule`
- `StreakDamperRule`
- `FrustrationReliefRule`
- `NewPlayerRule`
- `SessionFatigueRule`
- `CooldownRule`

Current extension reality:
- `AdjustmentEngine.AddRule(...)` exists.
- `IDDAService.RegisterRule(...)` exposes single-rule registration.
- `IDDAService.RegisterRuleProvider(...)` exposes rule-pack registration.
- `IDDAService.RegisterLevelTypeConfigProvider(...)` exposes per-project level-type/semantics overrides.
- Full plugin packaging and live-config injection still do not exist.

## Current Constraints and Risks

1. Parameter semantics are now modeled for scalar keys.
   Built-in `move_limit`, `time_limit`, and `goal_count` defaults have polarity and optional bounds, but Cadence still does not model board or authored-content semantics.

2. The simple `GetProposal(nextLevelParams)` overload has been removed.
   Proposal generation now always requires explicit next-level type context.

3. Tempo naming is broader than speed.
   Real-time `TempoScore` is derived from consistency of move intervals, not pure move speed.

4. Real-time flow is stronger than real-time adaptation.
   The detector updates during play, but proposals are still generated only when the caller requests them.

5. Secondary parameter metadata is not yet actioned.
   `LevelTypeDefaults` fills `SecondaryParameterKeys`, but rules do not use that list.

## Product Positioning

Reasonable to claim today:
- Internal DDA foundation for casual puzzle games
- Strong editor-assisted evaluation workflow
- Solid low-allocation runtime architecture

Not reasonable to claim today:
- World-class casual-tile DDA
- Publish-ready for every mobile puzzle game
- Plug-and-play modular SDK for external studios
- Board-aware support for authored puzzle content

## Planned Priorities

1. Flow detector correctness cleanup
   Keep `MoveOptimal` semantics strict and separate consistency from speed if the product keeps using "tempo" language.

2. Public extension surface
   Build on the new provider hooks with safer live-config injection and better packaging for external consumers.

3. Editor decomposition
   Break up large editor windows into shared helpers and smaller views.

4. One source of truth
   Keep README, package README, PRD, event mapping, and inline code comments aligned with the same contract.
