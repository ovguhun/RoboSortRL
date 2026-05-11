# RoboSortRL

**RoboSortRL** is a Unity ML-Agents reinforcement learning project where a PPO-trained kinematic robotic sorter learns to reject defective conveyor products while allowing good products to pass.

The project is framed as an industrial quality-control simulation for robotics and reinforcement learning.

---

## Final status

Final selected demo model:

`Assets/_RoboSortRL/Training/Models/RoboSort_ActuatorPenaltyFix_Defect35_Baseline_950k_Candidate.onnx`

Final selected run:

`RoboSort_PPO_PushDiscipline_ActuatorPenaltyFix_Baseline_001`

Selected checkpoint:

`RoboSort-949965.onnx`

Final checkpoint metrics:

| Metric | Value |
|---|---:|
| Mean reward | `0.8374` |
| Accuracy | `1.0000` |
| GoodAccepted | `110` |
| DefectRejected | `69` |
| GoodRejected | `0 / not found in final window` |
| DefectMissed | `0 / not found in final window` |
| TotalOutcomes | `179` |

---

## Project features

- Unity 3D industrial conveyor environment
- PPO reinforcement learning with Unity ML-Agents
- 8 parallel training cells in `TrainingScene_Parallel8`
- Moving conveyor products
- Kinematic robotic sorter / pusher
- 3 continuous actions
- Vector observations plus RayPerceptionSensor3D
- Custom PPO YAML configs
- TensorBoard experiment tracking
- Multiple PPO/hyperparameter comparisons
- Final visual demo scene using factory-style visual polish
- Visual assets separated from training simulation logic

---

## Architecture rule

The project separates training logic from visual assets:

```text
SimulationRoot = training truth
VisualRoot     = visual-only factory/demo skin
```

Imported visual assets do not control reward logic, observations, triggers, physics, product spawning, RayPerception targets, or episode reset logic.

This keeps PPO training stable while allowing the final demo scene to look more professional.

### Final RL setup

| Item | Final value |
|---|---|
| Algorithm | PPO |
| Network | 128 hidden units, 2 layers |
| Training scene | TrainingScene_Parallel8 |
| Demo scene | DemoScene |
| Action space | 3 continuous actions |
| Vector observation size | 13 |
| Sensors | Vector observations + RayPerceptionSensor3D |
| Direct product-type vector cue | Hidden |
| Defect probability | 0.35 |
| Reward V2 | Enabled for push discipline |
| Correct-sort speed bonus | Disabled (0) |
| Defect alignment shaping | Enabled |
| Final demo behavior type | Inference Only |

### Action space

The agent controls three continuous actions:

| Action | Meaning |
|---|---|
| 0 | Sorter carriage movement along Z |
| 1 | Pusher extension / retraction along X |
| 2 | Push activation / strength |

### Reward design

Final reward values:

| Outcome | Reward |
|---|---:|
| Good product accepted | +1.0 |
| Defect product rejected | +1.5 |
| Good product wrongly rejected | -1.5 |
| Defect product missed | -2.0 |
| Time penalty per decision | -0.001 |

Additional shaping and push discipline:

| Component | Value |
|---|---:|
| No-product push penalty | -0.005 |
| Good-product push penalty | -0.01 |
| Push penalty threshold | 0.5 actual mapped push strength |
| Defect alignment progress reward scale | 0.01 |
| Max defect alignment reward per decision | 0.003 |
| Defect alignment zone padding | 0.75 |

Reward standard deviation is not expected to collapse to near zero because good accepted products and rejected defects intentionally receive different reward magnitudes. Outcome counters are the primary success criteria.

### Key training runs

| Run | Purpose | Result |
|---|---|---|
| RoboSort_PPO_Baseline_002 | Initial PPO baseline | Proved basic sorting learnability |
| RoboSort_PPO_LargeNet_Parallel8_001 | Larger network comparison | Useful comparison, not final |
| RoboSort_PPO_SensorDrivenType_Defect30_001 | Strong earlier candidate with hidden product-type cue | Near-perfect old final candidate |
| RoboSort_PPO_PusherContact_OutcomeAlign_Baseline_001 | Corrected pusher contact + asymmetric rewards + alignment shaping | Strong 128-unit policy |
| RoboSort_PPO_PusherContact_PushDiscipline_Defect35_Baseline_001 | Defect35 fine-tune with push discipline | Strong final-family candidate |
| RoboSort_PPO_PushDiscipline_ActuatorPenaltyFix_Baseline_001 | Final actuator-aligned push-penalty fix | Selected final demo model |

### How to run inference demo

Open the Unity project.

Open:

`Assets/_RoboSortRL/Scenes/DemoScene.unity`

Select the demo RoboSortAgent.

Confirm Behavior Parameters:

- Behavior Name: RoboSort
- Behavior Type: Inference Only
- Model: RoboSort_ActuatorPenaltyFix_Defect35_Baseline_950k_Candidate
- Continuous Actions: 3

Press Play.

Expected demo behavior:

- defective products are rejected with clean visible contact,
- good products pass through the accept path,
- rejects occur through visible pusher contact,
- factory visuals remain visual-only.

### Training command example

Activate the ML-Agents environment:

    conda activate mlagents
    cd path/to/RoboSortRL

Start PPO training on CPU:

    mlagents-learn config/robosort_ppo_baseline.yaml \
        --run-id=RoboSort_PPO_Baseline_001 \
        --results-dir results \
        --torch-device cpu

To resume from a previous checkpoint, add:

    --initialize-from=Previous_Run_ID

### Documentation

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

The historical documents are kept intentionally because they show the project evolution: baseline PPO, RayPerception, parallel training, pusher-contact correction, reward tuning, and final model selection. The current selected final model is documented in `docs/final_model_update.md`.

### Notes for evaluation

This project uses reinforcement learning, not imitation learning.

It does not use:

- demonstration recordings
- `.demo` files
- behavioral cloning
- GAIL
- expert action labels

The final policy was trained with PPO using environment rewards, with training progress monitored and iterated through TensorBoard.


