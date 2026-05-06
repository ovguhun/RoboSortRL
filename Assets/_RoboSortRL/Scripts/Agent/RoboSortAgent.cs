using RoboSortRL.Simulation;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace RoboSortRL.Agents
{
    /// <summary>
    /// ML-Agents interface for the RoboSortRL Pro sorting cell.
    ///
    /// This agent does not move products or transforms directly.
    /// It controls the sorter only through SorterController.SetControl().
    ///
    /// First version:
    /// - Vector observations only.
    /// - 3 continuous actions.
    /// - Event-based rewards through SortingEventRouter.SortingOutcomeRouted.
    /// - RayPerceptionSensor3D will be added later after this path is stable.
    /// </summary>
    public class RoboSortAgent : Agent
    {
        private const int ExpectedContinuousActions = 3;
        private const int ProductObservationCount = 9;
        private const int SorterObservationCount = 3;
        private const int TotalVectorObservationCount = 1 + ProductObservationCount + SorterObservationCount;

        [Header("Scene References")]
        [SerializeField] private EpisodeManager episodeManager;
        [SerializeField] private ProductSpawner productSpawner;
        [SerializeField] private SorterController sorterController;
        [SerializeField] private SortingEventRouter sortingEventRouter;

        [Header("Observation References")]
        [SerializeField] private Transform observationRoot;
        [SerializeField] private Collider sortingZone;
        [SerializeField] private Collider rejectZone;
        [SerializeField] private Collider acceptZone;

        [Header("Observation Normalization")]
        [SerializeField] private float lateralPositionNormalization = 3f;
        [SerializeField] private float longitudinalPositionNormalization = 6f;
        [SerializeField] private float velocityNormalization = 6f;
        [SerializeField] private float distanceNormalization = 6f;
        [SerializeField] private float conveyorSpeedNormalization = 3f;

        [Header("Rewards")]
        [SerializeField] private float correctSortReward = 1f;
        [SerializeField] private float incorrectSortPenalty = -1f;
        [SerializeField] private float timePenaltyPerDecision = -0.001f;
        [SerializeField] private bool endEpisodeOnSortingOutcome = true;

        [Header("Debug")]
        [SerializeField] private bool hasOutcomeThisEpisode;
        [SerializeField] private SortingOutcome lastOutcome;
        [SerializeField] private float lastOutcomeReward;

        private bool isSubscribedToRouter;

        public override void Initialize()
        {
            if (observationRoot == null && sorterController != null)
            {
                observationRoot = sorterController.transform;
            }

            ValidateReferences();
            ValidateBehaviorParameters();
            SubscribeToSortingEvents();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToSortingEvents();
        }

        protected override void OnDisable()
        {
            UnsubscribeFromSortingEvents();
            base.OnDisable();
        }

        private void OnDestroy()
        {
            UnsubscribeFromSortingEvents();
        }

        public override void OnEpisodeBegin()
        {
            hasOutcomeThisEpisode = false;
            lastOutcomeReward = 0f;

            if (episodeManager == null)
            {
                Debug.LogError($"{nameof(RoboSortAgent)} on {name}: EpisodeManager reference is missing.", this);
                return;
            }

            episodeManager.BeginEpisode();
        }

        public override void CollectObservations(VectorSensor sensor)
        {
            Product currentProduct = productSpawner != null ? productSpawner.CurrentProduct : null;
            bool hasProduct = currentProduct != null
                              && currentProduct.gameObject.activeInHierarchy
                              && !currentProduct.HasBeenProcessed;

            // 1 value.
            sensor.AddObservation(hasProduct ? 1f : 0f);

            if (hasProduct)
            {
                AddProductObservations(sensor, currentProduct);
            }
            else
            {
                AddEmptyProductObservations(sensor);
            }

            AddSorterObservations(sensor);
        }

        public override void OnActionReceived(ActionBuffers actions)
        {
            ActionSegment<float> continuousActions = actions.ContinuousActions;

            float carriageInput = continuousActions.Length > 0 ? continuousActions[0] : 0f;
            float extensionInput = continuousActions.Length > 1 ? continuousActions[1] : 0f;
            float pushStrengthInput = continuousActions.Length > 2 ? continuousActions[2] : -1f;

            if (sorterController != null)
            {
                sorterController.SetControl(carriageInput, extensionInput, pushStrengthInput);
            }

            if (!Mathf.Approximately(timePenaltyPerDecision, 0f))
            {
                AddReward(timePenaltyPerDecision);
            }
        }

        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;

            if (continuousActions.Length < ExpectedContinuousActions)
            {
                return;
            }

            float carriageInput = 0f;
            float extensionInput = 0f;
            float pushStrengthInput = -1f;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed)
                {
                    carriageInput -= 1f;
                }

                if (keyboard.dKey.isPressed)
                {
                    carriageInput += 1f;
                }

                if (keyboard.sKey.isPressed)
                {
                    extensionInput -= 1f;
                }

                if (keyboard.wKey.isPressed)
                {
                    extensionInput += 1f;
                }

                if (keyboard.spaceKey.isPressed)
                {
                    pushStrengthInput = 1f;
                }
            }
#else
            if (Input.GetKey(KeyCode.A))
            {
                carriageInput -= 1f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                carriageInput += 1f;
            }

            if (Input.GetKey(KeyCode.S))
            {
                extensionInput -= 1f;
            }

            if (Input.GetKey(KeyCode.W))
            {
                extensionInput += 1f;
            }

            if (Input.GetKey(KeyCode.Space))
            {
                pushStrengthInput = 1f;
            }
#endif

            continuousActions[0] = Mathf.Clamp(carriageInput, -1f, 1f);
            continuousActions[1] = Mathf.Clamp(extensionInput, -1f, 1f);
            continuousActions[2] = Mathf.Clamp(pushStrengthInput, -1f, 1f);
        }

        private void AddProductObservations(VectorSensor sensor, Product product)
        {
            Vector3 localProductPosition = ToObservationLocalPosition(product.transform.position);
            ConveyorMover mover = product.GetComponent<ConveyorMover>();
            Vector3 localVelocity = mover != null
                ? ToObservationLocalDirection(mover.LastFrameVelocity)
                : Vector3.zero;

            // Product observations: ProductObservationCount = 9.
            sensor.AddObservation(ClampUnit(localProductPosition.x / lateralPositionNormalization));
            sensor.AddObservation(ClampUnit(localProductPosition.z / longitudinalPositionNormalization));
            sensor.AddObservation(ClampUnit(localVelocity.x / velocityNormalization));
            sensor.AddObservation(ClampUnit(localVelocity.z / velocityNormalization));
            sensor.AddObservation(product.IsDefective ? 1f : 0f);
            sensor.AddObservation(IsInsideSortingZone(product.transform.position) ? 1f : 0f);
            sensor.AddObservation(GetNormalizedDistanceToCollider(product.transform.position, rejectZone));
            sensor.AddObservation(GetNormalizedDistanceToCollider(product.transform.position, acceptZone));
            sensor.AddObservation(mover != null ? Mathf.Clamp01(mover.MoveSpeed / conveyorSpeedNormalization) : 0f);
        }

        private static void AddEmptyProductObservations(VectorSensor sensor)
        {
            for (int i = 0; i < ProductObservationCount; i++)
            {
                sensor.AddObservation(0f);
            }
        }

        private void AddSorterObservations(VectorSensor sensor)
        {
            if (sorterController == null)
            {
                for (int i = 0; i < SorterObservationCount; i++)
                {
                    sensor.AddObservation(0f);
                }

                return;
            }

            float centeredCarriagePosition = sorterController.NormalizedCarriagePosition * 2f - 1f;

            // Sorter observations: SorterObservationCount = 3.
            sensor.AddObservation(ClampUnit(centeredCarriagePosition));
            sensor.AddObservation(Mathf.Clamp01(sorterController.ExtensionAmount));
            sensor.AddObservation(Mathf.Clamp01(sorterController.PushStrength));
        }

        private void HandleSortingOutcome(SortingOutcome outcome, Product product, AcceptRejectZone zone)
        {
            if (hasOutcomeThisEpisode)
            {
                return;
            }

            hasOutcomeThisEpisode = true;
            lastOutcome = outcome;

            float reward = GetRewardForOutcome(outcome);
            lastOutcomeReward = reward;
            AddReward(reward);

            if (endEpisodeOnSortingOutcome)
            {
                EndEpisode();
            }
        }

        private float GetRewardForOutcome(SortingOutcome outcome)
        {
            switch (outcome)
            {
                case SortingOutcome.GoodAccepted:
                case SortingOutcome.DefectRejected:
                    return correctSortReward;

                case SortingOutcome.GoodRejected:
                case SortingOutcome.DefectMissed:
                    return incorrectSortPenalty;

                default:
                    return 0f;
            }
        }

        private bool IsInsideSortingZone(Vector3 worldPosition)
        {
            return sortingZone != null && sortingZone.bounds.Contains(worldPosition);
        }

        private float GetNormalizedDistanceToCollider(Vector3 worldPosition, Collider targetCollider)
        {
            if (targetCollider == null)
            {
                return 0f;
            }

            Vector3 closestPoint = targetCollider.ClosestPoint(worldPosition);
            float distance = Vector3.Distance(worldPosition, closestPoint);

            return Mathf.Clamp01(distance / distanceNormalization);
        }

        private Vector3 ToObservationLocalPosition(Vector3 worldPosition)
        {
            return observationRoot != null
                ? observationRoot.InverseTransformPoint(worldPosition)
                : worldPosition;
        }

        private Vector3 ToObservationLocalDirection(Vector3 worldDirection)
        {
            return observationRoot != null
                ? observationRoot.InverseTransformDirection(worldDirection)
                : worldDirection;
        }

        private void SubscribeToSortingEvents()
        {
            if (sortingEventRouter == null || isSubscribedToRouter)
            {
                return;
            }

            sortingEventRouter.SortingOutcomeRouted -= HandleSortingOutcome;
            sortingEventRouter.SortingOutcomeRouted += HandleSortingOutcome;
            isSubscribedToRouter = true;
        }

        private void UnsubscribeFromSortingEvents()
        {
            if (sortingEventRouter == null || !isSubscribedToRouter)
            {
                return;
            }

            sortingEventRouter.SortingOutcomeRouted -= HandleSortingOutcome;
            isSubscribedToRouter = false;
        }

        private void ValidateReferences()
        {
            if (episodeManager == null)
            {
                Debug.LogError($"{nameof(RoboSortAgent)} on {name}: EpisodeManager reference is missing.", this);
            }

            if (productSpawner == null)
            {
                Debug.LogError($"{nameof(RoboSortAgent)} on {name}: ProductSpawner reference is missing.", this);
            }

            if (sorterController == null)
            {
                Debug.LogError($"{nameof(RoboSortAgent)} on {name}: SorterController reference is missing.", this);
            }

            if (sortingEventRouter == null)
            {
                Debug.LogError($"{nameof(RoboSortAgent)} on {name}: SortingEventRouter reference is missing.", this);
            }

            if (sortingZone == null)
            {
                Debug.LogWarning($"{nameof(RoboSortAgent)} on {name}: SortingZone reference is missing. Sorting-zone observation will be 0.", this);
            }

            if (rejectZone == null)
            {
                Debug.LogWarning($"{nameof(RoboSortAgent)} on {name}: RejectZone reference is missing. Reject distance observation will be 0.", this);
            }

            if (acceptZone == null)
            {
                Debug.LogWarning($"{nameof(RoboSortAgent)} on {name}: AcceptZone reference is missing. Accept distance observation will be 0.", this);
            }
        }

        private void ValidateBehaviorParameters()
        {
            BehaviorParameters behaviorParameters = GetComponent<BehaviorParameters>();

            if (behaviorParameters == null)
            {
                return;
            }

            int configuredObservationSize = behaviorParameters.BrainParameters.VectorObservationSize;

            if (configuredObservationSize != TotalVectorObservationCount)
            {
                Debug.LogError(
                    $"{nameof(RoboSortAgent)} on {name}: Behavior Parameters Vector Observation Size = {configuredObservationSize}, " +
                    $"but this script emits {TotalVectorObservationCount}.",
                    this
                );
            }

            int configuredContinuousActions = behaviorParameters.BrainParameters.ActionSpec.NumContinuousActions;

            if (configuredContinuousActions != ExpectedContinuousActions)
            {
                Debug.LogError(
                    $"{nameof(RoboSortAgent)} on {name}: Behavior Parameters Continuous Actions = {configuredContinuousActions}, " +
                    $"but this script expects {ExpectedContinuousActions}.",
                    this
                );
            }
        }

        private static float ClampUnit(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return Mathf.Clamp(value, -1f, 1f);
        }
    }
}