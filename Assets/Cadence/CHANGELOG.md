# Changelog

## [1.2.0] - 2026-03-03

### Added
- **CadenceManager** — drag-and-drop singleton MonoBehaviour wrapping DDAService with auto-tick, auto-persistence, and static `CadenceManager.Service` access
- **ProfilePersistence** — PlayerPrefs-based profile save/load helper
- **Create Manager in Scene** menu (`Cadence/Create Manager in Scene`) — one-click scene setup with auto-DDAConfig discovery
- **Install Odin Inspector** menu (`Cadence/Install Odin Inspector`) — guides users to install Odin for enhanced inspectors
- **CadenceEditorBridge** — auto-connects Debug Window and Flow Visualizer to CadenceManager on Play
- **CadenceManagerEditor** — custom inspector for CadenceManager when Odin is not installed
- **ManagerIntegration sample** — simplified workflow using CadenceManager (no manual Tick/persistence)
- `IDDAService.GetProposal(params, levelType, levelIndex)` overload
- `IDDAService.SaveProfile()` and `IDDAService.LoadProfile(json)` for profile serialization

### Changed
- DDA Debug Window and Flow State Visualizer now auto-discover CadenceManager.Service in Play mode

## [1.0.0] - 2026-03-02

### Added
- Initial release as standalone UPM package
- Signal collection and ring buffer storage
- Session analysis with running averages
- Glicko-2 player skill modeling
- Real-time flow state detection (Flow, Boredom, Frustration, Anxiety)
- Adjustment engine with pluggable rules (Cooldown, FlowChannel, FrustrationRelief, StreakDamper)
- File-based signal persistence
- Signal replay system for debugging
- Editor windows: DDA Debug, Signal Replay, Flow State Visualizer
- Edit mode and play mode test suites
