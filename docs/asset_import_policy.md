# Asset Import Policy — RoboSortRL Pro

## Purpose

RoboSortRL Pro uses third-party visual assets only as a visual layer. Training logic must remain independent from imported assets.

## Unity Factory Usage

Unity Factory is used as local visual-only content for industrial presentation.

It must not define:

- Training colliders
- Product physics
- Reward triggers
- Conveyor movement logic
- Agent actions
- RayPerception targets
- Episode reset logic

## Architecture Rule

SimulationRoot is the source of truth for reinforcement learning.

VisualRoot is only a visual skin.

VisualRoot can be deleted and the training task should still work.

## Safety Rules

- Do not train inside Unity Factory demo scenes.
- Do not merge Unity Factory demo scenes into TrainingScene.
- Do not put imported visual assets under SimulationRoot.
- Disable decorative colliders or place visual objects on the VisualOnly layer.
- RayPerceptionSensor3D must ignore VisualOnly.
- Imported asset scripts must not control sorting, spawning, rewards, resets, or agent behavior.
- TrainingScene must remain proxy-based and stable.

## GitHub Policy

Raw Unity Factory Asset Store files are not redistributed in the public GitHub repository.

The local Unity project may contain:

```text
Assets/UnityFactorySceneHDRP/