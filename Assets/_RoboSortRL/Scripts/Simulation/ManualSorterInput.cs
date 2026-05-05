using UnityEngine;
using UnityEngine.InputSystem;

namespace RoboSortRL.Simulation
{
    /// <summary>
    /// Keyboard-only manual control bridge for validating the sorter before ML-Agents integration.
    ///
    /// This script is for debugging and environment validation only.
    /// The final project remains PPO reinforcement learning.
    ///
    /// Important:
    /// - During manual validation, Enable Manual Input should be checked.
    /// - During PPO training, disable this component or uncheck Enable Manual Input.
    /// - When manual input is disabled, this script does not write zero controls.
    ///   This prevents it from fighting RoboSortAgent actions later.
    ///
    /// Controls:
    /// - A / D: move sorter carriage along -Z / +Z.
    /// - W / S: extend / retract pusher along X.
    /// - Space: push activation strength.
    /// - R: begin a fresh episode through EpisodeManager.
    /// </summary>
    [RequireComponent(typeof(SorterController))]
    public class ManualSorterInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SorterController sorterController;
        [SerializeField] private EpisodeManager episodeManager;

        [Header("Input Settings")]
        [SerializeField] private bool enableManualInput = true;

        [Header("Runtime Debug")]
        [SerializeField, Range(-1f, 1f)] private float carriageInput;
        [SerializeField, Range(-1f, 1f)] private float extensionInput;
        [SerializeField, Range(-1f, 1f)] private float pushStrengthInput = -1f;

        private void Awake()
        {
            if (sorterController == null)
            {
                sorterController = GetComponent<SorterController>();
            }

            if (sorterController == null)
            {
                Debug.LogError($"{nameof(ManualSorterInput)} on {name}: SorterController is missing.", this);
                enabled = false;
                return;
            }

            if (episodeManager == null)
            {
                episodeManager = FindFirstObjectByType<EpisodeManager>();
            }
        }

        private void Update()
        {
            if (!enableManualInput)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            ReadKeyboardInput(keyboard);
            ApplyInput();

            if (keyboard.rKey.wasPressedThisFrame)
            {
                ResetManualEpisode();
            }
        }

        private void ReadKeyboardInput(Keyboard keyboard)
        {
            carriageInput = 0f;
            extensionInput = 0f;
            pushStrengthInput = -1f;

            if (keyboard.aKey.isPressed)
            {
                carriageInput -= 1f;
            }

            if (keyboard.dKey.isPressed)
            {
                carriageInput += 1f;
            }

            if (keyboard.wKey.isPressed)
            {
                extensionInput += 1f;
            }

            if (keyboard.sKey.isPressed)
            {
                extensionInput -= 1f;
            }

            if (keyboard.spaceKey.isPressed)
            {
                pushStrengthInput = 1f;
            }
        }

        private void ApplyInput()
        {
            sorterController.SetControl(
                carriageInput,
                extensionInput,
                pushStrengthInput
            );
        }

        private void ResetManualEpisode()
        {
            if (episodeManager != null)
            {
                episodeManager.BeginEpisode();
                return;
            }

            Debug.LogWarning(
                $"{nameof(ManualSorterInput)} on {name}: EpisodeManager is missing. Falling back to sorter-only reset.",
                this
            );

            sorterController.ResetSorter();
        }
    }
}