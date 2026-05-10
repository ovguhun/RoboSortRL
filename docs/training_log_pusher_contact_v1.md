# RoboSortRL Pro — Pusher Contact v1 Training Log

## Purpose

Retrain PPO after correcting the scripted pusher contact volume so the product is no longer pushed before visible contact.

## Git checkpoint

- Branch: feature/demo-pusher-contact-visual
- Commit: 038d9b1
- Tag: pusher-contact-volume-v1
- Backup tag before change: pre-pusher-contact-tuning

## Mechanical change

Corrected in Assets/_RoboSortRL/Prefabs/SortingCell.prefab:

- pushVolumeLocalOffset.x: 0.55 -> 0.70
- pushVolumeHalfExtents.x: 1.35 -> 0.10

Corrected world-space push slab:

- Pusher front face: P + 0.25
- Detection near edge: P + 0.25
- Detection far edge: P + 0.45

## Training scene

- Scene: TrainingScene_Parallel8
- Uses SortingCell prefab instances
- No direct pushVolume override found in TrainingScene_Parallel8

## Planned runs

| Run ID | Config | Purpose | Status |
|---|---|---|---|
| RoboSort_PPO_PusherContact_Baseline_001 | config/robosort_ppo_baseline.yaml | corrected-contact baseline network | Planned |
| RoboSort_PPO_PusherContact_LargeNet_001 | config/robosort_ppo_large.yaml | corrected-contact large network candidate | Planned |
| RoboSort_PPO_PusherContact_Ray_001 | config/robosort_ppo_ray.yaml | corrected-contact ray/sensor comparison | Planned |

## Notes

- Do not change defect probability yet.
- Do not enable Reward V2 yet.
- Do not change observations, action dimensions, rewards, tags, layers, or colliders during this suite.
- Evaluate with TensorBoard and final outcome counters, not reward alone.
