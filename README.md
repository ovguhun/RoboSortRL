# RoboSortRL

**RoboSortRL** is a Unity ML-Agents reinforcement learning project where a kinematic robotic sorter is trained with PPO to reject defective conveyor products while allowing good products to pass.

The project demonstrates an industrial quality-control scenario with continuous control, moving conveyor products, RayPerception sensors, TensorBoard-backed PPO experiments, and a final trained ONNX policy.

**Demo video:** [Watch the final demo on Google Drive](https://drive.google.com/file/d/1g_fRtZahQldUiNSnAivVDNn0IeIdDGaQ/view?usp=sharing)

---

## Final result

The final model was selected from a fine-tuned PPO run after correcting the pusher-contact behavior and adding push-discipline rewards.

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

Across the selected final TensorBoard window, the model accepted good products, rejected defective products, and recorded no false rejects or missed defects.

---

## Why this project is more than a tutorial

RoboSortRL combines the core ML-Agents concepts from smaller lab exercises into a larger industrial simulation:

- a 3D Unity environment,
- moving conveyor products,
- randomized spawn positions,
- randomized conveyor speeds,
- 8 parallel training cells,
- vector observations plus `RayPerceptionSensor3D`,
- 3 continuous actions,
- custom PPO YAML files,
- multiple PPO/hyperparameter comparisons,
- TensorBoard experiment evidence,
- a polished visual demo scene.

---

## Core architecture decision

The most important engineering rule in the project is the separation between simulation truth and visual assets:

```text
SimulationRoot = training truth
VisualRoot     = visual-only factory/demo skin
```

The imported factory visuals are used only for presentation. They do not control:

- reward logic,
- observations,
- triggers,
- physics,
- product spawning,
- RayPerception targets,
- episode reset logic.

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

Additional shaping and discipline:

| Component | Value |
|---|---:|
| No-product push penalty | `-0.005` |
| Good-product push penalty | `-0.01` |
| Push penalty threshold | `0.5` actual mapped push strength |
| Defect alignment progress reward scale | `0.01` |
| Max defect alignment reward per decision | `0.003` |
| Defect alignment zone padding | `0.75` |

Reward standard deviation is not expected to collapse to near zero because the reward distribution is intentionally asymmetric. Good accepted products and rejected defects receive different rewards, so outcome counters are the main success metrics.

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

The final evidence includes:

- `tensorboard_07_final_actuator_Accuracy.png`
- `tensorboard_07_final_actuator_Cumulative_Reward.png`
- `tensorboard_07_final_actuator_Defect_Rejected.png`
- `tensorboard_07_final_actuator_Defect_Missed.png`
- `tensorboard_07_final_actuator_GoodProductPushPenalty.png`
- `tensorboard_07_final_actuator_NoProductPushPenalty.png`

These screenshots show that the final policy reaches near-perfect sorting accuracy while push-discipline penalties decrease during fine-tuning.

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

1. Clone/open the Unity project.
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
- imported factory visuals remain visual-only,
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

This is not a high-fidelity robot-arm or sim-to-real robotics simulator. The sorter uses stable kinematic proxy mechanics so PPO can learn from reliable rewards and repeatable environment dynamics.

Main limitations:

- no full 6-axis robot arm,
- no real gripping,
- no camera-based CNN perception,
- no sim-to-real transfer validation,
- product geometry is simplified,
- factory assets are visual-only.

These choices were intentional because the project goal is reinforcement learning policy training, not fragile mechanical simulation.

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

This project is reinforcement learning, not imitation learning.

It does not use:

- demonstration recording,
- `.demo` files,
- behavioral cloning,
- GAIL,
- expert action labels.

The final model is trained with PPO using environment rewards and TensorBoard-guided iteration.
