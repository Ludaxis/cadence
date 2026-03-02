# Cadence

**Game-agnostic Dynamic Difficulty Adjustment SDK for Unity.**

Signal-based player modeling, real-time flow detection, Glicko-2 skill tracking, and adaptive difficulty proposals. Drop it into any game — puzzle, action, RPG — and let Cadence keep every player in the flow channel.

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

Pin a specific version with a tag:

```json
"com.ludaxis.cadence": "https://github.com/Ludaxis/cadence.git?path=Assets/Cadence#1.0.0"
```

### Option 2: Drag and drop

Copy the `Assets/Cadence/` folder into your project's `Assets/Plugins/Cadence/`.

### Option 3: Local package reference

Clone this repo and reference it locally:

```json
"com.ludaxis.cadence": "file:../../cadence/Assets/Cadence"
```

### Option 4: Open as Unity project

Clone and open the repo root directly in Unity Hub (2022.3+). The project is ready for development and testing.

---

## How It Works

```
Signals  ->  Analysis  ->  Player Model  ->  Flow Detection  ->  Adjustment  ->  Proposal
(input)     (aggregate)    (Glicko-2)        (real-time)         (rules)        (output)
```

| Module | What it does |
|--------|-------------|
| **Signals** | Collect, buffer, persist, and replay gameplay signals via a ring buffer |
| **Analysis** | Compute session-level statistics with exponential running averages |
| **Player Model** | Glicko-2 skill rating with confidence intervals and time-decay |
| **Flow Detection** | Classify player state in real-time: Flow, Boredom, Anxiety, Frustration |
| **Adjustment** | Propose difficulty changes via pluggable rules with cooldown and damping |

---

## Quick Start

```csharp
using Cadence;
using System.Collections.Generic;

// 1. Create config assets via Create menu: Cadence/DDA Config
//    (also create and assign sub-configs: Player Model, Flow Detector, Adjustment Engine)

// 2. Initialize the service
var config = Resources.Load<DDAConfig>("DDAConfig");
IDDAService dda = new DDAService(config);

// 3. Start a session when a level begins
dda.BeginSession("level_42", new Dictionary<string, float>
{
    { "enemy_count", 12f },
    { "time_limit", 90f }
});

// 4. Record signals as the player acts
dda.RecordSignal(SignalKeys.MoveExecuted);
dda.RecordSignal(SignalKeys.MoveOptimal);
dda.RecordSignal(SignalKeys.HesitationTime, 1.8f, SignalTier.BehavioralTempo);

// 5. Tick every frame for real-time flow detection
dda.Tick(Time.deltaTime);
FlowReading flow = dda.CurrentFlow;
// flow.State => Flow | Boredom | Anxiety | Frustration

// 6. End session and get adjustment proposal
dda.EndSession(SessionOutcome.Win);
AdjustmentProposal proposal = dda.GetProposal(nextLevelParams);
// proposal.Deltas => list of parameter changes to apply
// proposal.Confidence => how certain the system is (0..1)
```

---

## Signal Tiers

Signals are categorized by type so the system can weight them appropriately:

| Tier | Purpose | Example Keys |
|------|---------|-------------|
| **DecisionQuality** | Was the move good or bad? | `move.optimal`, `move.waste`, `resource.efficiency` |
| **BehavioralTempo** | How fast is the player acting? | `tempo.interval`, `tempo.hesitation`, `tempo.pause` |
| **StrategicPattern** | What strategy is the player using? | `strategy.powerup`, `strategy.sequence_match` |
| **RetryMeta** | Session-level retry behavior | `meta.attempt`, `meta.session_gap`, `meta.abandoned` |
| **RawInput** | Low-level input accuracy | `input.accuracy`, `input.rejected` |

Use `SignalKeys.*` constants or define your own string keys.

---

## Flow Detection

The flow detector runs a sliding window over recent signals and classifies the player's state:

| State | Meaning | What to do |
|-------|---------|-----------|
| **Flow** | Challenge matches skill | Keep current difficulty |
| **Boredom** | Too easy — high efficiency, fast tempo | Increase difficulty |
| **Anxiety** | Too hard — low efficiency, slow tempo | Decrease difficulty slightly |
| **Frustration** | Repeated failure, giving up signals | Decrease difficulty significantly |

Configure thresholds in `FlowDetectorConfig`:
- Window sizes for tempo, efficiency, and engagement
- Boredom/anxiety/frustration thresholds
- Hysteresis count to prevent state flickering
- Warmup moves before detection activates

---

## Player Model (Glicko-2)

Each player gets a skill profile with three components:

| Component | Meaning | Default |
|-----------|---------|---------|
| **Rating** | Estimated skill level | 1500 |
| **Deviation** | Uncertainty in the rating | 350 |
| **Volatility** | How erratic the player's performance is | 0.06 |

The model updates after each session based on outcome vs. expected performance. Deviation increases over time when the player is inactive (configurable decay rate).

`PlayerSkillProfile.Confidence01` = `1 - (Deviation / 350)` — ranges from 0 (no data) to ~1 (stable estimate).

---

## Adjustment Rules

The adjustment engine evaluates pluggable rules to generate proposals:

| Rule | What it does |
|------|-------------|
| **FlowChannelRule** | Nudges parameters toward the flow channel based on detected state |
| **FrustrationReliefRule** | Larger relief when frustration is detected |
| **StreakDamperRule** | Dampens adjustments during win/loss streaks to avoid overcorrection |
| **CooldownRule** | Prevents adjustments from firing too frequently |

Implement `IAdjustmentRule` to add custom rules:

```csharp
public class MyCustomRule : IAdjustmentRule
{
    public string RuleName => "MyRule";
    public bool IsApplicable(AdjustmentContext ctx) => ctx.Flow.State == FlowState.Boredom;
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

## Configuration

Create ScriptableObject assets via the Unity **Create** menu:

| Asset | Menu Path | Key Settings |
|-------|-----------|-------------|
| **DDA Config** | `Cadence/DDA Config` | Ring buffer size, storage toggle, mid-session detection |
| **Player Model Config** | `Cadence/Player Model Config` | Initial rating/deviation/volatility, tau, time decay |
| **Flow Detector Config** | `Cadence/Flow Detector Config` | Window sizes, thresholds, hysteresis, warmup |
| **Adjustment Engine Config** | `Cadence/Adjustment Engine Config` | Rule weights, cooldown, damping |

---

## Editor Tools

| Tool | Menu Path | Purpose |
|------|-----------|---------|
| **Debug Window** | `Cadence/Debug Window` | Live view of flow state, signals, and player profile during Play mode |
| **Signal Replay** | `Cadence/Signal Replay` | Replay recorded signal logs to reproduce and debug DDA behavior |
| **Flow State Visualizer** | *(auto-enabled)* | Scene view overlay showing current flow state as a colored badge |

---

## Architecture

```
Assets/Cadence/
├── Runtime/
│   ├── Core/              DDAService, DDAConfig, IDDAService
│   ├── Data/              Value types: FlowState, SignalEntry, AdjustmentProposal, etc.
│   ├── Signals/           SignalCollector, RingBuffer, FileStorage, Replay
│   ├── Analysis/          SessionAnalyzer, RunningAverage
│   ├── PlayerModel/       GlickoPlayerModel, PlayerSkillProfile
│   ├── FlowDetection/     FlowDetector, FlowWindow
│   ├── Adjustment/        AdjustmentEngine + Rules/
│   └── Debug/             DDADebugData
├── Editor/                Debug windows and visualizers
├── Tests/                 EditMode + PlayMode test suites
├── Samples~/              Example integrations (installable via Package Manager)
└── Documentation~/        Reference docs
```

**Zero external dependencies.** Only requires Unity 2021.3+.

---

## Testing

Open the project in Unity and run tests via **Window > General > Test Runner**:

- **Edit Mode** (5 tests): SignalCollector, SessionAnalyzer, FlowDetector, GlickoPlayerModel, AdjustmentEngine
- **Play Mode** (1 test): Full DDAService integration lifecycle

---

## License

[MIT](LICENSE)

---

Built by [Ludaxis](https://github.com/Ludaxis)
