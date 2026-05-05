using UnityEngine;

namespace RoboSortRL.Simulation
{
    /// <summary>
    /// Controls the simple kinematic sorter/pusher proxy.
    ///
    /// This script does not use ML-Agents yet.
    /// It exposes a clean control API that manual input and the future RoboSortAgent can both call.
    ///
    /// Coordinate convention:
    /// - X = conveyor width / reject direction.
    /// - Z = conveyor travel direction.
    /// - Pusher extends in +X.
    /// - Sorter carriage moves along Z to align with products inside the sorting zone.
    ///
    /// Physics rule:
    /// - PusherProxy has a kinematic Rigidbody.
    /// - PusherProxy is moved with Rigidbody.MovePosition in FixedUpdate.
    /// - Product pushing will be scripted later; we do not rely on kinematic-vs-kinematic collision impulses.
    /// </summary>
    public class SorterController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform sorterBaseProxy;
        [SerializeField] private Transform pusherProxy;

        [Header("Carriage Movement Along Z")]
        [SerializeField] private float minCarriageZOffset = -1.0f;
        [SerializeField] private float maxCarriageZOffset = 1.0f;
        [SerializeField] private float carriageMoveSpeed = 2.0f;

        [Header("Pusher Extension Along X")]
        [SerializeField] private float retractedLocalX = -1.37f;
        [SerializeField] private float extendedLocalX = 0.85f;
        [SerializeField] private float extensionMoveSpeed = 3.0f;

        [Header("Debug Control Inputs")]
        [SerializeField, Range(-1f, 1f)] private float requestedCarriageInput;
        [SerializeField, Range(-1f, 1f)] private float requestedExtensionInput;
        [SerializeField, Range(-1f, 1f)] private float requestedPushStrengthInput;

        [Header("Runtime State")]
        [SerializeField] private float currentCarriageZOffset;
        [SerializeField, Range(0f, 1f)] private float currentExtensionAmount;
        [SerializeField, Range(0f, 1f)] private float currentPushStrength;

        private Rigidbody pusherRigidbody;

        private Vector3 initialSorterBaseLocalPosition;
        private Vector3 initialPusherLocalPosition;

        public float NormalizedCarriagePosition { get; private set; }
        public float ExtensionAmount => currentExtensionAmount;
        public float PushStrength => currentPushStrength;

        private void Awake()
        {
            if (sorterBaseProxy == null)
            {
                Debug.LogError($"{nameof(SorterController)} on {name}: Sorter Base Proxy reference is missing.", this);
                enabled = false;
                return;
            }

            if (pusherProxy == null)
            {
                Debug.LogError($"{nameof(SorterController)} on {name}: Pusher Proxy reference is missing.", this);
                enabled = false;
                return;
            }

            pusherRigidbody = pusherProxy.GetComponent<Rigidbody>();

            if (pusherRigidbody == null)
            {
                Debug.LogError(
                    $"{nameof(SorterController)} on {name}: PusherProxy must have a kinematic Rigidbody.",
                    this
                );
                enabled = false;
                return;
            }

            if (!pusherRigidbody.isKinematic)
            {
                Debug.LogError(
                    $"{nameof(SorterController)} on {name}: PusherProxy Rigidbody must be kinematic.",
                    this
                );
                enabled = false;
                return;
            }

            initialSorterBaseLocalPosition = sorterBaseProxy.localPosition;
            initialPusherLocalPosition = pusherProxy.localPosition;

            ResetSorter();
        }

        private void FixedUpdate()
        {
            UpdateCarriageState();
            UpdateExtensionState();
            ApplySorterBasePosition();
            ApplyPusherPosition();
        }

        /// <summary>
        /// Sets sorter control inputs.
        ///
        /// Expected input range for all three values is [-1, 1].
        /// carriageInput: negative/positive moves carriage along -Z/+Z.
        /// extensionInput: negative retracts pusher, positive extends pusher.
        /// pushStrengthInput: mapped from [-1, 1] into [0, 1] for future scripted push strength.
        /// </summary>
        public void SetControl(float carriageInput, float extensionInput, float pushStrengthInput)
        {
            requestedCarriageInput = Mathf.Clamp(carriageInput, -1f, 1f);
            requestedExtensionInput = Mathf.Clamp(extensionInput, -1f, 1f);
            requestedPushStrengthInput = Mathf.Clamp(pushStrengthInput, -1f, 1f);
        }

        /// <summary>
        /// Resets carriage, pusher extension, and push strength to the starting state.
        /// Future EpisodeManager and RoboSortAgent reset logic should call this.
        /// </summary>
        public void ResetSorter()
        {
            requestedCarriageInput = 0f;
            requestedExtensionInput = 0f;
            requestedPushStrengthInput = -1f;

            currentCarriageZOffset = 0f;
            currentExtensionAmount = 0f;
            currentPushStrength = 0f;

            ApplySorterBasePosition();

            Vector3 pusherWorldPosition = GetPusherWorldTargetPosition();
            pusherRigidbody.position = pusherWorldPosition;
            pusherRigidbody.rotation = transform.rotation;

            NormalizedCarriagePosition = Mathf.InverseLerp(
                minCarriageZOffset,
                maxCarriageZOffset,
                currentCarriageZOffset
            );
        }

        private void UpdateCarriageState()
        {
            currentCarriageZOffset = Mathf.Clamp(
                currentCarriageZOffset + requestedCarriageInput * carriageMoveSpeed * Time.fixedDeltaTime,
                minCarriageZOffset,
                maxCarriageZOffset
            );

            NormalizedCarriagePosition = Mathf.InverseLerp(
                minCarriageZOffset,
                maxCarriageZOffset,
                currentCarriageZOffset
            );
        }

        private void UpdateExtensionState()
        {
            float extensionRange = Mathf.Abs(extendedLocalX - retractedLocalX);

            if (extensionRange <= 0.0001f)
            {
                currentExtensionAmount = 0f;
                currentPushStrength = 0f;
                return;
            }

            float extensionDelta = requestedExtensionInput * extensionMoveSpeed * Time.fixedDeltaTime;
            float normalizedDelta = extensionDelta / extensionRange;

            currentExtensionAmount = Mathf.Clamp01(currentExtensionAmount + normalizedDelta);

            // This is intentionally one-sided activation strength.
            // ML-Agents continuous actions are [-1, 1], but push strength is a [0, 1] actuator.
            currentPushStrength = Mathf.Clamp01((requestedPushStrengthInput + 1f) * 0.5f);
        }

        private void ApplySorterBasePosition()
        {
            Vector3 baseLocalPosition = initialSorterBaseLocalPosition;
            baseLocalPosition.z += currentCarriageZOffset;
            sorterBaseProxy.localPosition = baseLocalPosition;
        }

        private void ApplyPusherPosition()
        {
            Vector3 pusherWorldPosition = GetPusherWorldTargetPosition();

            pusherRigidbody.MovePosition(pusherWorldPosition);
            pusherRigidbody.MoveRotation(transform.rotation);
        }

        private Vector3 GetPusherWorldTargetPosition()
        {
            Vector3 pusherLocalPosition = initialPusherLocalPosition;

            pusherLocalPosition.x = Mathf.Lerp(
                retractedLocalX,
                extendedLocalX,
                currentExtensionAmount
            );

            pusherLocalPosition.z += currentCarriageZOffset;

            return transform.TransformPoint(pusherLocalPosition);
        }
    }
}