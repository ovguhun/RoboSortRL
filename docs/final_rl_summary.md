> **Final update:** The current selected final demo model is documented in docs/final_model_update.md and README.md.
> Older Defect30 references in this file are retained as historical training notes, not the final selected demo model.
# Final RL Summary

## RoboSortRL Pro — Final Training Result Summary

This document summarizes the strongest reinforcement learning results for RoboSortRL Pro.

---

## Final RL Core

The current final RL candidate uses:

- PPO with Unity ML-Agents
- `TrainingScene_Parallel8`
- 8 prefab-based parallel training cells
- 13 vector observations
- RayPerceptionSensor3D
- 3 continuous actions
- randomized product spawn X
- randomized conveyor speed
- defect probability `0.30`
- hidden direct product-type vector cue
- Reward V2 disabled

This setup is preferred because it makes the task more sensor-dependent than the earlier direct product-type vector setup and uses a more realistic defect distribution than the balanced `0.50` training setup.

---

## Key Results

| Run | Main Purpose | Result |
|---|---|---|
| `RoboSort_PPO_Parallel8_Smoke_001` | Validate 8-cell parallel training | Reached stable high reward quickly |
| `RoboSort_PPO_LargeNet_Stats_001` | Verify custom TensorBoard outcome stats | Late-stage accuracy reached `1.0000` |
| `RoboSort_PPO_HardenedSpawnSpeed_001` | Test spawn-X and conveyor-speed randomization | Task remained solvable with near-perfect accuracy |
| `RoboSort_PPO_SensorDrivenType_001` | Hide direct product-type vector cue with balanced `0.50` defect probability | Reached near-perfect accuracy after slower and noisier learning |
| `RoboSort_PPO_SensorDrivenType_Defect30_001` | Test final sensor-driven setup with `0.30` defect probability | Reached `1.0000` late-stage accuracy with no missed defects or false rejects at the final checkpoint |

---

## Selected Final Model

**Final realism-oriented candidate:**

`Assets/_RoboSortRL/Training/Models/RoboSort_SensorDrivenType_Defect30_Final.onnx`

**Backup balanced candidate:**

`Assets/_RoboSortRL/Training/Models/RoboSort_SensorDrivenType_Final.onnx`

The `Defect30` model is preferred for the final demo if inference remains visually stable, because it better represents a realistic industrial class distribution while still preserving strong PPO performance.

The balanced `0.50` model is retained as a fallback because it has cleaner convergence and is useful as a stability reference.

---

## Final Candidate Run

**Run:** `RoboSort_PPO_SensorDrivenType_Defect30_001`

**Max Steps:** `1,000,000`

**Final interpretation:**

- The direct product-type vector cue was hidden.
- The product-type observation slot emitted neutral `0.5`.
- Observation size stayed fixed at `13`.
- Defect probability was reduced from `0.50` to `0.30`.
- PPO still learned to sort products correctly.
- Final checkpoint accuracy reached `1.0000`.
- Final checkpoint outcome stats showed:
  - `GoodAccepted = 119`
  - `DefectRejected = 44`
  - `GoodRejected = 0`
  - `DefectMissed = 0`
  - `TotalOutcomes = 163`
- RayPerception became more important because the policy no longer received a direct Good/Defect vector label.

This is the preferred final candidate because it combines sensor-supported reinforcement learning with a more realistic defect distribution.

---

## Reward Interpretation

The converged reward around `0.69–0.71` in the `0.30` run is not a failure.

It is explained by the reward formula:

- Correct outcome: about `+1.0`
- Time penalty: `-0.001` per decision
- Typical decisions at outcome: about `300`

So expected reward is approximately:

`1.0 - (0.001 × 300) ≈ 0.70`

Therefore, accuracy and outcome counters are more important than raw reward for evaluating final task success.

---

## Why This Is the Final RL Candidate

This setup is strong because:

- It uses randomized environment conditions.
- It uses parallel training.
- It uses RayPerceptionSensor3D in a more meaningful way.
- It hides the direct product-type vector label.
- It uses a moderate `0.30` defect probability instead of an easier balanced-only setup.
- It keeps the simulation stable and reproducible.
- It avoids fragile overcomplicated robotics physics.
- It remains explainable for a final demo and GitHub portfolio.

---

## Optional Future Work

Reward V2 exists in code but is disabled by default.

Future experiments can enable Reward V2 to test whether:

- decisions-at-outcome decreases
- speed improves
- unnecessary pushing decreases
- sorting accuracy remains stable

A larger future extension could add a third product class and a ReworkZone, but this is intentionally left out of the stable final RL candidate.

A future curriculum-learning extension could train progressively through:

- `0.50` defect probability
- `0.30` defect probability
- `0.20` or `0.10` defect probability

This should not be added to the current final candidate without separate validation.

