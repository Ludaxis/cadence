# Cadence DDA — Event Mapping & Priority Matrix

> This document maps analytics events to Cadence DDA signal tiers, prioritized by impact on difficulty adaptation. Only **P0–P2 events** are required for a functional DDA pipeline. P3 events are optional enrichment.

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

## P0 — Session Lifecycle 🔴 CRITICAL

> ⚠️ These events define the session boundaries. Without them, `DDAService.BeginSession()` and `EndSession()` cannot function. Implementation is **mandatory**.

| Analytics Event | Cadence Signal | Tier | Parameters Used by DDA | When It Fires |
|----------------|---------------|------|----------------------|---------------|
| **song_start** | `session.started` | — | `level_id` — level identifier | When the player starts a level |
| | | | `attempt` — cumulative attempt count for this level | |
| | | | `play_type` — start \| restart \| replay | |
| | | | `level_type` — common \| daily \| secret | |
| | | | `live_status` — default \| streak_buff_1/2/3 \| infinite | |
| **song_result** | `session.ended` + `session.outcome` | — | `result` — win \| lose | When the result screen appears (win or lose) |
| | | | `progress` — 0–100 completion rate | |
| | | | `playtime` — seconds to complete | |
| | | | `perfect_percentage` — 0–100 accuracy rate | |
| | | | `hint_used` — count | |
| | | | `bomb_used` — count | |
| | | | `magnet_used` — count | |
| | | | `continue` — revive count | |

**Integration Example — Session Lifecycle:**

```csharp
// On song_start
_dda.BeginSession(levelId, new Dictionary<string, float>
{
    { "attempt", attemptCount },
    { "level_type_code", levelTypeCode },  // 0=common, 1=daily, 2=secret
    { "live_status_code", liveStatusCode } // 0=default, 1-3=streak_buff, 4=infinite
});

// On song_result
_dda.RecordSignal(SignalKeys.ProgressDelta, progress / 100f, SignalTier.DecisionQuality);
_dda.RecordSignal(SignalKeys.InputAccuracy, perfectPercentage / 100f, SignalTier.RawInput);
_dda.EndSession(result == "win" ? SessionOutcome.Win : SessionOutcome.Lose);
```

---

## P1 — In-Session Signals 🟡 HIGH

> ℹ️ These fire during active gameplay and feed the flow detector's real-time state classification (Flow, Boredom, Anxiety, Frustration).

| Analytics Event | Cadence Signal | Tier | Parameters Used by DDA | Why It Matters |
|----------------|---------------|------|----------------------|----------------|
| **song_booster_success** | `strategy.powerup` | Tier 2 | `booster_name` — hint \| bomb \| magnet | Confirmed booster use reveals when the player leans on assists. High usage = struggling. |
| | | | `requirement` — coin \| ad \| stock | Requirement type shows economy pressure. |
| **song_revive_impression** | `meta.frustration_trigger` | Tier 3 | `progress` — 0–100 at time of death | The player hit a wall — out of lives. |
| | | | `count` — times shown in this session | `count > 1` = repeated deaths in one session. |
| **song_revive_click** | `meta.revive_attempt` | Tier 3 | `location` — out_of_live \| are_you_sure | Player chose to continue despite failure. |
| | | | `count` — times in this session | Willingness to invest signals engagement despite difficulty. |
| **song_revive_success** | `meta.revive_success` | Tier 3 | `requirement` — coin \| ad | Revive completed — player invested resources to keep going. |
| | | | `progress` — 0–100 at revive | Combined with outcome, tells us if the investment paid off. |
| | | | `count` — times in this session | |

**Integration Example — In-Session Signals:**

```csharp
// On song_booster_success
_dda.RecordSignal(SignalKeys.PowerUpUsed, 1f, SignalTier.StrategicPattern);

// On song_revive_impression
_dda.RecordSignal("meta.frustration_trigger", progress / 100f, SignalTier.RetryMeta);

// On song_revive_click
_dda.RecordSignal("meta.revive_attempt", count, SignalTier.RetryMeta);

// On song_revive_success
_dda.RecordSignal("meta.revive_success", 1f, SignalTier.RetryMeta);
```

---

## P2 — Between-Session Enrichment 🔵 MEDIUM

> ℹ️ These enrich the player model between sessions and feed `StreakDamperRule` and `FrustrationReliefRule`.

| Analytics Event | Cadence Signal | Tier | Parameters Used by DDA | Why It Matters |
|----------------|---------------|------|----------------------|----------------|
| **song_start.attempt** | `meta.attempt` | Tier 3 | `attempt` — cumulative count | High attempt count = player is stuck. Direct input to `FrustrationReliefRule`. |
| **song_start.play_type** | `meta.play_type` | Tier 3 | `play_type` — start \| restart \| replay | `restart` = failed and retrying. `replay` = mastered, replaying for fun (skip DDA). |
| **song_booster_click** | `strategy.powerup_attempt` | Tier 2 | `booster_name` — hint \| bomb \| magnet | Booster tap that may not succeed (can't afford). |
| | | | `requirement` — coin \| ad \| stock | `requirement = coin` when broke = economy pressure signal. |
| **level_streak_update** | `meta.streak` | Tier 3 | `milestone` — 1 \| 2 \| 3 | Win streak milestones and resets. |
| | | | `status` — active \| reset | Directly feeds `StreakDamperRule` to prevent overcorrection. |
| **song_result (booster counts)** | `strategy.booster_total` | Tier 2 | `hint_used` — count | Total boosters consumed in a session. |
| | | | `bomb_used` — count | High total = player needed heavy assistance. |
| | | | `magnet_used` — count | Zero = player relied on skill alone. |
| **song_result.playtime** | `tempo.session_duration` | Tier 1 | `playtime` — seconds | Abnormally long = struggling. Abnormally short = too easy or gave up. |

**Integration Example — Between-Session:**

```csharp
// On song_start — capture attempt and play_type
_dda.RecordSignal(SignalKeys.AttemptNumber, attemptCount, SignalTier.RetryMeta);

// On level_streak_update
if (status == "reset")
    _dda.RecordSignal("meta.streak_reset", milestone, SignalTier.RetryMeta);
else
    _dda.RecordSignal("meta.streak_milestone", milestone, SignalTier.RetryMeta);

// On song_result — aggregate booster dependency
float totalBoosters = hintUsed + bombUsed + magnetUsed;
_dda.RecordSignal(SignalKeys.PowerUpUsed, totalBoosters, SignalTier.StrategicPattern);
_dda.RecordSignal(SignalKeys.InterMoveInterval, playtime, SignalTier.BehavioralTempo);
```

---

## P3 — Economy & Profile Context 🟢 LOW

> ℹ️ Optional enrichment signals. These don't fire during gameplay but provide cross-session context for the player model. Implement after P0–P2 are validated.

| Analytics Event | Cadence Signal | Tier | Parameters Used by DDA | Why It Matters |
|----------------|---------------|------|----------------------|----------------|
| **item_earned** | `resource.earned` | Tier 2 | `item_id` — currency type | Tracks resource income. |
| | | | `amount` — quantity | Low earn rate + high spend = economy squeeze. |
| | | | `source` — common_level_reward \| daily_level_reward \| treasure_hunt \| gold_tile | |
| **item_spent** | `resource.spent` | Tier 2 | `item_id` — currency type | Tracks spend on boosters. |
| | | | `amount` — quantity | Spend rate rising = compensating for difficulty with economy. |
| | | | `source` — hint \| bomb \| magnet | |
| **booster_update** | `resource.stored` | Tier 2 | `booster_name` — hint \| bomb \| magnet \| infinite_lives | Inventory snapshot. |
| | | | `amount` — quantity | Empty inventory + high difficulty = no safety net. |
| **User property: total_level_played** | *(profile enrichment)* | — | `total_level_played` — integer | Player maturity indicator. More levels = more Glicko-2 confidence. |
| **User property: coin_balance** | *(profile enrichment)* | — | `coin_balance` — integer | Economy health check. Near-zero constrains booster access. |
| **me_start / me_result** | *(duplicate filtering)* | — | Same as song_start / song_result | Only fires if playtime >= 15s. Filters accidental starts. |

---

## P4 — Not Needed for DDA ⚪ SKIP

> These events serve analytics, monetization, or UI tracking purposes. They carry no gameplay performance signal and should **not** be wired to Cadence.

| Category | Events | Reason to Skip |
|----------|--------|---------------|
| UI Navigation | `song_impression`, `song_click`, `song_ap`, `screen_open`, `popup_open`, `button_click` | Browsing and navigation — no gameplay skill signal |
| Ad Monetization | `fullads_request`, `fullads_show`, `rewarded_*`, `ad_impression`, all Firebase/AppsFlyer ad events | Business metrics — ad exposure does not indicate player skill |
| IAP | `iap_impression`, `iap_click`, `iap_purchased` | Purchase behavior — monetization tracking, not performance |
| Subscriptions | `sub_cata_show`, `sub_cata_click`, `sub_purchased` | Subscription funnel — business metrics only |
| FTUE | `ftue_main` (step_01 through step_28) | Tutorial tracking — DDA should be disabled entirely during onboarding |
| Collection | `artwork_unlock`, `artwork_click` | Meta-game progression — no difficulty signal |
| Cosmetics | `avatar_update`, `frame_update` | Cosmetic customization — irrelevant to difficulty |
| Treasure Hunt | `treasure_hunt_update`, `treasure_hunt_reward` | Meta-game progress — does not reflect in-level performance |
| Account | `login`, all ULS events | Authentication and social — no gameplay data |
| Content Metadata | All ACM events | Content catalog tagging — infrastructure, not gameplay |

---

## Implementation Checklist

| Phase | Priority | Events to Wire | Cadence APIs | Status |
|-------|----------|---------------|-------------|--------|
| **Phase 1** | 🔴 P0 | `song_start`, `song_result` | `BeginSession()`, `EndSession()`, `RecordSignal(ProgressDelta)`, `RecordSignal(InputAccuracy)` | TODO |
| **Phase 2** | 🟡 P1 | `song_booster_success`, `song_revive_impression`, `song_revive_click`, `song_revive_success` | `RecordSignal(PowerUpUsed)`, `RecordSignal(meta.frustration_trigger)`, `RecordSignal(meta.revive_*)` | TODO |
| **Phase 3** | 🔵 P2 | `song_start.attempt/play_type`, `song_booster_click`, `level_streak_update`, booster counts from `song_result` | `RecordSignal(AttemptNumber)`, `RecordSignal(meta.streak*)`, `RecordSignal(InterMoveInterval)` | TODO |
| **Phase 4** | 🟢 P3 | `item_earned`, `item_spent`, `booster_update`, user properties | `RecordSignal(ResourceStored)`, player profile enrichment | TODO |

---

## Summary

| Priority | Event Count | Purpose |
|----------|-------------|---------|
| 🔴 **P0 CRITICAL** | 2 events | Session open/close — DDA cannot function without these |
| 🟡 **P1 HIGH** | 4 events | In-session flow detection — booster use, revive behavior |
| 🔵 **P2 MEDIUM** | 6 signals (from 4 events) | Between-session model enrichment — retry, streak, tempo |
| 🟢 **P3 LOW** | 5 events + 2 user properties | Economy context and profile enrichment — optional |
| ⚪ **P4 SKIP** | ~50+ events | Ads, IAP, navigation, FTUE, cosmetics, content metadata — not wired to DDA |
