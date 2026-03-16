# Cadence DDA - Event Mapping

This document reflects the current `v1.2.1` runtime. It separates:
- events and signals Cadence actually uses today
- events that are useful for analytics but not consumed by the current runtime
- contract details that matter for puzzle integrations

## Current Priority Matrix

| Priority | Event / Signal Family | Status | Why |
|---|---|---|---|
| P0 | Session lifecycle | Required | Without session boundaries, Cadence cannot analyze or update the profile. |
| P0 | Per-move quality | Required | Real-time flow and session efficiency depend on it. |
| P1 | Pause / hesitation / booster enrichment | Recommended | Improves frustration and archetype signals. |
| P2 | Explicit interval / accuracy / resource efficiency / abandonment keys | Supported enrichment | Improves tempo, skill, frustration, and explicit-quit handling when you emit them. |

## Required Game Events

### 1. Session Start

Game event examples:
- `song_start`
- `level_start`

Cadence mapping:

| Game Data | Cadence Use |
|---|---|
| `level_id` | `BeginSession(levelId, ...)` |
| `level_type` | Pass explicit `LevelType` to `BeginSession(...)` |
| `attempt` | Optional `meta.attempt` enrichment |
| baseline level params | `levelParams` dictionary |

Example:

```csharp
_dda.BeginSession(levelId, levelParams, levelType);

if (attemptCount > 0)
{
    _dda.RecordSignal(SignalKeys.AttemptNumber,
        attemptCount,
        SignalTier.RetryMeta);
}
```

### 2. Per-Move Gameplay Event

Game event examples:
- `tile_tap`
- `move_committed`
- `word_submitted`
- `merge_resolved`

This is the most important integration event.

Current Cadence contract:
- emit `MoveExecuted` every move
- emit `MoveOptimal` every move with `1f` for good play and `0f` for bad play
- optionally emit `MoveWaste` for bad moves
- optionally emit `HesitationTime`
- optionally emit `ProgressDelta`

Example:

```csharp
_dda.RecordSignal(SignalKeys.MoveExecuted, 1f,
    SignalTier.DecisionQuality, moveIndex);

_dda.RecordSignal(SignalKeys.MoveOptimal,
    wasOptimal ? 1f : 0f,
    SignalTier.DecisionQuality,
    moveIndex);

if (!wasOptimal)
{
    _dda.RecordSignal(SignalKeys.MoveWaste, 1f,
        SignalTier.DecisionQuality, moveIndex);
}

if (progressDelta > 0f)
{
    _dda.RecordSignal(SignalKeys.ProgressDelta,
        progressDelta,
        SignalTier.DecisionQuality,
        moveIndex);
}

if (hesitationSeconds > 0f)
{
    _dda.RecordSignal(SignalKeys.HesitationTime,
        hesitationSeconds,
        SignalTier.BehavioralTempo,
        moveIndex);
}
```

Important:
- Do not emit `MoveOptimal` only for successful moves.
- The current flow detector pushes efficiency values only from `MoveOptimal`.
- If you emit only `MoveWaste` on bad moves, live efficiency is inflated.

### 3. Session End

Game event examples:
- `song_result`
- `level_result`

Cadence mapping:

| Game Data | Cadence Use |
|---|---|
| win / lose / abandon | `EndSession(SessionOutcome.XXX)` |
| next level params | input to `GetProposal(...)` |
| next level type | explicit `GetProposal(nextParams, nextType, nextIndex)` |

Example:

```csharp
_dda.EndSession(won ? SessionOutcome.Win : SessionOutcome.Lose);

AdjustmentProposal proposal = _dda.GetProposal(
    nextLevelParams,
    nextLevelType,
    nextLevelIndex);
```

## Recommended Enrichment Events

### Pause or Background

Map a pause event to:

```csharp
_dda.RecordSignal(SignalKeys.PauseTriggered, 1f, SignalTier.BehavioralTempo);
```

Used by:
- live engagement penalty
- session pause count

### Idle / Hesitation

Map an idle event to:

```csharp
_dda.RecordSignal(SignalKeys.HesitationTime,
    idleSeconds,
    SignalTier.BehavioralTempo);
```

Used by:
- session summary hesitation field

Current limitation:
- `HesitationTime` is part of session analysis, not a live flow input.

### Explicit Tempo Interval

Map explicit interval timing to:

```csharp
_dda.RecordSignal(SignalKeys.InterMoveInterval,
    intervalSeconds,
    SignalTier.BehavioralTempo,
    moveIndex);
```

Used by:
- live tempo consistency
- session interval mean/variance

Important:
- once a session emits `tempo.interval`, Cadence stops deriving tempo from `MoveExecuted` timestamps for that session
- non-positive interval values are ignored

### Input Accuracy

```csharp
_dda.RecordSignal(SignalKeys.InputAccuracy,
    accuracy01,
    SignalTier.RawInput,
    moveIndex);
```

Used by:
- session summary `InputAccuracy01`
- skill-score enrichment
- frustration enrichment

### Resource Efficiency

```csharp
_dda.RecordSignal(SignalKeys.ResourceEfficiency,
    efficiency01,
    SignalTier.DecisionQuality,
    moveIndex);
```

Used by:
- session summary `ResourceEfficiency01`
- `EffectiveEfficiency01`
- Glicko/player-profile efficiency tracking
- profiler trend inputs

### Explicit Abandonment

```csharp
_dda.RecordSignal(SignalKeys.LevelAbandoned, 1f, SignalTier.RetryMeta);
```

Used by:
- explicit abandon flag in `DDAService`
- outcome coercion to `SessionOutcome.Abandoned` when you later call `EndSession(...)`

### Booster Usage

Map booster use to:

```csharp
_dda.RecordSignal(SignalKeys.PowerUpUsed, 1f, SignalTier.StrategicPattern);
```

Used by:
- session summary booster count
- booster-dependent archetype classification

### Combo or Sequence Success

Map combo or planned-chain success to:

```csharp
_dda.RecordSignal(SignalKeys.SequenceMatch, 1f, SignalTier.StrategicPattern);
```

Used by:
- session sequence match rate

## Signals the Runtime Consumes Today

| Signal | Live Flow | Session Analyzer | Rules / Profiling |
|---|---|---|---|
| `move.executed` | Yes | Yes | Indirect |
| `move.optimal` | Yes | Yes | Indirect |
| `move.waste` | No | Yes | Indirect via frustration |
| `resource.efficiency` | No | Yes | Yes |
| `progress.delta` | Yes | Yes | Indirect |
| `tempo.interval` | Yes | Yes | Indirect |
| `tempo.hesitation` | No | Yes | Indirect |
| `tempo.pause` | Yes | Yes | Indirect |
| `strategy.powerup` | No | Yes | Yes |
| `strategy.sequence_match` | No | Yes | Indirect |
| `meta.attempt` | No | Yes | Stored only |
| `meta.session_gap` | No | Yes | Yes |
| `meta.abandoned` | No | Yes | Indirect |
| `input.accuracy` | No | Yes | Indirect |
| `input.rejected` | Yes | No | Indirect |
| `session.outcome` | No | Yes | Yes |

## Signals Declared but Not Consumed Today

| Signal | Current Reality |
|---|---|
| `strategy.stored` | Declared only. |

## Puzzle-Specific Integration Notes

1. Use the explicit `GetProposal(...)` overload for mixed-type progressions.
   It is now the only supported proposal path.

2. Treat custom parameter meaning as game-owned.
   Built-in move/time/goal defaults have scalar polarity metadata now, but your custom keys still need `ParameterSemanticsEntries` if bigger numbers are not simply harder.

3. Keep your own board-aware analytics.
   If your game difficulty depends on blockers, color distribution, spawn patterns, or tile layout, Cadence will not infer those relationships for you.

4. Abandonment can be signaled explicitly or ended directly.
   If the player quits, either call:

```csharp
_dda.EndSession(SessionOutcome.Abandoned);
```

   or record `meta.abandoned` and then end the session normally. Cadence will coerce the outcome to `Abandoned`.

## Minimal Working Contract

If you want the smallest integration that still matches the current runtime:

1. Start each attempt with `BeginSession(...)`
2. Emit `MoveExecuted` every move
3. Emit `MoveOptimal` with `1` or `0` every move
4. Call `Tick(...)` each frame
5. End the session with `EndSession(...)`
6. Request the next-level proposal with the explicit `GetProposal(...)` overload
