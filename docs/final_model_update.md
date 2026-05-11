# RoboSortRL Pro — Final Model Update

## Final selected model

`Assets/_RoboSortRL/Training/Models/RoboSort_ActuatorPenaltyFix_Defect35_Baseline_950k_Candidate.onnx`

## Final selected run

`RoboSort_PPO_PushDiscipline_ActuatorPenaltyFix_Baseline_001`

Selected checkpoint:

`RoboSort-949965.onnx`

## Why this became the final candidate

The original Defect30 candidate was strong, but later demo testing showed that after the pusher-contact correction, the agent could still show slight good-product push/touch behavior. The final candidate improves demo behavior and robustness by combining:

- corrected pusher contact volume,
- asymmetric defect-focused outcome rewards,
- defect-alignment discovery shaping,
- Reward V2 push-discipline penalties,
- an actuator-aligned push penalty fix,
- a harder `0.35` defect probability training setup.

This remains a PPO reinforcement learning setup. It does not use imitation learning, demonstrations, behavioral cloning, or GAIL.

## Final training setup

| Item | Final value |
|---|---|
| Algorithm | PPO |
| Network | 128 hidden units, 2 layers |
| Training scene | `TrainingScene_Parallel8` |
| Demo scene | `DemoScene` |
| Action space | 3 continuous actions |
| Vector observation size | 13 |
| Sensors | Vector observations + RayPerceptionSensor3D |
| Direct product-type vector cue | Hidden |
| Defect probability | `0.35` |
| Pusher contact volume | Corrected narrow contact volume |
| Reward V2 | Enabled for push discipline |
| Correct-sort speed bonus | Disabled (`0`) |
| Defect alignment shaping | Enabled |
| Final demo behavior type | Inference Only |

## Final reward design

| Outcome | Reward |
|---|---:|
| Good product accepted | `+1.0` |
| Defect product rejected | `+1.5` |
| Good product wrongly rejected | `-1.5` |
| Defect product missed | `-2.0` |
| Time penalty per decision | `-0.001` |

Additional shaping and discipline:

| Component | Value |
|---|---:|
| No-product push penalty | `-0.005` |
| Good-product push penalty | `-0.01` |
| Push penalty threshold | `0.5` actual mapped push strength |
| Defect alignment progress reward scale | `0.01` |
| Max defect alignment reward per decision | `0.003` |
| Defect alignment zone padding | `0.75` |

## Final checkpoint metrics

Metrics from the selected 950k checkpoint window:

| Metric | Value |
|---|---:|
| Mean reward | `0.8374` |
| Accuracy | `1.0000` |
| GoodAccepted | `110` |
| DefectRejected | `69` |
| GoodRejected | `0 / not found in final window` |
| DefectMissed | `0 implied in final window` |
| TotalOutcomes | `179` |

## Important interpretation

Reward standard deviation does not need to approach zero in the final setup because the reward distribution is intentionally asymmetric:

- good accepted products receive approximately `+1.0` minus time penalty,
- rejected defects receive approximately `+1.5` minus time penalty,
- penalties and timing vary by episode.

Therefore, the primary success metrics are outcome counters, especially:

- `RoboSort/Accuracy`,
- `RoboSort/DefectRejected`,
- `RoboSort/DefectMissed`,
- `RoboSort/GoodRejected`.

## Demo validation

The final DemoScene uses:

`RoboSort_ActuatorPenaltyFix_Defect35_Baseline_950k_Candidate.onnx`

Observed demo behavior:

- defective products are rejected with clean visible contact,
- no magic push behavior was observed,
- good products may receive minor inspection/touch behavior but are not rejected,
- console was clean during demo validation.

## Previous candidate status

The previous documented final candidate:

`RoboSort_SensorDrivenType_Defect30_Final.onnx`

is retained as an earlier strong baseline/reference model, but it is no longer the selected final demo model.

## Defense explanation

The final model demonstrates that a PPO-trained kinematic sorter can learn robust industrial defect rejection using stable proxy simulation, ray/vector observations, continuous control, event-based rewards, and TensorBoard-guided iteration.

The final training work improved visual/physical realism by aligning the scripted pusher contact with the visible pusher and then retraining/fine-tuning the policy under a harder defect distribution and push-discipline constraints.
