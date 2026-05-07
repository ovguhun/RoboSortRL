# Observations, Actions, and Rewards

## RoboSortRL Pro — Current RL Setup

RoboSortRL Pro is a Unity ML-Agents PPO reinforcement learning project. The agent controls a kinematic industrial sorter that learns to reject defective products while allowing good products to pass.

The current final RL candidate uses:

- 8 prefab-based parallel training cells
- Vector observations
- RayPerceptionSensor3D
- 3 continuous actions
- Event-based rewards
- Randomized product spawn X
- Randomized conveyor speed
- Hidden direct product-type vector cue so RayPerception is more important

---

## Behavior

**Behavior Name:** `RoboSort`

**Algorithm:** PPO

**Vector Observation Size:** `13`

**Continuous Actions:** `3`

---

## Vector Observations

The agent emits 13 vector observations.

### Product existence

| Index group | Meaning |
|---|---|
| 1 value | Whether an active unprocessed product exists |

### Product observations

The product observation block has 9 values.

| Observation | Meaning |
|---|---|
| Product local X position | Product lateral position relative to observation root |
| Product local Z position | Product conveyor-direction position relative to observation root |
| Product local X velocity | Product lateral velocity |
| Product local Z velocity | Product conveyor-direction velocity |
| Product type slot | Current final setup hides direct product type and emits neutral `0.5` |
| Inside sorting zone | Whether product is inside the sorting zone trigger |
| Distance to reject zone | Normalized distance to RejectZone |
| Distance to accept zone | Normalized distance to AcceptZone |
| Conveyor speed | Normalized current product conveyor speed |

### Sorter observations

The sorter observation block has 3 values.

| Observation | Meaning |
|---|---|
| Normalized carriage position | Lateral/longitudinal sorter alignment value |
| Pusher extension amount | Current pusher extension in `[0, 1]` |
| Push strength | Current push strength in `[0, 1]` |

---

## Sensor Observations

The project uses `RayPerceptionSensor3D` to give the policy spatial sensor information.

Detectable training tags include:

- `GoodProduct`
- `DefectProduct`
- `RejectZone`
- `AcceptZone`
- `Wall`

Important safety rule:

- `VisualOnly` must not be detected by RayPerception.
- Imported factory visuals must not affect observations, rewards, triggers, or physics.

---

## Observation Hardening

The current final candidate disables the direct product-type vector cue on `SortingCell.prefab`.

This means:

- The observation slot is preserved.
- Observation size remains `13`.
- The hidden product-type slot emits neutral `0.5`.
- The policy must rely more on RayPerception tags and spatial behavior instead of simple direct label lookup.

This makes the task more meaningful than the easier baseline where the vector observation directly encoded:

- Good product = `0`
- Defective product = `1`

---

## Continuous Actions

The action space has 3 continuous actions.

| Action index | Meaning |
|---:|---|
| 0 | Sorter carriage movement / alignment |
| 1 | Pusher extension and retraction |
| 2 | Push activation / push strength |

The agent does not move transforms directly. It only sends controls to `SorterController.SetControl()`.

---

## Main Reward V1

Reward V1 is the current active reward system.

| Outcome | Reward |
|---|---:|
| Good product accepted | `+1.0` |
| Defective product rejected | `+1.0` |
| Good product wrongly rejected | `-1.0` |
| Defective product missed / accepted | `-1.0` |
| Time penalty per decision | `-0.001` |

The agent ends the episode after a sorting outcome.

---

## Reward V2 Support

Reward V2 support exists in code but is disabled by default.

Current default:

- `Use Reward V2 = false`

Reward V2 can optionally add:

- Correct-sort speed bonus
- No-product push penalty
- Good-product push penalty
- TensorBoard counters for shaping diagnostics

This is kept disabled for the current stable final RL candidate.

---

## TensorBoard Diagnostics

Custom TensorBoard metrics include:

- `RoboSort/Accuracy`
- `RoboSort/TotalOutcomes`
- `RoboSort/GoodAccepted`
- `RoboSort/DefectRejected`
- `RoboSort/GoodRejected`
- `RoboSort/DefectMissed`
- `RoboSort/DecisionsAtOutcome`

Optional Reward V2 metrics include:

- `RoboSort/SpeedBonus`
- `RoboSort/NoProductPushPenalty`
- `RoboSort/GoodProductPushPenalty`

---

## Current Task Randomization

The current final candidate uses controlled randomization on the reusable `SortingCell.prefab`.

| Setting | Value |
|---|---|
| Randomize Spawn X | Enabled |
| Spawn X Range | `±0.50` |
| Randomize Conveyor Speed | Enabled |
| Min Conveyor Speed | `1.0` |
| Max Conveyor Speed | `1.6` |
| Defect Probability | `0.30` final candidate; `0.50` retained as balanced backup/reference |
| Use Fixed Seed | Disabled |

This makes the task harder while keeping the observation size, action size, rewards, tags, and zone logic stable.

---

## Final RL Interpretation

The strongest current run is the sensor-driven product-type task.

In this setup:

- The agent does not receive direct Good/Defect type in vector observations.
- RayPerception becomes materially more important.
- PPO still reaches near-perfect sorting accuracy.
- Learning is slower and noisier than the easier direct-type baseline.
- The reward plateau around `0.72–0.76` is explained by accumulated time penalty, not by poor sorting accuracy.

This is the preferred final RL core because it demonstrates sensor-driven reinforcement learning rather than simple vector-label lookup.
