using RoboSortRL.Simulation;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.Serialization;

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
    /// Current version:
    /// - Vector observations plus RayPerceptionSensor3D.
    /// - 3 continuous actions.
    /// - Event-based rewards through SortingEventRouter.SortingOutcomeRouted.
    /// - Optional Reward V2 support is available but disabled by default.
    /// - Optional product-type observation hiding is available for sensor-driven training.
    /// </summary>
    public class RoboSortAgent : Agent
    {
        private const int ExpectedContinuousActions = 3;
        private const int ProductObservationCount = 9;
        private const int SorterObservationCount = 3;
        private const int TotalVectorObservationCount = 1 + ProductObservationCount + SorterObservationCount;

        private const string DecisionsAtOutcomeStat = "RoboSort/DecisionsAtOutcome";
        private const string SpeedBonusStat = "RoboSort/SpeedBonus";
        private const string NoProductPushPenaltyStat = "RoboSort/NoProductPushPenalty";
        private const string GoodProductPushPenaltyStat = "RoboSort/GoodProductPushPenalty";
        private const string DefectAlignmentRewardStat = "RoboSort/DefectAlignmentReward";

        private const float NoPushDefault = -1f;
        private const float HiddenProductTypeObservationValue = 0.5f;
        private const float ColliderContainmentEpsilon = 0.000001f;

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

        [Header("Observation Hardening")]
        [Tooltip("If disabled, the product-type vector observation is hidden so the policy must rely more on RayPerception tags.")]
        [SerializeField] private bool includeProductTypeObservation = true;

        [Header("Observation Normalization")]
        [SerializeField] private float lateralPositionNormalization = 3f;
        [SerializeField] private float longitudinalPositionNormalization = 6f;
        [SerializeField] private float velocityNormalization = 6f;
        [SerializeField] private float distanceNormalization = 6f;
        [SerializeField] private float conveyorSpeedNormalization = 3f;

        [Header("Outcome Rewards")]
        [Tooltip("Reward for a good product correctly reaching AcceptZone.")]
        [FormerlySerializedAs("correctSortReward")]
        [SerializeField] private float goodAcceptedReward = 1f;

        [Tooltip("Higher reward for correctly rejecting a defective product.")]
        [SerializeField] private float defectRejectedReward = 1.5f;

        [Tooltip("Penalty for wrongly rejecting a good product.")]
        [FormerlySerializedAs("incorrectSortPenalty")]
        [SerializeField] private float goodRejectedPenalty = -1.5f;

        [Tooltip("Higher penalty for missing a defective product. False negatives are costly in quality control.")]
        [SerializeField] private float defectMissedPenalty = -2f;

        [Header("Time Penalty")]
        [SerializeField] private float timePenaltyPerDecision = -0.001f;
        [SerializeField] private bool endEpisodeOnSortingOutcome = true;

        [Header("Reward V2")]
        [SerializeField] private bool useRewardV2 = false;
        [SerializeField] private float correctSortSpeedBonus = 0.1f;
        [SerializeField] private int targetDecisionsForMaxSpeedBonus = 300;
        [SerializeField] private float noProductPushPenalty = -0.005f;
        [SerializeField] private float goodProductPushPenalty = -0.005f;
        [SerializeField] private float pushPenaltyThreshold = 0.5f;
        [SerializeField] private float extensionPenaltyThreshold = 0.2f;

        [Header("Defect Alignment Shaping")]
        [Tooltip("Small discovery reward for improving Z alignment with a defective product near the sorting zone.")]
        [SerializeField] private bool useDefectAlignmentShaping = true;

        [Tooltip("Reward multiplier for reducing Z alignment error with a defective product.")]
        [SerializeField] private float defectAlignmentProgressRewardScale = 0.01f;

        [Tooltip("Maximum shaping reward allowed per decision.")]
        [SerializeField] private float maxDefectAlignmentRewardPerDecision = 0.003f;

        [Tooltip("Allows shaping slightly before/after the sorting zone so the agent can prepare the pusher.")]
        [SerializeField] private float defectAlignmentZonePadding = 0.75f;

        [SerializeField] private float previousDefectAlignmentError = -1f;

        [Header("Debug")]
        [SerializeField] private bool hasOutcomeThisEpisode;
        [SerializeField] private SortingOutcome lastOutcome;
        [SerializeField] private float lastOutcomeReward;
        [SerializeField] private int decisionsThisEpisode;

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
            decisionsThisEpisode = 0;
            previousDefectAlignmentError = -1f;

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
            float pushStrengthInput = continuousActions.Length > 2 ? continuousActions[2] : NoPushDefault;

            decisionsThisEpisode++;

            if (sorterController != null)
            {
                sorterController.SetControl(carriageInput, extensionInput, pushStrengthInput);
            }

            if (!Mathf.Approximately(timePenaltyPerDecision, 0f))
            {
                AddReward(timePenaltyPerDecision);
            }

            ApplyPushDisciplinePenalty(pushStrengthInput);
            ApplyDefectAlignmentShaping();
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
            float pushStrengthInput = NoPushDefault;

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
            sensor.AddObservation(GetProductTypeObservation(product));
            sensor.AddObservation(IsInsideSortingZone(product.transform.position) ? 1f : 0f);
            sensor.AddObservation(GetNormalizedDistanceToCollider(product.transform.position, rejectZone));
            sensor.AddObservation(GetNormalizedDistanceToCollider(product.transform.position, acceptZone));
            sensor.AddObservation(mover != null ? SafeClamp01(mover.MoveSpeed / conveyorSpeedNormalization) : 0f);
        }

        private float GetProductTypeObservation(Product product)
        {
            if (product == null)
            {
                return 0f;
            }

            if (!includeProductTypeObservation)
            {
                return HiddenProductTypeObservationValue;
            }

            return product.IsDefective ? 1f : 0f;
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
            sensor.AddObservation(SafeClamp01(sorterController.ExtensionAmount));
            sensor.AddObservation(SafeClamp01(sorterController.PushStrength));
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

            Academy.Instance.StatsRecorder.Add(
                DecisionsAtOutcomeStat,
                decisionsThisEpisode,
                StatAggregationMethod.Average
            );

            if (endEpisodeOnSortingOutcome)
            {
                EndEpisode();
            }
        }

        // Asymmetric reward: defect-related outcomes are weighted more heavily
        // because false negatives have higher real-world cost in quality control.
        // Symmetric +1/-1 rewards were tested first; this variant is for reducing
        // DefectMissed behavior after the corrected pusher-contact change.
        private float GetRewardForOutcome(SortingOutcome outcome)
        {
            switch (outcome)
            {
                case SortingOutcome.GoodAccepted:
                    return goodAcceptedReward + GetCorrectSortSpeedBonus();

                case SortingOutcome.DefectRejected:
                    return defectRejectedReward + GetCorrectSortSpeedBonus();

                case SortingOutcome.GoodRejected:
                    return goodRejectedPenalty;

                case SortingOutcome.DefectMissed:
                    return defectMissedPenalty;

                default:
                    return 0f;
            }
        }

        private void ApplyDefectAlignmentShaping()
        {
            if (!useDefectAlignmentShaping || sorterController == null || productSpawner == null)
            {
                previousDefectAlignmentError = -1f;
                return;
            }

            Product currentProduct = productSpawner.CurrentProduct;
            bool hasDefectiveProduct = currentProduct != null
                                       && currentProduct.gameObject.activeInHierarchy
                                       && !currentProduct.HasBeenProcessed
                                       && currentProduct.IsDefective;

            if (!hasDefectiveProduct || !IsInsideOrNearSortingZone(currentProduct.transform.position, defectAlignmentZonePadding))
            {
                previousDefectAlignmentError = -1f;
                return;
            }

            Vector3 sorterLocalProductPosition = sorterController.transform.InverseTransformPoint(
                currentProduct.transform.position
            );

            float currentAlignmentError = Mathf.Abs(
                sorterLocalProductPosition.z - sorterController.CurrentCarriageZOffset
            );

            if (previousDefectAlignmentError >= 0f)
            {
                float improvement = previousDefectAlignmentError - currentAlignmentError;

                if (improvement > 0f)
                {
                    float reward = Mathf.Min(
                        improvement * defectAlignmentProgressRewardScale,
                        maxDefectAlignmentRewardPerDecision
                    );

                    AddReward(reward);
                    Academy.Instance.StatsRecorder.Add(
                        DefectAlignmentRewardStat,
                        reward,
                        StatAggregationMethod.Sum
                    );
                }
            }

            previousDefectAlignmentError = currentAlignmentError;
        }

        private bool IsInsideOrNearSortingZone(Vector3 worldPosition, float padding)
        {
            if (sortingZone == null)
            {
                return false;
            }

            Vector3 closestPoint = sortingZone.ClosestPoint(worldPosition);
            float distance = Vector3.Distance(worldPosition, closestPoint);

            return distance <= Mathf.Max(0f, padding);
        }

        private float GetCorrectSortSpeedBonus()
        {
            if (!useRewardV2 || correctSortSpeedBonus <= 0f || targetDecisionsForMaxSpeedBonus <= 0)
            {
                return 0f;
            }

            float normalizedDelay = Mathf.Clamp01((float)decisionsThisEpisode / targetDecisionsForMaxSpeedBonus);
            float speedFactor = 1f - normalizedDelay;
            float bonus = correctSortSpeedBonus * speedFactor;

            // Records average speed bonus for correct sorting outcomes only.
            Academy.Instance.StatsRecorder.Add(SpeedBonusStat, bonus, StatAggregationMethod.Average);

            return bonus;
        }

        private void ApplyPushDisciplinePenalty(float pushStrengthInput)
        {
            if (!useRewardV2)
            {
                return;
            }

            if (sorterController == null || sorterController.PushStrength <= pushPenaltyThreshold)
            {
                return;
            }

            if (sorterController.ExtensionAmount < extensionPenaltyThreshold)
            {
                return;
            }

            Product currentProduct = productSpawner != null ? productSpawner.CurrentProduct : null;
            bool hasProduct = currentProduct != null
                              && currentProduct.gameObject.activeInHierarchy
                              && !currentProduct.HasBeenProcessed;

            if (!hasProduct || !IsInsideSortingZone(currentProduct.transform.position))
            {
                AddReward(noProductPushPenalty);
                Academy.Instance.StatsRecorder.Add(NoProductPushPenaltyStat, 1f, StatAggregationMethod.Sum);
                return;
            }

            if (!currentProduct.IsDefective)
            {
                AddReward(goodProductPushPenalty);
                Academy.Instance.StatsRecorder.Add(GoodProductPushPenaltyStat, 1f, StatAggregationMethod.Sum);
            }
        }

        private bool IsInsideSortingZone(Vector3 worldPosition)
        {
            if (sortingZone == null)
            {
                return false;
            }

            Vector3 closestPoint = sortingZone.ClosestPoint(worldPosition);
            return (closestPoint - worldPosition).sqrMagnitude <= ColliderContainmentEpsilon;
        }

        private float GetNormalizedDistanceToCollider(Vector3 worldPosition, Collider targetCollider)
        {
            if (targetCollider == null || distanceNormalization <= 0f)
            {
                return 0f;
            }

            Vector3 closestPoint = targetCollider.ClosestPoint(worldPosition);
            float distance = Vector3.Distance(worldPosition, closestPoint);

            return SafeClamp01(distance / distanceNormalization);
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

        private static float SafeClamp01(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return Mathf.Clamp01(value);
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