# Changelog

All notable changes to the Cadence DDA SDK are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/).

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
