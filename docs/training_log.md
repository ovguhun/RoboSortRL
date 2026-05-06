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
