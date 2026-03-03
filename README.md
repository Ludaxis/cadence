# Cadence

**Game-agnostic Dynamic Difficulty Adjustment SDK for Unity.**

Signal-based player modeling, real-time flow detection, Glicko-2 skill tracking, and adaptive difficulty proposals. Drop it into any game — puzzle, action, RPG — and let Cadence keep every player in the flow channel.

---

## Table of Contents

- [Installation](#installation)
- [Quick Start — CadenceManager (Recommended)](#quick-start--cadencemanager-recommended)
- [Quick Start — Code-Only](#quick-start--code-only)
- [Core Concepts](#core-concepts)
  - [The Pipeline](#the-pipeline)
  - [Sessions](#sessions)
  - [Signals](#signals)
  - [Flow Detection](#flow-detection)
  - [Player Model (Glicko-2)](#player-model-glicko-2)
  - [Adjustment Proposals](#adjustment-proposals)
  - [Level Types & Sawtooth Scheduling](#level-types--sawtooth-scheduling)
  - [Player Archetypes](#player-archetypes)
- [Integration Guide](#integration-guide)
  - [Step 1: Install Cadence](#step-1-install-cadence)
  - [Step 2: Create Configuration](#step-2-create-configuration)
  - [Step 3: Add CadenceManager to Your Scene](#step-3-add-cadencemanager-to-your-scene)
  - [Step 4: Record Signals During Gameplay](#step-4-record-signals-during-gameplay)
  - [Step 5: Get Proposals Between Levels](#step-5-get-proposals-between-levels)
  - [Step 6: Apply Proposals to Your Game](#step-6-apply-proposals-to-your-game)
- [API Reference](#api-reference)
  - [IDDAService](#iddaservice)
  - [CadenceManager](#cadencemanager)
  - [Signal Keys](#signal-keys)
  - [Data Types](#data-types)
- [Configuration Reference](#configuration-reference)
- [Editor Tools](#editor-tools)
- [Custom Adjustment Rules](#custom-adjustment-rules)
- [Profile Persistence](#profile-persistence)
- [Samples](#samples)
- [Architecture](#architecture)
- [Testing](#testing)
- [FAQ](#faq)
- [License](#license)

---

## Installation

### Option 1: Git URL (recommended)

Add to your project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.ludaxis.cadence": "https://github.com/Ludaxis/cadence.git?path=Assets/Cadence"
  }
}
```

Pin a specific version:

```json
"com.ludaxis.cadence": "https://github.com/Ludaxis/cadence.git?path=Assets/Cadence#v1.2.0"
```

### Option 2: Drag and drop

Copy the `Assets/Cadence/` folder into your project's `Assets/Plugins/Cadence/`.

### Option 3: Local package reference

Clone this repo and reference it locally:

```json
"com.ludaxis.cadence": "file:../../cadence/Assets/Cadence"
```

### Option 4: Open as Unity project

Clone and open the repo root in Unity Hub (2021.3+). The project is ready for development and testing.

---

## Quick Start — CadenceManager (Recommended)

The fastest way to get Cadence running. Zero boilerplate.

**1. Create config assets** via the menu: `Assets > Create > Cadence > DDA Config` (also create and wire the sub-configs using the Setup Wizard: `Cadence > Setup Wizard`).

**2. Add CadenceManager to your scene** via `Cadence > Create Manager in Scene`. It auto-finds your DDAConfig.

**3. Use it from any script:**

```csharp
using Cadence;
using System.Collections.Generic;

public class MyGameManager : MonoBehaviour
{
    void StartLevel(string levelId, Dictionary<string, float> levelParams)
    {
        // CadenceManager handles Tick() and profile persistence automatically
        CadenceManager.Service.BeginSession(levelId, levelParams);
    }

    void OnPlayerMove(bool wasOptimal, float hesitation)
    {
        var dda = CadenceManager.Service;
        dda.RecordSignal(SignalKeys.MoveExecuted);
        dda.RecordSignal(wasOptimal ? SignalKeys.MoveOptimal : SignalKeys.MoveWaste);
        dda.RecordSignal(SignalKeys.HesitationTime, hesitation, SignalTier.BehavioralTempo);
    }

    void EndLevel(bool won, Dictionary<string, float> nextLevelParams)
    {
        var dda = CadenceManager.Service;
        dda.EndSession(won ? SessionOutcome.Win : SessionOutcome.Lose);

        // Get difficulty proposal for the next level
        var proposal = dda.GetProposal(nextLevelParams);
        foreach (var delta in proposal.Deltas)
        {
            nextLevelParams[delta.ParameterKey] = delta.ProposedValue;
        }
    }
}
```

That's it. CadenceManager handles:
- Calling `Tick()` every frame (using `Time.unscaledDeltaTime`)
- Loading/saving the player profile to PlayerPrefs
- Surviving scene loads (`DontDestroyOnLoad`)
- Connecting to the Debug Window and Flow Visualizer automatically

---

## Quick Start — Code-Only

For maximum control, skip CadenceManager and manage the service yourself:

```csharp
using Cadence;
using System.Collections.Generic;

// Create the service
var config = Resources.Load<DDAConfig>("DDAConfig");
IDDAService dda = new DDAService(config);

// Optional: load a saved profile
string json = PlayerPrefs.GetString("cadence_profile", null);
if (!string.IsNullOrEmpty(json)) dda.LoadProfile(json);

// Start a session
dda.BeginSession("level_42", new Dictionary<string, float>
{
    { "enemy_count", 12f },
    { "time_limit", 90f }
});

// Record signals during gameplay
dda.RecordSignal(SignalKeys.MoveExecuted);
dda.RecordSignal(SignalKeys.MoveOptimal);
dda.RecordSignal(SignalKeys.HesitationTime, 1.8f, SignalTier.BehavioralTempo);

// Tick every frame for real-time flow detection
dda.Tick(Time.deltaTime);
FlowReading flow = dda.CurrentFlow;
// flow.State => Flow | Boredom | Anxiety | Frustration

// End session and get proposal
dda.EndSession(SessionOutcome.Win);
AdjustmentProposal proposal = dda.GetProposal(nextLevelParams);

// Save profile
PlayerPrefs.SetString("cadence_profile", dda.SaveProfile());
```

---

## Core Concepts

### The Pipeline

```
Signals  →  Analysis  →  Player Model  →  Flow Detection  →  Adjustment  →  Proposal
(input)     (aggregate)   (Glicko-2)       (real-time)        (rules)        (output)
```

Every frame during gameplay, Cadence collects signals, updates running statistics, and classifies the player's psychological state. Between sessions, it evaluates adjustment rules and proposes difficulty changes.

### Sessions

A **session** is one attempt at a level. Everything in Cadence revolves around sessions:

```csharp
dda.BeginSession("level_42", levelParams);   // Start tracking
// ... gameplay happens, signals are recorded ...
dda.EndSession(SessionOutcome.Win);          // Finalize and update player model
```

- You must call `BeginSession` before recording signals
- You must call `EndSession` before getting a proposal
- If you call `BeginSession` while a session is active, the previous session is auto-ended as `Abandoned`

### Signals

**Signals** are gameplay events that feed the DDA system. Each signal has a **key**, **value**, and **tier**:

```csharp
dda.RecordSignal(SignalKeys.MoveExecuted);                                    // Tier 0
dda.RecordSignal(SignalKeys.HesitationTime, 2.1f, SignalTier.BehavioralTempo); // Tier 1
dda.RecordSignal(SignalKeys.PowerUpUsed, 1f, SignalTier.StrategicPattern);     // Tier 2
```

Signals are stored in a fixed-size ring buffer (default 512 entries) — no allocations, no GC pressure.

**Signal Tiers** control how the system weights different types of information:

| Tier | Name | What It Captures | Example Keys |
|------|------|-----------------|-------------|
| 0 | **DecisionQuality** | Was the move good or bad? | `move.optimal`, `move.waste`, `progress.delta` |
| 1 | **BehavioralTempo** | How fast is the player? | `tempo.interval`, `tempo.hesitation`, `tempo.pause` |
| 2 | **StrategicPattern** | What strategy are they using? | `strategy.powerup`, `strategy.sequence_match` |
| 3 | **RetryMeta** | Session-level retry behavior | `meta.attempt`, `meta.session_gap` |
| 4 | **RawInput** | Low-level input accuracy | `input.accuracy`, `input.rejected` |

**Minimum viable signals:** Just record `MoveExecuted` and `MoveOptimal` / `MoveWaste`. That's enough for the core pipeline to work. Add tempo and strategic signals for better accuracy.

### Flow Detection

The flow detector runs every frame (via `Tick()`) and classifies the player into one of four states:

| State | What It Means | System Response |
|-------|--------------|-----------------|
| **Flow** | Challenge matches skill — the player is "in the zone" | Maintain current difficulty |
| **Boredom** | Too easy — high efficiency, fast tempo, low engagement | Increase difficulty |
| **Anxiety** | Too hard — low efficiency, slow tempo, rising hesitation | Decrease difficulty slightly |
| **Frustration** | Repeated failure, giving up — pauses, rejected inputs | Decrease difficulty significantly |

Read the current state anytime:

```csharp
FlowReading flow = dda.CurrentFlow;
Debug.Log($"State: {flow.State}, Confidence: {flow.Confidence:P0}");
Debug.Log($"Tempo: {flow.TempoScore:F2}, Efficiency: {flow.EfficiencyScore:F2}");
```

The detector uses sliding windows with configurable thresholds and hysteresis to prevent state flickering.

### Player Model (Glicko-2)

Each player gets a skill profile that evolves over multiple sessions:

| Component | What It Means | Default |
|-----------|--------------|---------|
| **Rating** | Estimated skill level (like Elo, but better) | 1500 |
| **Deviation** | Uncertainty — how sure are we? | 350 |
| **Volatility** | How erratic is the player's performance? | 0.06 |

```csharp
PlayerSkillProfile profile = dda.PlayerProfile;
float confidence = profile.Confidence01; // 0 = no data, ~1 = stable estimate
```

Key behaviors:
- Rating updates after each session based on outcome vs. expected performance
- Deviation increases when the player is inactive (configurable time decay)
- Confidence grows as more sessions are completed
- The model persists automatically when using CadenceManager

### Adjustment Proposals

After a session ends, ask Cadence what to change for the next level:

```csharp
dda.EndSession(SessionOutcome.Win);

var proposal = dda.GetProposal(nextLevelParams);
// proposal.Confidence  => 0..1 (how certain the system is)
// proposal.Reason      => human-readable explanation
// proposal.DetectedState => the flow state that triggered this
// proposal.Deltas      => list of parameter changes

foreach (var delta in proposal.Deltas)
{
    Debug.Log($"{delta.ParameterKey}: {delta.CurrentValue} → {delta.ProposedValue} [{delta.RuleName}]");
    nextLevelParams[delta.ParameterKey] = delta.ProposedValue;
}
```

The engine evaluates four built-in rules:

| Rule | What It Does |
|------|-------------|
| **FlowChannelRule** | Nudges parameters toward the flow channel based on detected state |
| **FrustrationReliefRule** | Larger relief when frustration is detected |
| **StreakDamperRule** | Dampens adjustments during win/loss streaks to prevent overcorrection |
| **CooldownRule** | Prevents adjustments from firing too frequently |

**Cadence proposes, your game decides.** You always control which proposals to accept and how to apply them.

### Level Types & Sawtooth Scheduling

Cadence supports different level types with per-type adjustment behavior:

| Level Type | DDA Behavior |
|-----------|-------------|
| `Standard` | Full DDA active |
| `Tutorial` | DDA disabled (let the designer control) |
| `Boss` | DDA active, higher difficulty baseline |
| `Breather` | DDA active, lower difficulty baseline |
| `MoveLimited` | Standard DDA with move-based constraints |
| `TimeLimited` | Standard DDA with time-based constraints |

The **sawtooth scheduler** creates periodic difficulty waves across your level sequence:

```
Difficulty
    ▲
    │    /\      /\      /\
    │   /  \    /  \    /  \
    │  / Boss\ / Boss\ / Boss\
    │ /      \/      \/      \
    │/ Breather  Breather  Breather
    └──────────────────────────────→ Level Index
```

```csharp
// Get the scheduled multiplier for a specific level
float multiplier = dda.GetTargetMultiplier(levelIndex);

// Get a proposal with full level-type context
var proposal = dda.GetProposal(nextLevelParams, LevelType.Standard, levelIndex);
```

### Player Archetypes

After enough sessions, Cadence classifies players into archetypes that influence adjustment behavior:

| Archetype | Characteristics |
|-----------|----------------|
| **SpeedRunner** | Fast tempo, high efficiency, low hesitation |
| **CarefulThinker** | Slow tempo, high efficiency, methodical |
| **StrugglingLearner** | Low efficiency, high variance, improving |
| **BoosterDependent** | Relies heavily on power-ups |
| **ChurnRisk** | Low engagement, long session gaps, declining performance |

```csharp
var archetype = dda.CurrentArchetype;
Debug.Log($"Primary: {archetype.Primary} ({archetype.PrimaryConfidence:P0})");
Debug.Log($"Secondary: {archetype.Secondary} ({archetype.SecondaryConfidence:P0})");
```

---

## Integration Guide

### Step 1: Install Cadence

Add the package via one of the [installation methods](#installation) above.

### Step 2: Create Configuration

**Option A — Setup Wizard (recommended):**

Open `Cadence > Setup Wizard` from the Unity menu. It creates and wires all config assets automatically.

**Option B — Manual:**

1. `Assets > Create > Cadence > DDA Config` — the root config
2. `Assets > Create > Cadence > Player Model Config` — Glicko-2 parameters
3. `Assets > Create > Cadence > Flow Detector Config` — flow thresholds
4. `Assets > Create > Cadence > Adjustment Engine Config` — rule settings
5. Wire the sub-configs into the DDA Config's Inspector fields

Optionally create a `Cadence > Sawtooth Curve Config` for difficulty wave scheduling.

### Step 3: Add CadenceManager to Your Scene

`Cadence > Create Manager in Scene`

This creates a GameObject with the `CadenceManager` component. It:
- Auto-assigns the DDAConfig it finds in your project
- Persists across scene loads (`DontDestroyOnLoad`)
- Auto-ticks the flow detector every frame
- Auto-saves/loads the player profile via PlayerPrefs

**Inspector settings:**

| Field | Default | Description |
|-------|---------|-------------|
| Config | *(auto-assigned)* | Your DDAConfig asset |
| Auto-Save Profile | `true` | Automatically persist profile to PlayerPrefs |
| Profile Key | `Cadence_PlayerProfile` | PlayerPrefs key for the saved profile |
| Enable File Storage | `true` | Save signal batches to disk for replay/debugging |
| Verbose Logging | `false` | Log initialization and save events |

### Step 4: Record Signals During Gameplay

In your gameplay code, record signals as the player acts:

```csharp
using Cadence;

public class MyGameplay : MonoBehaviour
{
    void OnPlayerTap(bool wasCorrect, float hesitationSeconds)
    {
        var dda = CadenceManager.Service;
        if (dda == null || !dda.IsSessionActive) return;

        dda.RecordSignal(SignalKeys.MoveExecuted);

        if (wasCorrect)
            dda.RecordSignal(SignalKeys.MoveOptimal);
        else
            dda.RecordSignal(SignalKeys.MoveWaste);

        dda.RecordSignal(SignalKeys.HesitationTime, hesitationSeconds,
            SignalTier.BehavioralTempo);
    }

    void OnBoosterUsed()
    {
        CadenceManager.Service?.RecordSignal(SignalKeys.PowerUpUsed, 1f,
            SignalTier.StrategicPattern);
    }

    void OnPlayerPaused()
    {
        CadenceManager.Service?.RecordSignal(SignalKeys.PauseTriggered, 1f,
            SignalTier.BehavioralTempo);
    }
}
```

**Tip:** The minimum viable integration is just `MoveExecuted` + `MoveOptimal`/`MoveWaste`. Everything else improves accuracy but isn't required.

### Step 5: Get Proposals Between Levels

After a level ends, get the adjustment proposal:

```csharp
void OnLevelComplete(bool won)
{
    var dda = CadenceManager.Service;

    // End the current session
    var outcome = won ? SessionOutcome.Win : SessionOutcome.Lose;
    dda.EndSession(outcome);

    // Get proposal for the next level
    var nextParams = GetNextLevelBaseParams();
    var proposal = dda.GetProposal(nextParams, LevelType.Standard, nextLevelIndex);

    if (proposal != null && proposal.Deltas.Count > 0)
    {
        Debug.Log($"DDA Proposal: {proposal.Reason} (confidence: {proposal.Confidence:P0})");
    }
}
```

### Step 6: Apply Proposals to Your Game

Proposals are suggestions — you decide how to apply them:

```csharp
// Option A: Apply all proposals directly
foreach (var delta in proposal.Deltas)
{
    nextLevelParams[delta.ParameterKey] = delta.ProposedValue;
}

// Option B: Apply only high-confidence proposals
foreach (var delta in proposal.Deltas)
{
    if (proposal.Confidence > 0.5f)
        nextLevelParams[delta.ParameterKey] = delta.ProposedValue;
}

// Option C: Apply partially (blend toward proposed value)
foreach (var delta in proposal.Deltas)
{
    float current = nextLevelParams[delta.ParameterKey];
    nextLevelParams[delta.ParameterKey] = Mathf.Lerp(current, delta.ProposedValue, 0.5f);
}
```

---

## API Reference

### IDDAService

The core interface. Access via `CadenceManager.Service` or `new DDAService(config)`.

```csharp
public interface IDDAService
{
    // Session lifecycle
    void BeginSession(string levelId, Dictionary<string, float> levelParameters);
    void BeginSession(string levelId, Dictionary<string, float> levelParameters, LevelType type);
    void EndSession(SessionOutcome outcome);
    bool IsSessionActive { get; }

    // Signal recording (only works during an active session)
    void RecordSignal(string key, float value = 1f,
        SignalTier tier = SignalTier.DecisionQuality, int moveIndex = -1);

    // Real-time flow detection (call each frame during gameplay)
    void Tick(float deltaTime);
    FlowReading CurrentFlow { get; }

    // Between-session adjustment proposals
    AdjustmentProposal GetProposal(Dictionary<string, float> nextLevelParameters);
    AdjustmentProposal GetProposal(Dictionary<string, float> nextLevelParameters,
        LevelType nextLevelType, int nextLevelIndex = -1);

    // Player profile
    PlayerSkillProfile PlayerProfile { get; }
    PlayerArchetypeReading CurrentArchetype { get; }

    // Profile serialization
    string SaveProfile();
    void LoadProfile(string json);

    // Difficulty scheduling
    float GetTargetMultiplier(int levelIndex);

    // Debug
    DDADebugData GetDebugSnapshot();
}
```

### CadenceManager

Singleton MonoBehaviour that wraps `DDAService`. Optional — the code-only workflow remains fully supported.

| Member | Type | Description |
|--------|------|-------------|
| `CadenceManager.Instance` | `CadenceManager` | The singleton instance (null if none) |
| `CadenceManager.Service` | `IDDAService` | The managed service (null if not initialized) |
| `CadenceManager.OnServiceInitialized` | `event Action<IDDAService>` | Fires once after service is created and profile is loaded |
| `.IsInitialized` | `bool` | Whether the service is ready |
| `.ResetProfile()` | `void` | Deletes saved profile and reinitializes with fresh data |

**Lifecycle:**
- `Awake()` — singleton check → `DontDestroyOnLoad` → creates `DDAService` → loads profile → fires event
- `Update()` — calls `Tick(Time.unscaledDeltaTime)` (zero-cost when no session is active)
- `OnApplicationPause(true)` / `OnDestroy()` — saves profile
- Duplicate instances self-destruct in `Awake()`

### Signal Keys

All built-in signal constants in `SignalKeys`:

| Key | Tier | Description |
|-----|------|-------------|
| `MoveExecuted` | DecisionQuality | A move was made (feeds efficiency denominator) |
| `MoveOptimal` | DecisionQuality | The move was strategically optimal |
| `MoveWaste` | DecisionQuality | The move was wasteful |
| `ProgressDelta` | DecisionQuality | Micro-progress per action (0-1) |
| `ResourceEfficiency` | DecisionQuality | Resource economy efficiency (0-1) |
| `InterMoveInterval` | BehavioralTempo | Seconds since previous move |
| `HesitationTime` | BehavioralTempo | Delay before acting (seconds) |
| `PauseTriggered` | BehavioralTempo | Player paused or went idle |
| `PowerUpUsed` | StrategicPattern | Booster / power-up used |
| `SequenceMatch` | StrategicPattern | Planned combo executed correctly |
| `ResourceStored` | StrategicPattern | Resource banked (economy games) |
| `AttemptNumber` | RetryMeta | Current retry count for this level |
| `SessionGapDays` | RetryMeta | Days since last session (auto-recorded) |
| `LevelAbandoned` | RetryMeta | Level quit without finishing |
| `InputAccuracy` | RawInput | Input accuracy metric (0-1) |
| `InputRejected` | RawInput | Invalid input attempted |

You can also define custom string keys:

```csharp
dda.RecordSignal("my_game.combo_broken", 1f, SignalTier.DecisionQuality);
```

### Data Types

| Type | Description |
|------|-------------|
| `FlowReading` | Current flow state + confidence + tempo/efficiency/engagement scores |
| `FlowState` | Enum: `Flow`, `Boredom`, `Anxiety`, `Frustration` |
| `AdjustmentProposal` | Proposed changes: `Deltas`, `Confidence`, `Reason`, `DetectedState`, `Timing` |
| `ParameterDelta` | Single change: `ParameterKey`, `CurrentValue`, `ProposedValue`, `Delta`, `RuleName` |
| `PlayerSkillProfile` | Glicko-2 profile: `Rating`, `Deviation`, `Volatility`, `Confidence01`, `SessionsCompleted`, `AverageEfficiency`, `AverageOutcome`, `RecentHistory` |
| `PlayerArchetypeReading` | Archetype classification: `Primary`, `Secondary` + confidence scores per archetype |
| `SessionOutcome` | Enum: `Win`, `Lose`, `Abandoned` |
| `SignalTier` | Enum: `DecisionQuality`, `BehavioralTempo`, `StrategicPattern`, `RetryMeta`, `RawInput` |
| `LevelType` | Enum: `Standard`, `Tutorial`, `Boss`, `Breather`, `MoveLimited`, `TimeLimited` |
| `DDADebugData` | Full snapshot for editor tools |

---

## Configuration Reference

Create assets via `Assets > Create > Cadence > ...` or use the Setup Wizard (`Cadence > Setup Wizard`).

### DDA Config (root)

| Field | Default | Description |
|-------|---------|-------------|
| PlayerModelConfig | *(required)* | Glicko-2 skill rating settings |
| FlowDetectorConfig | *(required)* | Flow state detection thresholds |
| AdjustmentEngineConfig | *(required)* | Adjustment rule weights and cooldowns |
| SawtoothCurveConfig | *(optional)* | Difficulty wave scheduling. Null = flat difficulty. |
| RingBufferCapacity | 512 | Max in-memory signals per session |
| EnableSignalStorage | true | Persist signals to disk for replay/debugging |
| MaxStoredSessions | 50 | Max signal files on disk (oldest pruned) |
| EnableMidSessionDetection | true | Run flow detector every frame |
| EnableBetweenSessionAdjustment | true | Generate proposals after sessions |

### Player Model Config

| Field | Description |
|-------|-------------|
| InitialRating | Starting Glicko-2 rating (default: 1500) |
| InitialDeviation | Starting uncertainty (default: 350) |
| InitialVolatility | Starting volatility (default: 0.06) |
| Tau | System constant controlling volatility change speed |
| TimeDecayRate | How fast deviation increases during inactivity |

### Flow Detector Config

| Field | Description |
|-------|-------------|
| TempoWindowSize | Sliding window size for tempo metrics |
| EfficiencyWindowSize | Sliding window size for efficiency metrics |
| EngagementWindowSize | Sliding window size for engagement metrics |
| BoredomThreshold | Efficiency above this = boredom candidate |
| AnxietyThreshold | Efficiency below this = anxiety candidate |
| FrustrationThreshold | Compound score above this = frustration |
| HysteresisCount | Consecutive readings before state change |
| WarmupMoves | Minimum moves before detection activates |

### Adjustment Engine Config

| Field | Description |
|-------|-------------|
| Rule weights | Per-rule influence on the final proposal |
| CooldownDuration | Minimum time between adjustments |
| DampingFactor | Global damping multiplier (0-1) |

### Sawtooth Curve Config

| Field | Description |
|-------|-------------|
| WaveLength | Levels per difficulty cycle |
| PeakMultiplier | Boss level difficulty multiplier |
| TroughMultiplier | Breather level difficulty multiplier |

---

## Editor Tools

All accessible from the `Cadence` menu in Unity.

| Tool | Menu Path | Description |
|------|-----------|-------------|
| **Setup Wizard** | `Cadence > Setup Wizard` | Guided creation of all config assets |
| **Create Manager** | `Cadence > Create Manager in Scene` | One-click CadenceManager setup |
| **Debug Window** | `Cadence > Debug Window` | Live flow state, signals, profile, and proposals during Play mode |
| **Signal Replay** | `Cadence > Signal Replay` | Replay recorded signal logs to reproduce DDA behavior |
| **Flow Visualizer** | *(auto-enabled)* | Scene view overlay showing flow state as a colored badge |
| **Difficulty Curve** | `Cadence > Difficulty Curve Preview` | Visualize sawtooth scheduling curve |
| **Sandbox** | `Cadence > Sandbox Dashboard` | Test DDA behavior with simulated sessions |
| **Update Checker** | `Cadence > Check for Updates` | Check for new SDK versions on GitHub |
| **Odin Helper** | `Cadence > Install Odin Inspector` | Guide for installing Odin (enhanced inspectors) |

**Auto-discovery:** The Debug Window and Flow Visualizer automatically connect to CadenceManager when you enter Play mode — no manual `SetService()` call needed.

**Odin Inspector support:** If [Odin Inspector](https://odininspector.com/) is installed, all Cadence configs get grouped sections, inline editing, validation, and conditional visibility. Cadence works perfectly without Odin — the enhanced inspectors are a bonus.

---

## Custom Adjustment Rules

Implement `IAdjustmentRule` to add game-specific logic:

```csharp
using Cadence;

public class MyCustomRule : IAdjustmentRule
{
    public string RuleName => "MyCustomRule";

    public bool IsApplicable(AdjustmentContext ctx)
    {
        // Only activate when the player is bored
        return ctx.LastFlowReading.State == FlowState.Boredom;
    }

    public void Evaluate(AdjustmentContext ctx, AdjustmentProposal proposal)
    {
        proposal.Deltas.Add(new ParameterDelta
        {
            ParameterKey = "enemy_speed",
            CurrentValue = ctx.LevelParameters["enemy_speed"],
            ProposedValue = ctx.LevelParameters["enemy_speed"] * 1.1f,
            RuleName = RuleName
        });
    }
}
```

---

## Profile Persistence

### With CadenceManager (automatic)

CadenceManager saves and loads the player profile via PlayerPrefs automatically:
- **Load:** On `Awake()`, immediately after service creation
- **Save:** On `OnApplicationPause(true)`, `OnDestroy()`
- **Reset:** Call `CadenceManager.Instance.ResetProfile()` to wipe saved data and start fresh

The PlayerPrefs key defaults to `"Cadence_PlayerProfile"` and is configurable in the Inspector.

### Without CadenceManager (manual)

```csharp
// Save
string json = dda.SaveProfile();
PlayerPrefs.SetString("my_profile_key", json);

// Load
string json = PlayerPrefs.GetString("my_profile_key");
dda.LoadProfile(json);
```

You can also use `ProfilePersistence` for a cleaner API:

```csharp
ProfilePersistence.Save("my_key", dda.SaveProfile());
string json = ProfilePersistence.Load("my_key"); // returns null if not found
ProfilePersistence.Delete("my_key");
bool exists = ProfilePersistence.HasProfile("my_key");
```

---

## Samples

Install samples via the Unity Package Manager window (`Window > Package Manager > Cadence > Samples`).

| Sample | Description |
|--------|-------------|
| **Basic Integration** | Minimal session lifecycle, signal recording, and proposal reading |
| **Advanced Integration** | Sawtooth scheduling, player archetypes, level-type-aware proposals |
| **Manager Integration** | Simplified workflow using CadenceManager — no manual Tick or persistence code |

---

## Architecture

```
Assets/Cadence/
├── Runtime/
│   ├── Core/              IDDAService, DDAService, DDAConfig, CadenceManager, ProfilePersistence
│   ├── Data/              FlowState, SignalEntry, AdjustmentProposal, PlayerSkillProfile, etc.
│   ├── Signals/           SignalCollector, RingBuffer, FileStorage, Replay
│   ├── Analysis/          SessionAnalyzer, RunningAverage
│   ├── PlayerModel/       GlickoPlayerModel, PlayerModelConfig
│   ├── FlowDetection/     FlowDetector, FlowWindow, FlowDetectorConfig
│   ├── Adjustment/        AdjustmentEngine + Rules/ (4 built-in rules)
│   ├── Scheduling/        DifficultyScheduler, SawtoothCurveConfig
│   ├── Profiling/         PlayerProfiler, ArchetypeAdjustmentStrategy
│   └── Debug/             DDADebugData
├── Editor/                Debug windows, visualizers, setup wizard, manager tools
├── Tests/                 EditMode (7 suites) + PlayMode (1 integration test)
└── Samples~/              3 installable example integrations
```

**Two assemblies:**
- `Cadence` (Runtime) — zero dependencies, works in builds
- `Cadence.Editor` (Editor-only) — references Runtime, provides all editor tools

**Zero external dependencies.** Only requires Unity 2021.3+.

---

## Testing

Open the project in Unity and run tests via **Window > General > Test Runner**:

- **Edit Mode**: SignalCollector, SessionAnalyzer, FlowDetector, GlickoPlayerModel, AdjustmentEngine, DifficultyScheduler, PlayerProfiler, LevelTypes
- **Play Mode**: Full DDAService integration lifecycle

---

## FAQ

**Q: Does Cadence change my game's difficulty automatically?**
No. Cadence *proposes* changes via `GetProposal()`. Your game decides whether and how to apply them. You're always in control.

**Q: How many signals should I record?**
At minimum: `MoveExecuted` and `MoveOptimal` / `MoveWaste`. That's enough for the core pipeline. Add `HesitationTime`, `PauseTriggered`, and `PowerUpUsed` for significantly better accuracy.

**Q: What happens if I don't call Tick()?**
The flow detector won't update — `CurrentFlow` will remain at its initial state. Between-session proposals still work (they use session summary data, not real-time flow). CadenceManager calls `Tick()` automatically.

**Q: Can I use Cadence without CadenceManager?**
Yes. `new DDAService(config)` works exactly as before. CadenceManager is a convenience wrapper.

**Q: What's the performance cost?**
`Tick()` costs < 0.1ms per frame with an active session. Zero cost when no session is active. The ring buffer is fixed-size with no GC allocations.

**Q: Does the player profile persist across app restarts?**
With CadenceManager (auto-save enabled): yes, via PlayerPrefs. Without CadenceManager: you must call `SaveProfile()` / `LoadProfile()` yourself.

**Q: Can I use this for multiplayer?**
Cadence rates player-vs-level, not player-vs-player. It's designed for single-player adaptive difficulty. For competitive matchmaking, look at dedicated Glicko-2/Elo libraries.

---

## License

[MIT](LICENSE)

---

Built by [Ludaxis](https://github.com/Ludaxis)
