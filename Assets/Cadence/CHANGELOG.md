# Changelog

All notable changes to the Cadence DDA SDK are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/).

## [1.3.1] - 2026-05-15

### Added
- Low-confidence variant-step cap helper via `IDDAService.CapVariantStepForConfidence(...)`.

### Fixed
- Abandoned sessions with zero executed moves are ignored for DDA player history and next-level proposal output.

## [1.3.0] - 2026-05-08

### Added
- Proposal application API via `RecordProposalApplied`, so cooldowns are recorded only after the host actually applies a proposal.
- Stable `AdjustmentProposal.RuleFired` attribution values for analytics, including early-session `gate_blocked`.
- Signal confidence support on `RecordSignal` and persisted signal entries.

### Fixed
- Signal replay now round-trips level parameters and confidence.
- Profile persistence flushes PlayerPrefs on save and delete.
- File signal storage writes synchronously to avoid losing pause/quit session data.
- Editor simulation tools emit `move.optimal = 0` for non-optimal moves.
- Samples and editor simulation tools now call `RecordProposalApplied` after they actually apply proposal deltas.
- Scenario simulations record applied proposals against simulated time so cooldown behavior matches the simulated session clock.
- Runtime config validation clamps invalid ring buffer and flow window sizes.
- Flow detection no longer falls through to confident `Flow` when `move.optimal` data is missing.

## [1.2.1] - 2026-03-03

### Improved
- **XML documentation** on all public interfaces, classes, structs, and methods across 33 files
- **Named constants** replace magic numbers in SessionAnalyzer, FlowDetector, PlayerProfiler, AdjustmentEngine, and GlickoPlayerModel
- **OnValidate** editor-time validation added to SawtoothCurveConfig and PlayerModelConfig (all 5 configs now validated)
- **Switch expressions** modernize ArchetypeAdjustmentStrategy, LevelTypeDefaults, and DifficultyScheduler (C# 8+)
- **Null guards** in SessionAnalyzer.Analyze, GlickoPlayerModel constructor, and AdjustmentEngine rule loop
- **Sandbox Dashboard** completely rewritten — fixes 6 critical bugs (dead profile fields, broken compare mode, missing DDA feedback loop, wrong signal tiers, missing session summary, binary batch simulation)
- **Non-Odin inspector** attributes (rich tooltips, headers, range sliders) on all config ScriptableObjects

## [1.2.0] - 2026-03-03

### Added
- **CadenceManager** — drag-and-drop singleton MonoBehaviour wrapping DDAService with auto-tick, auto-persistence, and static `CadenceManager.Service` access
- **ProfilePersistence** — PlayerPrefs-based profile save/load helper
- **Create Manager in Scene** menu (`Cadence > Create Manager in Scene`) — one-click scene setup with auto-DDAConfig discovery
- **Install Odin Inspector** menu (`Cadence > Install Odin Inspector`) — guides users to install Odin for enhanced inspectors
- **CadenceEditorBridge** — auto-connects Debug Window and Flow Visualizer to CadenceManager on Play
- **CadenceManagerEditor** — custom inspector for CadenceManager when Odin is not installed
- **Manager Integration sample** — simplified workflow using CadenceManager (no manual Tick/persistence)
- `IDDAService.GetProposal(params, levelType, levelIndex)` — overload with level type and sawtooth index
- `IDDAService.SaveProfile()` and `IDDAService.LoadProfile(json)` — profile serialization on the interface

### Changed
- DDA Debug Window and Flow State Visualizer now auto-discover `CadenceManager.Service` in Play mode
- Update Checker uses `PackageManager.UI.Window.Open()` instead of `ExecuteMenuItem` for cross-version compatibility

## [1.1.0] - 2026-03-02

### Added
- **Level Types** — `Standard`, `Tutorial`, `Boss`, `Breather`, `MoveLimited`, `TimeLimited` with per-type DDA behavior
- **Sawtooth Difficulty Scheduler** — periodic difficulty wave curves across level sequences
- **Player Archetype Profiler** — classifies players as SpeedRunner, CarefulThinker, StrugglingLearner, BoosterDependent, ChurnRisk
- **Odin Inspector attributes** — conditional `#if ODIN_INSPECTOR` attributes on all config ScriptableObjects
- **Update Checker** — editor window for checking new SDK versions on GitHub
- **Sandbox Dashboard** — test DDA behavior with simulated sessions
- **Difficulty Curve Preview** — visualize sawtooth scheduling curve in the editor

## [1.0.0] - 2026-03-02

### Added
- Initial release as standalone UPM package
- Signal collection with fixed-size ring buffer (zero GC allocations)
- Session analysis with exponential running averages
- Glicko-2 player skill modeling with confidence intervals and time-decay
- Real-time flow state detection (Flow, Boredom, Anxiety, Frustration)
- Adjustment engine with 4 pluggable rules: FlowChannel, FrustrationRelief, StreakDamper, Cooldown
- File-based signal persistence (`Application.persistentDataPath/Cadence/Signals/`)
- Signal replay system for debugging
- Editor windows: DDA Debug, Signal Replay, Flow State Visualizer
- Setup Wizard for guided configuration
- Edit mode and play mode test suites
- 2 installable samples: Basic Integration, Advanced Integration
