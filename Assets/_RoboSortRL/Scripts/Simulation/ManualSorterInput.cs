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
    /// Controls:
    /// - A / D: move sorter carriage along -Z / +Z
    /// - W / S: retract / extend pusher along X
    /// - Space: push activation strength
    /// - R: reset sorter pose only
    /// </summary>
    [RequireComponent(typeof(SorterController))]
    public class ManualSorterInput : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SorterController sorterController;

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
            }
        }

        private void Update()
        {
            if (!enableManualInput)
            {
                sorterController.SetControl(0f, 0f, -1f);
                return;
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                sorterController.SetControl(0f, 0f, -1f);
                return;
            }

            ReadKeyboardInput(keyboard);
            ApplyInput();

            if (keyboard.rKey.wasPressedThisFrame)
            {
                sorterController.ResetSorter();
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

            // W extends the pusher toward the conveyor/reject direction.
            if (keyboard.wKey.isPressed)
            {
                extensionInput += 1f;
            }

            // S retracts the pusher.
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
    }
}