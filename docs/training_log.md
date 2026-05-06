# RoboSortRL Pro — Training Log

## Run: RoboSort_PPO_Baseline_002

**Date:** 2026-05-06  
**Scene:** TrainingScene  
**Behavior Name:** RoboSort  
**Algorithm:** PPO  
**Config:** `config/robosort_ppo_baseline.yaml`  
**Observation setup:** 13 vector observations, no RayPerception yet  
**Action setup:** 3 continuous actions  
**Decision Period:** 5  
**Torch:** 2.2.2+cpu  
**ML-Agents Python:** 1.1.0  
**Unity ML-Agents package:** 4.0.3  

### Result Summary

This was the first successful PPO baseline run after fixing ONNX export compatibility.

The agent showed clear learning progress:

| Step | Mean Reward | Std Reward |
|---:|---:|---:|
| 10,000 | -0.480 | 0.991 |
| 20,000 | -0.204 | 0.997 |
| 30,000 | 0.080 | 0.929 |
| 40,000 | 0.152 | 0.925 |
| 50,000 | 0.423 | 0.719 |
| 70,000 | 0.699 | 0.277 |
| 90,000 | 0.752 | 0.091 |
| 150,000 | 0.753 | 0.100 |
| 180,000 | 0.748 | 0.100 |

### Checkpoints Exported

- `RoboSort-49931.onnx`
- `RoboSort-99987.onnx`
- `RoboSort-149999.onnx`
- `RoboSort-189680.onnx`
- Final copied model: `results/RoboSort_PPO_Baseline_002/RoboSort.onnx`

### Interpretation

The baseline single-cell PPO task is learnable. Mean reward improved from negative values to a stable plateau around `0.74–0.75`.

This confirms:

- Agent action path works.
- Event-based reward path works.
- Episode reset path works.
- PPO trainer connects correctly.
- TensorBoard event output is generated.
- ONNX export works.

### Known Limitation

Training was interrupted around 189,680 steps because the Unity Editor communicator exited. The run still exported a valid final ONNX model. For longer and parallel runs, a Unity executable build should be preferred over Editor training.

### Next Planned Improvements

- Add RayPerceptionSensor3D.
- Add parallel environments.
- Run harder PPO variants.
- Compare multiple YAML configurations in TensorBoard.

---

## Run: RoboSort_PPO_Ray_001

**Date:** 2026-05-06  
**Scene:** TrainingScene  
**Behavior Name:** RoboSort  
**Algorithm:** PPO  
**Config:** `config/robosort_ppo_ray.yaml`  
**Observation setup:** 13 vector observations + RayPerceptionSensor3D  
**Action setup:** 3 continuous actions  
**Decision Period:** 5  
**Torch:** 2.2.2+cpu  
**ML-Agents Python:** 1.1.0  
**Unity ML-Agents package:** 4.0.3  

### Result Summary

This was the first PPO run after adding RayPerceptionSensor3D.

The agent showed clear learning progress:

| Step | Mean Reward | Std Reward |
|---:|---:|---:|
| 10,000 | -0.326 | 1.011 |
| 20,000 | -0.191 | 0.994 |
| 30,000 | 0.193 | 0.880 |
| 40,000 | 0.278 | 0.858 |
| 50,000 | 0.360 | 0.781 |
| 60,000 | 0.589 | 0.523 |
| 70,000 | 0.718 | 0.215 |
| 80,000 | 0.720 | 0.223 |
| 90,000 | 0.759 | 0.091 |
| 100,000 | 0.729 | 0.178 |
| 120,000 | 0.738 | 0.096 |

### Checkpoints Exported

- `RoboSort-49942.onnx`
- `RoboSort-99991.onnx`
- `RoboSort-129256.onnx`
- Final copied model: `results/RoboSort_PPO_Ray_001/RoboSort.onnx`

### Interpretation

RayPerceptionSensor3D did not break the observation pipeline, PPO training, reward routing, or ONNX export.

The ray-enabled model reached a similar reward range to the vector-only baseline. This suggests the current task is already solvable from vector observations, but the project now satisfies the sensor requirement and has a valid comparison between vector-only PPO and vector-plus-ray PPO.

### Known Limitation

Training was manually interrupted around 129,256 steps using Ctrl+C after confirming learning progress and checkpoint export. ML-Agents still exported a valid final ONNX model. For longer and parallel runs, a Unity executable build should be preferred over Editor training.

### Comparison Against Baseline

- `RoboSort_PPO_Baseline_002` reached about `0.752` mean reward around 90k steps.
- `RoboSort_PPO_Ray_001` reached about `0.759` mean reward around 90k steps.

The ray-enabled run is valid, but it does not yet prove a major performance advantage because the current environment is still relatively easy.


---

## Run: RoboSort_PPO_Parallel2_Smoke_001

**Date:** 2026-05-06  
**Scene:** TrainingScene_Parallel  
**Behavior Name:** RoboSort  
**Algorithm:** PPO  
**Config:** `config/robosort_ppo_ray.yaml`  
**Environment setup:** 2 parallel sorting cells in one Unity scene  
**Observation setup:** 13 vector observations + RayPerceptionSensor3D  
**Action setup:** 3 continuous actions  
**Decision Period:** 5  
**Torch:** 2.2.2+cpu  
**ML-Agents Python:** 1.1.0  
**Unity ML-Agents package:** 4.0.3  

### Result Summary

This was the first PPO smoke test using the duplicated prefab-based parallel training scene.

The run confirmed that multiple RoboSort agents can train under the same shared `RoboSort` behavior in one Unity scene.

| Step | Mean Reward | Std Reward |
|---:|---:|---:|
| 10,000 | -0.407 | 1.004 |
| 20,000 | -0.356 | 1.008 |
| 30,000 | -0.076 | 0.971 |
| 40,000 | 0.102 | 0.938 |
| 50,000 | 0.515 | 0.617 |

### Checkpoints Exported

- `RoboSort-49954.onnx`
- `RoboSort-57875.onnx`
- Final copied model: `results/RoboSort_PPO_Parallel2_Smoke_001/RoboSort.onnx`

### Interpretation

The 2-cell parallel scene is valid. Both cells used the same behavior name, trained under the same PPO policy, and produced successful TensorBoard and ONNX outputs.

The 2-cell setup improved sample throughput compared with the single-cell RayPerception run:

- Single-cell Ray run reached 50k steps in about 161 seconds.
- 2-cell smoke run reached 50k steps in about 95 seconds.

This confirms that prefab-based scene-level parallelism is working and is suitable for scaling to more cells.

### Known Limitation

This was a smoke test, not a full convergence run. Training was manually interrupted after confirming learning progress and ONNX export.

---

## Run: RoboSort_PPO_Parallel4_Smoke_001

**Date:** 2026-05-06  
**Scene:** TrainingScene_Parallel  
**Behavior Name:** RoboSort  
**Algorithm:** PPO  
**Config:** `config/robosort_ppo_ray.yaml`  
**Environment setup:** 4 prefab-based parallel sorting cells in one Unity scene  
**Observation setup:** 13 vector observations + RayPerceptionSensor3D  
**Action setup:** 3 continuous actions  
**Decision Period:** 5  
**Torch:** 2.2.2+cpu  
**ML-Agents Python:** 1.1.0  
**Unity ML-Agents package:** 4.0.3  

### Result Summary

This was the first PPO smoke test using the 4-cell parallel training scene.

| Step | Mean Reward | Std Reward |
|---:|---:|---:|
| 10,000 | -0.244 | 1.007 |
| 20,000 | -0.352 | 1.019 |
| 30,000 | -0.004 | 0.967 |
| 40,000 | 0.123 | 0.934 |
| 50,000 | 0.212 | 0.870 |
| 60,000 | 0.578 | 0.539 |
| 70,000 | 0.674 | 0.349 |
| 80,000 | 0.691 | 0.278 |
| 90,000 | 0.688 | 0.267 |
| 100,000 | 0.715 | 0.086 |

### Checkpoints Exported

- `RoboSort-49968.onnx`
- `RoboSort-99997.onnx`
- `RoboSort-105210.onnx`
- Final copied model: `results/RoboSort_PPO_Parallel4_Smoke_001/RoboSort.onnx`

### Interpretation

The 4-cell parallel training scene is valid. Multiple prefab-based RoboSort agents trained under the shared `RoboSort` behavior and produced successful TensorBoard and ONNX outputs.

The run reached 100k steps in about 130 seconds, which is a major throughput improvement over the single-cell Editor runs.

### Follow-up Fix

After this smoke test, `ProductSpawner.useFixedSeed` was disabled on `SortingCell.prefab` so future parallel runs do not use identical fixed random sequences across cells.

### Known Limitation

This was a smoke test, not a full convergence run. Training was manually interrupted after confirming learning progress and ONNX export.

---

## Run: RoboSort_PPO_Parallel8_Smoke_001

**Date:** 2026-05-06  
**Scene:** TrainingScene_Parallel8  
**Behavior Name:** RoboSort  
**Algorithm:** PPO  
**Config:** `config/robosort_ppo_ray.yaml`  
**Environment setup:** 8 prefab-based parallel sorting cells in one Unity scene  
**Observation setup:** 13 vector observations + RayPerceptionSensor3D  
**Action setup:** 3 continuous actions  
**Decision Period:** 5  
**Torch:** 2.2.2+cpu  
**ML-Agents Python:** 1.1.0  
**Unity ML-Agents package:** 4.0.3  

### Result Summary

This was the first PPO run using the 8-cell parallel training scene.

| Step | Mean Reward | Std Reward |
|---:|---:|---:|
| 10,000 | -0.270 | 1.000 |
| 20,000 | -0.324 | 1.008 |
| 30,000 | -0.133 | 0.997 |
| 40,000 | -0.087 | 0.997 |
| 50,000 | 0.246 | 0.852 |
| 60,000 | 0.604 | 0.498 |
| 70,000 | 0.676 | 0.350 |
| 80,000 | 0.725 | 0.230 |
| 90,000 | 0.716 | 0.268 |
| 100,000 | 0.763 | 0.095 |
| 150,000 | 0.751 | 0.101 |
| 200,000 | 0.741 | 0.102 |
| 250,000 | 0.755 | 0.102 |
| 270,000 | 0.758 | 0.103 |

### Checkpoints Exported

- `RoboSort-99986.onnx`
- `RoboSort-149937.onnx`
- `RoboSort-199959.onnx`
- `RoboSort-249989.onnx`
- `RoboSort-273190.onnx`
- Final copied model: `results/RoboSort_PPO_Parallel8_Smoke_001/RoboSort.onnx`

### Interpretation

The 8-cell parallel training scene is valid and significantly faster than the previous Editor training runs.

Throughput comparison:

- Single-cell Ray run reached 100k steps in about 315 seconds.
- 4-cell parallel run reached 100k steps in about 130 seconds.
- 8-cell parallel run reached 100k steps in about 77 seconds.

The model again stabilized around `0.74–0.76` mean reward, suggesting the current performance ceiling is likely caused by task/reward/episode design rather than insufficient sample throughput.

### Known Limitation

Training was manually interrupted after confirming stable learning progress and ONNX export. This run is useful evidence for parallel training, but it is not yet a final convergence/hyperparameter comparison run.

---

## Run: RoboSort_PPO_LargeNet_StatsCheck_001

**Date:** 2026-05-06  
**Scene:** TrainingScene_Parallel8  
**Behavior Name:** RoboSort  
**Algorithm:** PPO  
**Config:** `config/robosort_ppo_large.yaml`  
**Environment setup:** 8 prefab-based parallel sorting cells in one Unity scene  
**Observation setup:** 13 vector observations + RayPerceptionSensor3D  
**Action setup:** 3 continuous actions  
**Decision Period:** 5  

### Result Summary

This was a short verification run after adding custom ML-Agents `StatsRecorder` metrics to `SortingEventRouter`.

| Step | Mean Reward | Std Reward |
|---:|---:|---:|
| 10,000 | -0.413 | 0.992 |
| 20,000 | -0.315 | 1.003 |
| 30,000 | -0.016 | 0.966 |
| 40,000 | 0.176 | 0.910 |
| 50,000 | 0.453 | 0.698 |

### Checkpoints Exported

- `RoboSort-49955.onnx`
- `RoboSort-57442.onnx`
- Final copied model: `results/RoboSort_PPO_LargeNet_StatsCheck_001/RoboSort.onnx`

### TensorBoard Stats Verified

The TensorBoard event file contains the custom sorting diagnostics:

- `RoboSort/TotalOutcomes`
- `RoboSort/Accuracy`
- `RoboSort/DefectRejected`
- `RoboSort/GoodRejected`
- `RoboSort/DefectMissed`
- `RoboSort/GoodAccepted`

### Interpretation

The custom outcome counters are successfully written to TensorBoard. Future PPO runs can now be analyzed by reward trend, sorting accuracy, and per-outcome failure mode.

---

## Run: RoboSort_PPO_LargeNet_Stats_001

**Date:** 2026-05-06  
**Scene:** TrainingScene_Parallel8  
**Behavior Name:** RoboSort  
**Algorithm:** PPO  
**Config:** `config/robosort_ppo_large.yaml`  
**Environment setup:** 8 prefab-based parallel sorting cells in one Unity scene  
**Observation setup:** 13 vector observations + RayPerceptionSensor3D  
**Action setup:** 3 continuous actions  
**Decision Period:** 5  

### Result Summary

This was the first longer 8-cell PPO run after enabling custom TensorBoard outcome diagnostics.

| Step | Mean Reward | Std Reward |
|---:|---:|---:|
| 50,000 | 0.463 | 0.692 |
| 100,000 | 0.733 | 0.166 |
| 150,000 | 0.745 | 0.100 |
| 200,000 | 0.733 | 0.169 |
| 250,000 | 0.757 | 0.102 |
| 300,000 | 0.760 | 0.102 |

### Checkpoints Exported

- `RoboSort-49951.onnx`
- `RoboSort-99947.onnx`
- `RoboSort-149952.onnx`
- `RoboSort-199932.onnx`
- `RoboSort-249933.onnx`
- `RoboSort-299987.onnx`
- `RoboSort-309698.onnx`
- Final copied model: `results/RoboSort_PPO_LargeNet_Stats_001/RoboSort.onnx`

### TensorBoard Outcome Diagnosis

Custom TensorBoard stats showed that the late-stage policy reached perfect sorting accuracy in the final logged windows.

From 210k to 300k steps:

- `RoboSort/Accuracy` remained at `1.0000`.
- `RoboSort/GoodRejected` produced no new late-stage errors.
- `RoboSort/DefectMissed` produced no new late-stage errors.
- `GoodAccepted + DefectRejected = TotalOutcomes` in the final windows.

This means the observed reward plateau around `0.74–0.76` is not caused by incorrect sorting. It is most likely caused by accumulated time penalty before the final sorting outcome.

This changes the interpretation of the baseline: the agent is not merely around 90% accurate. In the late-stage TensorBoard windows, it reaches effectively 100% sorting accuracy under the current task setup.

