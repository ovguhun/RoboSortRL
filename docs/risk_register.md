# Risk Register

## RoboSortRL Pro

This document tracks important technical risks for the RoboSortRL Pro Unity ML-Agents project and how they are controlled.

---

## Core Architecture Risks

| Risk | Impact | Mitigation | Current Status |
|---|---|---|---|
| Visual assets influence training physics | Imported meshes/colliders could change rewards, triggers, ray observations, or product movement | Keep `SimulationRoot` as training truth and `VisualRoot` as visual-only skin | Controlled |
| VisualOnly layer detected by RayPerception | Agent may learn from decorative assets instead of simulation proxies | RayPerception layer mask must exclude `VisualOnly` | Controlled |
| Decorative colliders affect products | Products may bounce, slow, or trigger incorrectly | Disable decorative colliders or place visuals on `VisualOnly` with no physics collisions | Controlled |
| MeshColliders from imported assets enter training scene | Fragile physics and inconsistent training behavior | Do not use imported MeshColliders for training logic | Controlled |

---

## Observation Risks

| Risk | Impact | Mitigation | Current Status |
|---|---|---|---|
| Vector observation size mismatch | ML-Agents behavior parameter errors or invalid trained model compatibility | `RoboSortAgent` validates emitted observation count against Behavior Parameters | Controlled |
| Action size mismatch | Agent action buffer may not match script expectations | `RoboSortAgent` validates 3 continuous actions | Controlled |
| Product type too easy as direct vector label | Agent may ignore RayPerception and solve via direct label lookup | `includeProductTypeObservation` is disabled on `SortingCell.prefab`; hidden slot emits neutral `0.5` | Controlled |
| NaN/Infinity observations | PPO training can silently corrupt | `ClampUnit` and `SafeClamp01` guard observation values | Controlled |
| RayPerception tag misconfiguration | Agent may fail to detect products/zones/walls | Detectable tags include `GoodProduct`, `DefectProduct`, `RejectZone`, `AcceptZone`, and `Wall` | Controlled |
| RayPerception ignores product tags | Sensor-driven task becomes impossible | Verified in Unity Inspector and Play Mode | Controlled |

---

## Reward and Training Risks

| Risk | Impact | Mitigation | Current Status |
|---|---|---|---|
| Reward plateau misinterpreted as poor learning | Could cause unnecessary PPO experiments | TensorBoard stats show accuracy and decisions-at-outcome; plateau explained by time penalty | Controlled |
| Reward hacking through random pushing | Agent may push unnecessarily if not penalized | Reward V2 supports push-discipline penalties, disabled by default for stable final candidate | Monitored |
| Time penalty dominates cumulative reward | Correct outcomes can appear as reward plateau around `0.72–0.76` | Documented interpretation; use accuracy/outcome counters as main success metrics | Controlled |
| Reward V2 destabilizes PPO | Extra shaping could reduce accuracy | Reward V2 is optional and disabled by default | Controlled |
| Training on too-easy direct label task | Project appears less sensor-driven | Final candidate hides product-type vector cue and relies more on RayPerception | Controlled |

---

## Environment and Randomization Risks

| Risk | Impact | Mitigation | Current Status |
|---|---|---|---|
| Spawn randomization creates impossible cases | Product may spawn outside reach or off conveyor | Spawn X range limited to `±0.50` and validated in Play Mode | Controlled |
| Conveyor speed randomization too aggressive | Agent may not have enough time to react | Speed range limited to `1.0–1.6` and validated by PPO run | Controlled |
| Defect probability too realistic during training | Rare defects could make recall unstable and encourage “always accept” behavior | Final candidate uses moderate `0.30` defect probability after validation; balanced `0.50` model is retained as backup/reference; avoid rare `0.10`/`0.05` training without curriculum or reward rebalancing | Controlled |
| Parallel cells share identical random sequences | Reduces training diversity | `useFixedSeed` disabled on `SortingCell.prefab` | Controlled |
| Prefab overrides drift between cells | Parallel cells may behave inconsistently | Use reusable `SortingCell.prefab`; inspect diffs before committing scene changes | Controlled |

---

## Scene and Git Risks

| Risk | Impact | Mitigation | Current Status |
|---|---|---|---|
| Unity ProjectSettings noise gets committed accidentally | Git history polluted with unrelated changes | Restore `ProjectSettings/EditorBuildSettings.asset` and `ProjectSettings/ProjectSettings.asset` unless intentionally changed | Controlled |
| Scene camera/visual edits modify training logic | Could accidentally change colliders, zones, agents, or prefab overrides | Inspect scene diffs before commits | Controlled |
| TrainingScene and DemoScene responsibilities blur | Demo visuals/UI could affect training | Keep TrainingScene simulation-focused; use DemoScene for polished visuals/UI | Pending demo phase |
| ONNX/results files not clearly selected | Final model may be ambiguous | Training log records run IDs and exported checkpoints | Partially controlled |

---

## Demo and Presentation Risks

| Risk | Impact | Mitigation | Current Status |
|---|---|---|---|
| Project looks like only cubes/proxies | Weak final presentation despite strong RL | Use factory visuals as visual-only skin after RL core is stable | Pending |
| UI breaks training logic | UI objects may add colliders/ray targets or affect observations | Prefer screen-space UI; if world-space UI is used, keep it VisualOnly/no colliders/ray ignored | Pending |
| Final video does not show learning evidence | Viewer may not understand RL contribution | Include TensorBoard screenshots, sensor-driven explanation, and before/after policy behavior | Pending |
| Demo model mismatch with final scene | ONNX trained on one setup but demonstrated in another | Use final sensor-driven trained model with matching prefab settings | Pending |

---

## Current Final RL Candidate

The current strongest RL core is:

- `TrainingScene_Parallel8`
- 8 prefab-based parallel cells
- PPO with `config/robosort_ppo_large.yaml`
- 13 vector observations
- RayPerceptionSensor3D
- 3 continuous actions
- randomized spawn X
- randomized conveyor speed
- direct product-type vector cue hidden
- Reward V2 disabled
- near-perfect PPO accuracy after full training

This is the current preferred final RL configuration because it demonstrates sensor-driven reinforcement learning while staying stable and explainable.
