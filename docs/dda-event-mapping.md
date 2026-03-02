# Cadence DDA — Player Performance Event Tracking

> Source format reference: `NCT_Event Mastersheet.xlsx`
> Scope: Events needed for player performance tracking in a mobile casual puzzle game.
> Only **Must Have** and **Should Have** events are listed — analytics-only events (ads, IAP, navigation, FTUE, cosmetics, account) are excluded.
>
> Tags: `[NCT]` = already exists in the mastersheet. `[NEW]` = must be added for DDA.

---

## The Problem With Current Event Coverage

The existing event mastersheet captures **session boundaries** (`song_start`, `song_result`) and **aggregate outcomes** (total boosters, final progress, playtime). But Cadence's FlowDetector needs **moment-to-moment in-session signals** — individual tile taps, timing between moves, heart losses, and hesitation patterns that happen *during* gameplay. Without these, the DDA system is blind between session open and session close.

---

## Signal Tier Reference

| Tier | Name | Purpose | Example |
|------|------|---------|---------|
| **Tier 0** | Decision Quality | Was the player's action good or bad? | `move.optimal`, `move.waste`, `progress.delta` |
| **Tier 1** | Behavioral Tempo | How fast is the player acting? | `tempo.interval`, `tempo.hesitation` |
| **Tier 2** | Strategic Pattern | What strategy is the player using? | `strategy.powerup`, `resource.efficiency` |
| **Tier 3** | Retry & Meta | Session-level retry and abandonment behavior | `meta.attempt`, `meta.session_gap`, `meta.abandoned` |
| **Tier 4** | Raw Input | Low-level input accuracy | `input.accuracy`, `input.rejected` |

---

## Must Have Events

### M1 — `[NCT]` song_start

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 1 | song_start | | | | Trigger whenever user starts a level (when the level starts and the puzzle is shown). Including: restart, replay. Not including: revive or resume. | Session boundary — `BeginSession()` |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | attempt | Integer | [cumulative count] | | Total number of attempts for the song. High count = player is stuck. |
| | | level_type | String | common \| daily \| secret | | Level category |
| | | play_type | String | start \| restart \| replay | | How the user entered. `restart` = failed and retrying. `replay` = mastered. |
| | | live_status | String | default \| streak_buff_1 \| streak_buff_2 \| streak_buff_3 \| infinite | | Hearts/shields at level start |
| | | total_level_played | Integer | [number] | | Total unique levels played. Player maturity indicator. |
| | | `[NEW]` par_moves | Integer | [number] | | Level designer's minimum moves to complete the level perfectly. Needed for skill efficiency ratio. |

**Cadence Mapping:** `session.started` (—), `meta.attempt` (Tier 3), `meta.play_type` (Tier 3)

---

### M2 — `[NCT]` song_result

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 2 | song_result | | | | Trigger when user reaches the result screen (Congratulations or Out-of-Lives refuse). | Session boundary — `EndSession()` |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | level_type | String | common \| daily \| secret | | Level category |
| | | result | String | win \| lose | | Core outcome for Glicko-2 skill model update |
| | | playtime | Integer | [seconds] | | Total time playing. Abnormally long = struggling. Abnormally short = too easy or gave up. |
| | | progress | Integer | 0–100 | | Completion rate (removed tiles / all tiles) |
| | | perfect_percentage | Integer | 0–100 | | Accuracy rate (perfect tiles / all tiles) |
| | | hint_used | Integer | [count] | | Total hints used. High = needed assistance. |
| | | bomb_used | Integer | [count] | | Total bombs used. |
| | | magnet_used | Integer | [count] | | Total magnets used. |
| | | continue | Integer | [count] | | Total revives used. |
| | | `[NEW]` actual_moves | Integer | [number] | | Player's total moves this session. |
| | | `[NEW]` par_moves | Integer | [number] | | Echo of level minimum moves. Enables `actual_moves / par_moves` skill ratio. |

**Cadence Mapping:**

| Parameter | Cadence Signal | Tier |
|-----------|---------------|------|
| *(event itself)* | `session.ended` + `session.outcome` | — |
| result | SessionOutcome.Win / Lose | — |
| progress | `progress.delta` | Tier 0 |
| perfect_percentage | `input.accuracy` | Tier 4 |
| playtime | `tempo.session_duration` | Tier 1 |
| hint_used + bomb_used + magnet_used | `strategy.booster_total` | Tier 2 |
| par_moves / actual_moves | `resource.efficiency` | Tier 0 |

**Integration Example:**

```csharp
// On song_start
_dda.BeginSession(levelId, new Dictionary<string, float>
{
    { "attempt", attemptCount },
    { "par_moves", parMoves },
    { "level_type_code", levelTypeCode },
    { "live_status_code", liveStatusCode }
});
_dda.RecordSignal(SignalKeys.AttemptNumber, attemptCount, SignalTier.RetryMeta);

// On song_result
_dda.RecordSignal(SignalKeys.ProgressDelta, progress / 100f, SignalTier.DecisionQuality);
_dda.RecordSignal(SignalKeys.InputAccuracy, perfectPercentage / 100f, SignalTier.RawInput);

float moveEfficiency = parMoves > 0
    ? Mathf.Clamp01((float)parMoves / actualMoves)
    : 0f;
_dda.RecordSignal(SignalKeys.ResourceEfficiency, moveEfficiency, SignalTier.DecisionQuality);

float totalBoosters = hintUsed + bombUsed + magnetUsed;
_dda.RecordSignal(SignalKeys.PowerUpUsed, totalBoosters, SignalTier.StrategicPattern);

_dda.EndSession(result == "win" ? SessionOutcome.Win : SessionOutcome.Lose);
```

---

### M3 — `[NEW]` tile_tap

> **The most important new event.** Every single tile interaction. This is the atomic unit of gameplay that feeds the FlowDetector's real-time classification. Without this, Cadence cannot detect Flow, Boredom, Anxiety, or Frustration during gameplay.

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 3 | tile_tap | | | | Trigger on every tile interaction by the player during action phase. Fires regardless of whether the tap results in a perfect, good, or miss. | The fundamental gameplay unit for DDA. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | move_index | Integer | [sequential, 1-based] | | Sequential move number within this session. Increments per tap. |
| | | result | String | perfect | | Tile tapped with perfect timing/accuracy |
| | | | | good | | Tile tapped correctly but not perfect |
| | | | | miss | | Tile tapped incorrectly or at wrong time |
| | | tap_time | Float | [seconds] | | Time elapsed since level start when the tap occurred |
| | | inter_move_time | Float | [seconds] | | Time since the previous tile_tap (0 for first tap). Key tempo signal. |
| | | tiles_remaining | Integer | [count] | | Tiles remaining on the board after this tap |
| | | tiles_total | Integer | [count] | | Total tiles in the level (constant per level) |
| | | hearts_remaining | Integer | [count] | | Current hearts/lives remaining after this tap |
| | | combo_count | Integer | [count] | | Current consecutive combo count (0 if combo just broke) |
| | | progress | Integer | 0–100 | | Completion rate at this moment |

**What this single event feeds in Cadence (all 5 tiers):**

| Derived Cadence Signal | Tier | How It's Derived |
|----------------------|------|-----------------|
| `move.executed` | Tier 0 | Every tile_tap fires MoveExecuted |
| `move.optimal` | Tier 0 | result = perfect or good → 1.0; result = miss → 0.0 |
| `move.waste` | Tier 0 | result = miss → waste value of 1.0 |
| `progress.delta` | Tier 0 | Change in progress between consecutive taps |
| `tempo.interval` | Tier 1 | inter_move_time directly → InterMoveInterval |
| `strategy.sequence_match` | Tier 2 | combo_count > 0 → combo sustained (skill signal) |
| `input.accuracy` | Tier 4 | Running ratio of (perfect + good) / total taps |

**Integration Example:**

```csharp
// On tile_tap
_dda.RecordSignal(SignalKeys.MoveExecuted, 1f, SignalTier.DecisionQuality, moveIndex);

bool isOptimal = (result == "perfect" || result == "good");
_dda.RecordSignal(SignalKeys.MoveOptimal, isOptimal ? 1f : 0f,
    SignalTier.DecisionQuality, moveIndex);

if (result == "miss")
    _dda.RecordSignal(SignalKeys.MoveWaste, 1f, SignalTier.DecisionQuality, moveIndex);

_dda.RecordSignal(SignalKeys.InterMoveInterval, interMoveTime, SignalTier.BehavioralTempo);

if (comboCount > 0)
    _dda.RecordSignal(SignalKeys.SequenceMatch, 1f, SignalTier.StrategicPattern);
```

---

### M4 — `[NEW]` heart_lost

> Fires the instant a heart/life is lost. Direct frustration signal. Unlike song_revive_impression (which fires when ALL lives are gone), this fires on EACH individual life loss.

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 4 | heart_lost | | | | Trigger every time the player loses a heart/life during action phase. | Immediate frustration signal. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | move_index | Integer | [sequential] | | Move number when the heart was lost |
| | | cause | String | wrong_tile | | Heart lost due to tapping wrong tile |
| | | | | timeout | | Heart lost due to time running out |
| | | | | obstacle | | Heart lost due to obstacle or trap tile |
| | | hearts_remaining | Integer | [count] | | Hearts remaining AFTER this loss (0 = out of lives) |
| | | hearts_total | Integer | [count] | | Hearts the player started with |
| | | progress | Integer | 0–100 | | Completion rate when the heart was lost |
| | | time_since_last_heart_lost | Float | [seconds] | | Time since previous heart_lost (0 if first). Rapid successive losses = strong frustration. |

**Cadence Mapping:** `move.waste` (Tier 0), `input.rejected` (Tier 4), `meta.frustration_trigger` (Tier 3 when hearts_remaining == 0)

**Integration Example:**

```csharp
// On heart_lost
_dda.RecordSignal(SignalKeys.MoveWaste, 1f, SignalTier.DecisionQuality, moveIndex);
_dda.RecordSignal(SignalKeys.InputRejected, 1f, SignalTier.RawInput);

if (heartsRemaining == 0)
    _dda.RecordSignal("meta.frustration_trigger", progress / 100f, SignalTier.RetryMeta);

if (timeSinceLastHeartLost > 0f && timeSinceLastHeartLost < 5f)
    _dda.RecordSignal("meta.rapid_failure", 1f, SignalTier.RetryMeta);
```

---

### M5 — `[NEW]` level_pause

> Fires when the player pauses the game. Disengagement signal — frequent or long pauses indicate confusion, frustration, or loss of interest.

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 5 | level_pause | | | | Trigger when the player pauses the game during action phase (pause button, app background). | Disengagement signal. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | trigger | String | button | | Player tapped the pause button |
| | | | | background | | App went to background |
| | | pause_time | Float | [seconds] | | Time elapsed since level start when pause occurred |
| | | progress | Integer | 0–100 | | Completion rate when pause occurred |
| | | hearts_remaining | Integer | [count] | | Hearts remaining when pause occurred |

**Cadence Mapping:** `tempo.pause` (Tier 1)

```csharp
_dda.RecordSignal(SignalKeys.PauseTriggered, 1f, SignalTier.BehavioralTempo);
```

---

### M6 — `[NEW]` idle_detected

> Fires when the player has not interacted with the board for a configurable threshold (recommended: 5 seconds) while the game is still running. Hesitation is the earliest signal of anxiety.

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 6 | idle_detected | | | | Trigger when the player has not tapped any tile for threshold period (default: 5 seconds) during active gameplay. Does NOT fire if game is paused. Fires once per idle period, resets on next tap. | Hesitation/confusion signal. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | idle_duration | Float | [seconds] | | How long the player has been idle when this fires |
| | | move_index | Integer | [sequential] | | Last move index before idle started |
| | | progress | Integer | 0–100 | | Completion rate when idle was detected |
| | | hearts_remaining | Integer | [count] | | Hearts remaining when idle was detected |
| | | tiles_remaining | Integer | [count] | | Tiles remaining when idle was detected |

**Cadence Mapping:** `tempo.hesitation` (Tier 1)

```csharp
_dda.RecordSignal(SignalKeys.HesitationTime, idleDuration, SignalTier.BehavioralTempo);
```

---

### M7 — `[NCT]` song_booster_success

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 7 | song_booster_success | | | | Triggered every time user successfully uses a booster in action phase. | In-session struggle indicator. High usage = player is struggling. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | level_type | String | common \| daily \| secret | | Level category |
| | | booster_name | String | hint \| bomb \| magnet | | Which assist the player needed |
| | | requirement | String | coin \| ad \| stock | | Resource pressure context |

**Cadence Mapping:** `strategy.powerup` (Tier 2)

---

### M8 — `[NCT]` song_revive_impression

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 8 | song_revive_impression | | | | Triggered when the Out-of-Lives popup is shown. | Strong frustration trigger — player ran out of all lives. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | level_type | String | common \| daily \| secret | | Level category |
| | | progress | Integer | 0–100 | | Completion rate at time of death |
| | | count | Integer | [number] | | Times shown in this session. count > 1 = repeated deaths. |

**Cadence Mapping:** `meta.frustration_trigger` (Tier 3)

---

### M9 — `[NCT]` song_revive_click

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 9 | song_revive_click | | | | Triggered when user clicks to revive. | Effort/intent to continue despite failure. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | level_type | String | common \| daily \| secret | | Level category |
| | | progress | Integer | 0–100 | | Completion rate at time of click |
| | | location | String | out_of_live \| are_you_sure | | UX point where revive was clicked |
| | | count | Integer | [number] | | Times revive clicked in this session |

**Cadence Mapping:** `meta.revive_attempt` (Tier 3)

---

### M10 — `[NCT]` song_revive_success

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 10 | song_revive_success | | | | Triggered when user passes revive requirement and game returns to action phase. | Actual survival continuation. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | level_type | String | common \| daily \| secret | | Level category |
| | | requirement | String | coin \| ad | | How the revive was paid for |
| | | progress | Integer | 0–100 | | Completion rate at time of revive |
| | | location | String | out_of_live \| are_you_sure | | UX point where revive happened |
| | | count | Integer | [number] | | Times revived in this session |

**Cadence Mapping:** `meta.revive_success` (Tier 3)

---

## Should Have Events

### S1 — `[NEW]` combo_break

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 11 | combo_break | | | | Trigger when a combo streak of 2+ is broken by a miss or timeout. | Skill pattern signal — combo length reveals player skill. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | combo_length | Integer | [count] | | Length of the combo that just broke (minimum 2) |
| | | move_index | Integer | [sequential] | | Move index when the combo broke |
| | | cause | String | miss \| timeout \| booster | | What broke the combo |
| | | progress | Integer | 0–100 | | Completion rate when combo broke |

**Cadence Mapping:** `strategy.sequence_match` (Tier 2), `move.waste` (Tier 0 on miss/timeout)

---

### S2 — `[NEW]` wrong_tap

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 12 | wrong_tap | | | | Trigger when the player taps an area that is not a valid interactive tile. Does NOT include tile_tap misses. | Raw input noise — confusion or panic indicator. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | move_index | Integer | [sequential] | | Last tile_tap move_index (wrong taps don't increment) |
| | | target | String | empty \| locked_tile \| ui_element \| cooldown_tile | | What the player tapped |
| | | tap_time | Float | [seconds] | | Time elapsed since level start |

**Cadence Mapping:** `input.rejected` (Tier 4)

---

### S3 — `[NEW]` move_efficiency_snapshot

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 13 | move_efficiency_snapshot | | | | Trigger every N moves (configurable, default: 5) during action phase. | Mid-session performance health check. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | move_index | Integer | [sequential] | | Current move number |
| | | total_taps | Integer | [count] | | Total taps so far |
| | | perfect_taps | Integer | [count] | | Perfect taps so far |
| | | good_taps | Integer | [count] | | Good taps so far |
| | | miss_taps | Integer | [count] | | Miss taps so far |
| | | current_efficiency | Float | 0.0–1.0 | | Running accuracy: (perfect + good) / total_taps |
| | | avg_inter_move_time | Float | [seconds] | | Average time between taps over last N moves |
| | | progress | Integer | 0–100 | | Completion rate at this snapshot |
| | | hearts_remaining | Integer | [count] | | Hearts remaining |
| | | boosters_used_so_far | Integer | [count] | | Total boosters used to this point |
| | | elapsed_time | Float | [seconds] | | Time since level start |

**Cadence Mapping:** `resource.efficiency` (Tier 0), `tempo.interval` (Tier 1)

---

### S4 — `[NEW]` song_input_summary

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 14 | song_input_summary | | | | Trigger once at song_result time. Aggregates input metrics for the completed session. | Input quality summary for skill model. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | total_taps | Integer | [count] | | Total tile_tap events |
| | | perfect_count | Integer | [count] | | Taps with result = perfect |
| | | good_count | Integer | [count] | | Taps with result = good |
| | | miss_count | Integer | [count] | | Taps with result = miss |
| | | wrong_tap_count | Integer | [count] | | Total wrong_tap events |
| | | max_combo | Integer | [count] | | Longest combo achieved |
| | | avg_combo_length | Float | [number] | | Average combo length across all combos |
| | | avg_inter_move_time | Float | [seconds] | | Mean time between taps |
| | | inter_move_time_stddev | Float | [seconds] | | Standard deviation of inter-move time. High = erratic tempo. |
| | | idle_count | Integer | [count] | | Number of idle_detected events |
| | | total_idle_time | Float | [seconds] | | Cumulative idle time |
| | | longest_idle | Float | [seconds] | | Longest single idle period — peak confusion moment |

**Cadence Mapping:** `input.accuracy` (Tier 4), `tempo.hesitation` (Tier 1), `input.rejected` (Tier 4)

---

### S5 — `[NCT]` song_booster_click

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 15 | song_booster_click | | | | Triggered every time user clicks to use a booster (may not succeed). | Intent to seek help — even if blocked by economy. |
| | | level_id | String | [id] | | Current level ID |
| | | song_id | String | [id] | | Song name that user plays |
| | | booster_name | String | hint \| bomb \| magnet | | Which assist requested |
| | | requirement | String | coin \| ad \| stock | | coin when broke = economy pressure + desperation |
| | | level_type | String | common \| daily \| secret | | Level category |

**Cadence Mapping:** `strategy.powerup_attempt` (Tier 2)

---

### S6 — `[NCT]` level_streak_update

| No | Event Name | Event Parameter name | Value Type | Parameter Value | Trigger Logic (Description) | Description |
|----|-----------|---------------------|-----------|----------------|---------------------------|-------------|
| 16 | level_streak_update | | | | Trigger whenever there is an update from Level Streak. | Win/loss momentum — feeds StreakDamperRule. |
| | | milestone | Integer | 1 \| 2 \| 3 | | Milestone of level streak |
| | | status | String | active \| reset | | Whether milestone reached or streak broken |

**Cadence Mapping:** `meta.streak_milestone` / `meta.streak_reset` (Tier 3)

---

## Signal Flow Diagram

```
tile_tap ─────────► MoveExecuted      → FlowDetector: efficiency window
                  ► MoveOptimal       → FlowDetector: efficiency window
                  ► MoveWaste         → SessionAnalyzer: FrustrationScore
                  ► InterMoveInterval → FlowDetector: tempo window
                  ► SequenceMatch     → SessionAnalyzer: SequenceMatchRate
                  ► InputAccuracy     → SessionAnalyzer: running accuracy

heart_lost ───────► MoveWaste         → SessionAnalyzer: FrustrationScore
                  ► InputRejected     → FlowDetector: engagement window (push 0)
                  ► FrustrationTrigger→ FrustrationReliefRule

level_pause ──────► PauseTriggered    → FlowDetector: engagement window (push 0)
                                      → SessionAnalyzer: EngagementScore penalty

idle_detected ────► HesitationTime    → FlowDetector: tempo score
                                      → SessionAnalyzer: HesitationTime

combo_break ──────► SequenceMatch     → SessionAnalyzer: SequenceMatchRate
                  ► MoveWaste         → SessionAnalyzer (on miss/timeout)

wrong_tap ────────► InputRejected     → FlowDetector: engagement window (push 0)

song_booster_* ──► PowerUpUsed       → SessionAnalyzer: PowerUpsUsed

song_revive_* ───► meta.*            → FrustrationReliefRule, AdjustmentContext

song_result ──────► ResourceEfficiency→ Glicko-2 update, AdjustmentEngine
(+ move counts)   ► SessionOutcome    → PlayerModel skill rating
```

---

## Implementation Checklist

| Phase | Priority | Events | New / Existing | Status |
|-------|----------|--------|---------------|--------|
| 1 | Must Have | song_start + `par_moves`, song_result + `actual_moves`/`par_moves` | Existing + new params | TODO |
| 2 | Must Have | tile_tap | **NEW** | TODO |
| 3 | Must Have | heart_lost | **NEW** | TODO |
| 4 | Must Have | level_pause | **NEW** | TODO |
| 5 | Must Have | idle_detected | **NEW** | TODO |
| 6 | Must Have | song_booster_success | Existing | TODO |
| 7 | Must Have | song_revive_impression, song_revive_click, song_revive_success | Existing | TODO |
| 8 | Should Have | combo_break | **NEW** | TODO |
| 9 | Should Have | wrong_tap | **NEW** | TODO |
| 10 | Should Have | move_efficiency_snapshot | **NEW** | TODO |
| 11 | Should Have | song_input_summary | **NEW** | TODO |
| 12 | Should Have | song_booster_click | Existing | TODO |
| 13 | Should Have | level_streak_update | Existing | TODO |

---

## Summary

| Priority | Event Count | New / Existing | Purpose |
|----------|-------------|---------------|---------|
| **Must Have** | 10 events (4 new + 6 existing) | tile_tap, heart_lost, level_pause, idle_detected + session lifecycle & revive/booster | Core DDA pipeline — session boundaries, real-time flow detection, frustration signals |
| **Should Have** | 6 events (4 new + 2 existing) | combo_break, wrong_tap, move_efficiency_snapshot, song_input_summary + booster click & streak | Enrichment — skill patterns, input quality, mid-session health checks |

---

## Minimum Viable DDA

If you can only implement the absolute minimum, these **3 events** make the DDA functional:

1. **song_start** (existing) — opens the session
2. **tile_tap** (new) — feeds all 5 signal tiers from a single event
3. **song_result** (existing) — closes the session and triggers Glicko-2 update

Everything else improves accuracy, but `tile_tap` alone gives the FlowDetector enough data to classify Flow, Boredom, Anxiety, and Frustration in real-time.

---

## Volume Considerations

`tile_tap` fires on every player interaction — potentially hundreds per session. If event volume is a concern:

- **For Cadence runtime:** No concern. Signals go into a local ring buffer (default 512 entries), not a network call.
- **For analytics backend:** Batch tile_tap into chunks of 5–10, or use `move_efficiency_snapshot` as a lower-volume alternative.
- **For debugging:** Enable `FileSignalStorage` to persist full signal logs locally. Use the Signal Replay editor window to replay sessions.
