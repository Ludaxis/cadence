# Cadence Casual-Tile DDA Audit

**Updated**: 2026-03-13
**Benchmark**: Internal DDA system for casual tile / mobile puzzle games
**Method**: 8 structured workstreams plus coordinator synthesis
**Repo reviewed**: runtime, editor, tests, samples, docs, package metadata, and current audit artifacts
**Code footprint reviewed**: 13,819 lines across runtime, editor, tests, and samples

## Coordinator Verdict

Cadence is **not** a world-class casual-tile DDA system today.

It is now substantially closer to first-project readiness than it was at the start of this audit cycle. The runtime now has:
- scalar parameter polarity for built-in puzzle defaults
- public custom-rule registration
- real support for `tempo.interval`, `input.accuracy`, `resource.efficiency`, and explicit abandonment
- default-on session fatigue for long contiguous play sessions
- executable EditMode and PlayMode coverage for the new contract

The remaining gaps are now narrower and more specific:
- the public integration surface is improved, but still not SDK-grade
- the puzzle fit is still scalar-parameter-first, not board- or blocker-aware
- proposal generation is now explicit-only, and provider hooks now exist, but external packaging is still not mature
- editor monoliths still need refactoring before broad publication

### Direct Answers

| Question | Answer |
|---|---|
| Is this world class for puzzle games? | No. |
| Can I publish it today as broadly claimed? | No. Not as a broad "any mobile puzzle game" SDK. |
| Can I use it internally? | Yes. It is now viable for a first internal scalar-puzzle project with clear scope discipline. |
| Is it modular and easy to integrate for developers? | Moderately. Good for in-house engineers, better than before, still not strong enough for external SDK consumers. |
| Can it serve every mobile puzzle subtype? | No. It is much stronger for scalar-difficulty puzzle loops than for blocker-heavy, board-authored, or topology-driven puzzle games. |

## Final Score

This score reflects the system itself against the agreed benchmark. Documentation was reconciled in this pass, but the runtime verdict does not change.

| Dimension | Weight | Score | Notes |
|---|---:|---:|---|
| Runtime correctness and safety | 35% | 8.1 / 10 | Efficient core, with scalar semantics, explicit signal support, abandonment coercion, and default fatigue now in place. |
| Casual tile / pop puzzle fit | 25% | 6.9 / 10 | Stronger for scalar puzzle production, still weak on board/blocker semantics and authored-content understanding. |
| Integration and developer ergonomics | 20% | 7.9 / 10 | Good manager/tooling path, explicit-only proposal contract, and public provider hooks for rules and level semantics; still not plugin-grade. |
| Documentation truthfulness | 10% | 8.6 / 10 | Runtime, docs, samples, and audit are now materially closer to one truthful contract. |
| Tests and maintainability | 10% | 8.4 / 10 | EditMode and PlayMode suites now cover the new signal contract and fatigue behavior, though maintainability hotspots remain. |
| **Weighted total** | 100% | **7.9 / 10** | **Strong internal first-project foundation, still not world-class and not ready to market broadly.** |

Verdict bands:
- `9.0+`: world-class internal casual-tile DDA
- `8.0-8.9`: strong and publishable with minor caveats
- `6.0-7.9`: promising but not world-class / not ready to market broadly
- `<6.0`: not ready to publish as claimed

Cadence now lands in the `6.0-7.9` band, near the top of it.

## Research Baseline

The evaluation criteria were anchored to a small, high-signal reference set:

- [Robin Hunicke, "The Case for Dynamic Difficulty Adjustment in Games"](https://www.researchgate.net/profile/Robin_Hunicke/publication/220982524_The_case_for_dynamic_difficulty_adjustment_in_games/links/53fb98490cf2dca8fffe800a.pdf)
  Baseline principle: DDA should keep the player inside a target challenge band without feeling arbitrary or obviously fake.

- [Mark Glickman, "Example of the Glicko-2 system"](https://glicko.net/glicko/glicko2.pdf)
  Baseline principle: rating + deviation + volatility is a valid confidence-aware player model, but only when the modeled match and difficulty semantics are coherent.

- [King / GDC Vault, "Blockers: Analyzing Difficulty Drivers in Candy Crush Games"](https://www.gdcvault.com/play/1026879/Blockers-Analyzing-Difficulty-Drivers-in)
  Puzzle-specific baseline: casual tile difficulty is not just a scalar number. Moves, blockers, board complexity, and authored level content all matter.

- [PMC, "Inferring and Comparing Game Difficulty Curves using Player-vs-Level Match Data"](https://pmc.ncbi.nlm.nih.gov/articles/PMC8336693/)
  Baseline principle: player-vs-level rating ideas are useful, but they still need level-side semantics and difficulty-curve awareness.

- [ScienceDirect, "Personalized game design for improved user retention and monetization in freemium games"](https://www.sciencedirect.com/science/article/pii/S0167811625000060)
  Product baseline: in freemium games, difficulty tuning interacts with retention and monetization. A production DDA stack needs clear guardrails around assists, boosts, and live-ops goals.

### What Puzzle DDA Leaders Actually Do

From those references, a puzzle-appropriate DDA system typically needs:
- a target challenge band, not just raw win/loss reaction
- parameter semantics that understand whether a larger value makes the level harder or easier
- content-aware levers such as blockers, board state, goal density, or move budget
- onboarding protection for the first sessions
- clean separation between player model, current frustration state, and monetization/live-ops constraints

Cadence covers only part of that list today.

## How Cadence Works Today

### Runtime Pipeline

1. `DDAService.BeginSession(...)` starts an attempt, resets the collector and flow detector, applies session-gap decay, and records `session.started`.
2. `RecordSignal(...)` appends raw signals to the session batch and recent ring buffer.
3. `Tick(deltaTime)` feeds the flow detector from new ring-buffer entries only.
4. `EndSession(outcome)` records outcome signals, builds a `SessionSummary`, and updates the Glicko-2 profile.
5. `GetProposal(...)` classifies the player's archetype, builds `AdjustmentContext`, runs rules, merges duplicate deltas by largest absolute change, applies sawtooth scaling, clamps results, and returns `AdjustmentProposal`.
6. The host game decides whether and how to apply each proposed value.

### Actual Inputs

| Category | Inputs actually required or consumed |
|---|---|
| Session boundaries | `BeginSession(...)`, `EndSession(...)` |
| Required gameplay contract | `move.executed`, `move.optimal` with `1/0` every move |
| Optional but currently consumed | `move.waste`, `progress.delta`, `tempo.hesitation`, `tempo.pause`, `strategy.powerup`, `strategy.sequence_match`, `meta.attempt`, `meta.session_gap`, `input.rejected` |
| Runtime clock | `Tick(deltaTime)` each frame unless `CadenceManager` is used |
| Proposal context | `nextLevelParameters`, plus preferably explicit `nextLevelType` and `nextLevelIndex` |

### Actual Outputs

| Output | Produced by | Notes |
|---|---|---|
| `FlowReading` | `CurrentFlow` | Real-time state, confidence, tempo, efficiency, engagement |
| `SessionSummary` | `SessionAnalyzer` | Not public directly, but stored in debug data and fed into rules / player model |
| `PlayerSkillProfile` | `PlayerProfile` | Rating, deviation, volatility, averages, recent history |
| `PlayerArchetypeReading` | `CurrentArchetype` | Primary and secondary archetype labels |
| `AdjustmentProposal` | `GetProposal(...)` | Deltas, reason, confidence, detected state, timing |
| `DDADebugData` | `GetDebugSnapshot()` | Full tool-facing snapshot |

## Acceptance Checks

| Check | Result | Notes |
|---|---|---|
| Move-limited and time-limited parameter directionality | **Pass** | Built-in defaults now declare polarity and rules translate semantic harder/easier into the correct numeric direction. |
| Documented sample path vs real-time flow correctness | **Pass after this reconciliation** | README and sample integrations were updated to emit `MoveOptimal = 1/0` every move. The runtime contract is still brittle, but the sample path now matches it. |
| Public custom-rule extensibility from the actual API surface | **Pass** | `IDDAService` now exposes single-rule registration plus provider hooks for rule packs and level-type config overrides. |
| Next-level type handling with explicit proposal API | **Pass** | Proposal generation now always requires explicit next-level type context. |
| Signal/docs parity for `tempo.interval`, `input.accuracy`, `resource.efficiency`, and abandonment signals | **Pass** | Those keys are now consumed by the runtime and documented accordingly. |
| Redundancy and monolith hotspots in editor tools, rules, and samples | **Fail** | Oversized editor windows and duplicated integration snippets remain. |

## Workstream Reports

### 1. Research Track

**Mission**: Build the benchmark for a casual tile / pop puzzle DDA system.

**Findings**
- Cadence aligns with classic DDA ideas around challenge bands and confidence-aware player modeling.
- Cadence does not yet align with King-style puzzle difficulty practice, where authored blockers and content semantics matter as much as win rate.
- Cadence also lacks a clear monetization / assist-policy layer, which matters for freemium puzzle production even when the DDA is internal.

**Impact**
- The project can be judged fairly as a strong generic adaptation engine.
- It cannot currently be judged as world-class for casual tile content specifically.

### 2. Core / API Track

**Mission**: Inspect session lifecycle, public API, persistence, and developer integration.

**Strengths**
- `DDAService` is clean, readable, and low ceremony.
- `CadenceManager` gives a workable drag-and-drop path for internal teams.
- `GetDebugSnapshot()` and profile save/load make the system inspectable.
- `RegisterRuleProvider(...)` and `RegisterLevelTypeConfigProvider(...)` now let projects extend rules and scalar semantics without forking runtime defaults.

**Weaknesses**
- External packaging is still not polished enough for third-party plugin expectations.
- Real-time flow is public, but real-time intervention is not a real public workflow.

**Verdict**
- Good internal API.
- Not yet a world-class SDK surface.

### 3. Signals / Flow Track

**Mission**: Verify what the flow detector and analyzer really consume.

**Strengths**
- The recent-signal processing path is now technically sound.
- Live flow classification is cheap and well structured.

**Weaknesses**
- `MoveOptimal` is still the only live efficiency input.
- `MoveWaste` still matters only after session analysis.
- `TempoScore` is still based on interval consistency, not pure speed.
- `HesitationTime` still affects session analysis, not live flow.

**Impact**
- The live flow contract is narrower than the product language suggests.
- Integrations must be precise or they get misleading flow signals.

### 4. Player Model / Profiling Track

**Mission**: Evaluate Glicko-2 usage, cold start, and archetype logic.

**Strengths**
- The Glicko-2 implementation is competent and now protected against malformed profile data.
- Cold start is materially better than the earlier dormant-first-five-sessions state because `NewPlayerRule` exists by default.
- Archetype classification adds useful modulation.

**Weaknesses**
- The flow-channel rule is driven by `AverageOutcome`, not by runtime use of `PredictWinRate(...)`.
- Effective opponent difficulty is still a coarse move-efficiency proxy.
- Booster dependence and churn risk are profile heuristics, not puzzle-mechanic understanding.

**Impact**
- Strong player-modeling foundation.
- Not yet as semantically grounded as the surrounding docs used to claim.

### 5. Adjustment / Scheduling Track

**Mission**: Evaluate the proposal engine, rules, level types, and sawtooth behavior.

**Strengths**
- Rules are understandable and deterministic.
- Cooldown, new-player easing, and sawtooth scaling are now correctly wired.
- Duplicate delta merge and relative clamping are defensible.

**Weaknesses**
- Parameter polarity and bounds exist for built-in scalar puzzle keys, but not for authored content semantics.
- `SecondaryParameterKeys` are populated but not actually used by rule weighting.
- Provider hooks now exist, but they still stop short of live-config injection or polished external packaging.

**Impact**
- This track contains the main publishability blocker for casual puzzle use.

### 6. Editor / DX Track

**Mission**: Assess practical developer usability.

**Strengths**
- Setup Wizard, Sandbox Dashboard, Scenario Simulator, Debug Window, and visual tools are a major asset.
- Debugging the system is much easier than with most internal DDA prototypes.

**Weaknesses**
- The editor is concentrated in a few very large files:
  - `ScenarioSimulator.cs` (~1112 lines)
  - `SandboxDashboard.cs` (~1070 lines)
  - `CadenceUpdateChecker.cs` (~661 lines)
- Those monoliths are maintainability costs and make smaller improvements harder.

**Verdict**
- Strong internal tooling.
- Not yet refined into clean, small, extensible editor modules.

### 7. Tests / Quality Track

**Mission**: Review tests and verify executable coverage where possible.

**Strengths**
- Core algorithm coverage is better than average for an internal Unity package.
- Recent fixes are backed by targeted tests.

**Weaknesses**
- Important behavior is still not fully covered:
  - provider precedence and failure-fallback paths
  - richer board/content semantics that do not yet exist
  - editor/tool refactors

**Verification status**
- Static test review: completed.
- Unity CLI EditMode run: passed with machine-readable XML.
- Unity CLI PlayMode run: passed with machine-readable XML.

### 8. Docs Consistency Track

**Mission**: Compare code, samples, and docs claim by claim.

**Findings before reconciliation**
- README and package README overclaimed game-agnostic scope.
- Event-mapping docs previously overstated or misstated support for `tempo.interval`, `input.accuracy`, `resource.efficiency`, and `meta.abandoned`.
- PRD described extension and signal behavior that had drifted from reality.
- Inline comments still contained stale semantics such as "negative delta = easier" as if that were universally true.

**Changes made in this pass**
- READMEs narrowed to the actual supported scope and current contract.
- Event-mapping docs were rewritten around supported-now vs declared-only signals.
- PRD was rewritten as a current-state document, not an aspirational feature deck.
- Samples and inline docs were corrected where they directly contradicted runtime behavior.
- Audit conclusions were updated to reflect the runtime now shipping the formerly dormant signal path and default fatigue behavior.

## Key Findings by Severity

### Critical

1. **Cadence is scalar-parameter-aware, not board-aware**
   The system has no understanding of blockers, board topology, scripted hazards, or authored puzzle content. For casual tile games, that matters.

### High

2. **The public extension surface is improved, but still not SDK-ready**
   Custom rules, rule packs, and level-type config providers are now registrable through `IDDAService`, but packaging is still internal-first.

3. **The public extension surface is still narrower than the internal architecture**
   Proposal safety is better now that next-level type is mandatory, and provider hooks now cover rule packs and scalar level semantics, but there is still no live-config or packaged plugin story.

### Medium

6. **Real-time flow is stronger than real-time action**
   Cadence exposes live state, but not a clean live intervention pipeline.

7. **Oversized editor classes are refactor pressure**
   Tooling is valuable, but the large windows are becoming maintenance sinks.

8. **Some inline comments still overgeneralize semantics**
   The code comments needed cleanup to stop implying that numeric sign alone maps to difficulty direction.

## Publishability Answer

### Can this be published today?

**As a broad external SDK: still no.**

Reasons:
- scalar puzzle safety is improved, but board/content semantics are still missing
- the public extension surface is still internal-first rather than polished for external SDK consumers
- puzzle fit is still not content-aware enough for an "any mobile puzzle game" claim
- editor and extension refactors are still needed before external publication

### Can this be used internally?

**Yes, with scope discipline.**

Reasonable internal use today:
- a studio-owned casual puzzle project
- engineers willing to explicitly manage parameter meaning
- integrations that can obey the strict signal contract
- teams treating Cadence as a proposal engine, not as a fully autonomous DDA stack

## Modularity and Integration Answer

### Is it modular?

**Moderately.**

Why:
- runtime responsibilities are separated well
- interfaces are generally clean
- manager and service split is sensible
- scalar parameter semantics and custom rule registration are now exposed enough for better internal extensibility
- but provider hooks still need stronger packaging and live-config support

### Is it easy to integrate?

**Moderately easy for internal engineers, not truly easy for external consumers.**

Easy parts:
- `CadenceManager`
- config assets
- readable core service lifecycle
- strong editor tools

Hard parts:
- strict `MoveOptimal = 1/0` per-move contract
- next-level-type overload gotcha
- unclear semantics for keys like `move_limit` and `time_limit`
- no public hook for custom rules

## Redundancy Inventory

Static duplicate-code scan (`jscpd`) found only small clone groups, but structural redundancy still matters:

1. Sample integration logic is repeated across all three sample scripts.
2. Rule classes repeat similar "iterate level parameters and emit scaled delta" logic.
3. `ScenarioSimulator`, `SandboxDashboard`, and `CadenceUpdateChecker` are monolithic and mix rendering, state management, and domain logic.
4. `SignalBatch.Entries` is publicly mutable while `GetEntries()` exists but is largely unused, which is both redundant and leaky.
5. `SecondaryParameterKeys` are configured but unused by the rules.

## Refactor Roadmap

### 1. Flow-detector correctness cleanup

Do three things:
- keep the strict `MoveOptimal = 1/0` contract explicit
- decide whether `MoveWaste` should affect live efficiency too
- separate "speed" from "consistency" if product language keeps using "tempo"

Outcome:
- live flow states become easier to interpret and integrate correctly

### 2. Extension surface

Expand beyond basic public rule registration:
- alternate rule sets
- packaged parameter semantics providers
- safer lifecycle/config hooks

Outcome:
- Cadence becomes much more publishable as a toolkit

### 3. Remove remaining public-surface ambiguity

Do one thing:
- decide whether `strategy.stored` should be wired or demoted from the public signal contract

Outcome:
- a smaller and more truthful public surface

### 4. Editor decomposition

Split large windows into:
- shared graph helpers
- simulation services
- serialized editor state
- small panel renderers

Outcome:
- tooling stays a strength without becoming a maintenance anchor

### 5. One source of truth cleanup

Keep these aligned:
- root README
- package README
- PRD
- event-mapping docs
- inline XML docs

Outcome:
- much lower integration risk for the next developer

## Verification Notes

What was verified directly:
- runtime, editor, tests, samples, and docs were reviewed line by line
- static search was used to confirm call paths, signal consumers, and API exposure
- duplicate-code scan was run across runtime, editor, and samples

What was verified executable:
- Unity batch compile/open pass succeeded
- Unity EditMode tests passed and emitted `/tmp/cadence-editmode-results.xml`
- Unity PlayMode tests passed and emitted `/tmp/cadence-playmode-results.xml`

## Bottom Line

Cadence is now a good internal DDA base for a first scalar-puzzle project if the studio understands its limits. It is **not** a world-class casual-tile DDA system yet, and it should **not** be published as a broad "works for any mobile puzzle game" solution until public extensibility and broader puzzle-content semantics are addressed.
