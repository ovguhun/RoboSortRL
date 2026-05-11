# RoboSortRL

**RoboSortRL** is a Unity ML-Agents reinforcement learning project where a kinematic robotic sorter is trained with PPO to reject defective conveyor products while allowing good products to pass.

The project simulates an industrial quality-control task with moving conveyor products, sensor-based observations, continuous control, TensorBoard-backed PPO experiments, and a final trained ONNX policy.

---

## Final result

The selected final policy was trained after correcting the pusher-contact behavior and fine-tuning the agent with push-discipline rewards.

| Item | Final value |
|---|---|
| Final model | `Assets/_RoboSortRL/Training/Models/RoboSort_ActuatorPenaltyFix_Defect35_Baseline_950k_Candidate.onnx` |
| Final run | `RoboSort_PPO_PushDiscipline_ActuatorPenaltyFix_Baseline_001` |
| Selected checkpoint | `RoboSort-949965.onnx` |
| Algorithm | PPO |
| Network | 128 hidden units, 2 layers |
| Training scene | `TrainingScene_Parallel8` |
| Demo scene | `DemoScene` |

Selected checkpoint metrics:

| Metric | Value |
|---|---:|
| Mean reward | `0.8374` |
| Accuracy | `1.0000` |
| GoodAccepted | `110` |
| DefectRejected | `69` |
| GoodRejected | `0` |
| DefectMissed | `0` |
| TotalOutcomes | `179` |

Across the selected TensorBoard window, the policy accepted good products, rejected defective products, and recorded no false rejects or missed defects.

---

## Project highlights

- Unity 3D industrial conveyor environment
- PPO reinforcement learning with Unity ML-Agents
- 8 parallel training cells in `TrainingScene_Parallel8`
- Moving conveyor products with randomized spawn positions and conveyor speeds
- Vector observations plus `RayPerceptionSensor3D`
- 3 continuous actions
- Custom PPO YAML files
- Multiple PPO/hyperparameter comparisons
- TensorBoard diagnostics and final evidence screenshots
- Visual-only factory demo scene
- Final trained ONNX policy used in `DemoScene`

---

## Architecture note

The project keeps training logic and presentation assets separate:

```text
SimulationRoot = training truth
VisualRoot     = visual-only factory/demo skin
```

Factory visuals are used as visual-only presentation assets. Simulation proxies remain responsible for rewards, observations, triggers, physics, product spawning, ray targets, and episode reset logic.

This keeps PPO training stable while allowing the demo scene to look like an industrial environment.

---

## RL setup

| Component | Final setup |
|---|---|
| Behavior name | `RoboSort` |
| Algorithm | PPO |
| Vector observation size | `13` |
| Action space | `3` continuous actions |
| Sensors | Vector observations + RayPerceptionSensor3D |
| Direct product-type vector cue | Hidden |
| Defect probability | `0.35` |
| Reward V2 | Enabled for push discipline |
| Correct-sort speed bonus | Disabled (`0`) |
| Defect alignment shaping | Enabled |
| Demo behavior type | Inference Only |

### Actions

| Action index | Meaning |
|---:|---|
| `0` | Sorter carriage movement along Z |
| `1` | Pusher extension / retraction along X |
| `2` | Push activation / strength |

### Rewards

| Outcome | Reward |
|---|---:|
| Good product accepted | `+1.0` |
| Defect product rejected | `+1.5` |
| Good product wrongly rejected | `-1.5` |
| Defect product missed | `-2.0` |
| Time penalty per decision | `-0.001` |

Additional shaping and push discipline:

| Component | Value |
|---|---:|
| No-product push penalty | `-0.005` |
| Good-product push penalty | `-0.01` |
| Push penalty threshold | `0.5` actual mapped push strength |
| Defect alignment progress reward scale | `0.01` |
| Max defect alignment reward per decision | `0.003` |
| Defect alignment zone padding | `0.75` |

Reward standard deviation is not expected to collapse to near zero because the final reward distribution is intentionally asymmetric. Outcome counters are the primary success metrics.

---

## Training run history

| Run | Purpose | Result |
|---|---|---|
| `RoboSort_PPO_Baseline_002` | Initial PPO baseline | Proved basic sorting learnability |
| `RoboSort_PPO_Ray_001` | Add RayPerceptionSensor3D | Confirmed ray-enabled training works |
| `RoboSort_PPO_Parallel8_Smoke_001` | Validate 8 parallel cells | Improved training throughput |
| `RoboSort_PPO_LargeNet_Parallel8_001` | Larger network comparison | Useful comparison, not final |
| `RoboSort_PPO_SensorDrivenType_Defect30_001` | Earlier strong sensor-driven candidate | Near-perfect previous candidate |
| `RoboSort_PPO_PusherContact_OutcomeAlign_Baseline_001` | Corrected pusher contact + asymmetric rewards + alignment shaping | Strong 128-unit policy |
| `RoboSort_PPO_PusherContact_PushDiscipline_Defect35_Baseline_001` | Defect35 fine-tune with push discipline | Strong final-family candidate |
| `RoboSort_PPO_PushDiscipline_ActuatorPenaltyFix_Baseline_001` | Final actuator-aligned push-penalty fix | Selected final demo model |

---

## TensorBoard evidence

Final TensorBoard screenshots are stored in:

```text
media/screenshots/tensorboard/
```

Final evidence includes:

- `tensorboard_07_final_actuator_Accuracy.png`
- `tensorboard_07_final_actuator_Cumulative_Reward.png`
- `tensorboard_07_final_actuator_Defect_Rejected.png`
- `tensorboard_07_final_actuator_Defect_Missed.png`
- `tensorboard_07_final_actuator_GoodProductPushPenalty.png`
- `tensorboard_07_final_actuator_NoProductPushPenalty.png`

---

## Requirements

The project was developed and tested with:

| Tool | Version |
|---|---|
| Unity | Unity 6.3 LTS / `6000.3.14f1` |
| Unity ML-Agents package | `4.0.3` |
| Python | `3.10.12` |
| ML-Agents Python package | `1.1.0` |
| PyTorch | `2.2.2+cpu` |
| OS used for development | Windows 11 |

---

## How to run the demo

1. Open the Unity project.
2. Open:

   ```text
   Assets/_RoboSortRL/Scenes/DemoScene.unity
   ```

3. Select the demo `RoboSortAgent`.
4. Confirm `Behavior Parameters`:

   - Behavior Name: `RoboSort`
   - Behavior Type: `Inference Only`
   - Model: `RoboSort_ActuatorPenaltyFix_Defect35_Baseline_950k_Candidate`
   - Continuous Actions: `3`

5. Press Play.

Expected behavior:

- defective products are rejected with visible pusher contact,
- good products pass through the accept path,
- factory visuals remain visual-only,
- training simulation logic remains proxy-based.

---

## Training command example

Activate the ML-Agents environment and move to the project root:

```powershell
conda activate mlagents
cd path/to/RoboSortRL
```

Example PPO training command from scratch:

```powershell
mlagents-learn config/robosort_ppo_baseline.yaml `
    --run-id=RoboSort_PPO_Example_Run `
    --results-dir results `
    --torch-device cpu
```

The final selected model was produced through iterative PPO training and fine-tuning. See `docs/final_model_update.md` for the final model explanation.

---

## Limitations

RoboSortRL is an RL-focused industrial sorting simulation, not a high-fidelity robotics or sim-to-real system.

The project intentionally uses stable kinematic proxy mechanics so PPO can learn from reliable reward signals and repeatable environment dynamics.

---

## Documentation

Current final source of truth:

- `README.md`
- `docs/final_model_update.md`

Architecture and safety policy:

- `docs/asset_import_policy.md`

Historical experiment/reference documents:

- `docs/training_log.md`
- `docs/training_log_pusher_contact_v1.md`
- `docs/final_rl_summary.md`
- `docs/observations_actions_rewards.md`
- `docs/risk_register.md`

The historical documents are kept intentionally because they show the project evolution: baseline PPO, RayPerception, parallel training, pusher-contact correction, reward tuning, and final model selection.

---

## Methodology

This project uses reinforcement learning, not imitation learning.

It does not use:

- demonstration recordings
- `.demo` files
- behavioral cloning
- GAIL
- expert action labels

The final policy was trained with PPO using environment rewards and TensorBoard-guided iteration.
