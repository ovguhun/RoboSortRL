using System;
using UnityEngine;
using Unity.MLAgents;

namespace RoboSortRL.Simulation
{
    public class SortingEventRouter : MonoBehaviour
    {
        /// <summary>
        /// Broadcasts every final sorting outcome from both AcceptZone and RejectZone.
        /// Future reward logic should subscribe here, not directly to AcceptRejectZone.ProductProcessed.
        /// </summary>
        public event Action<SortingOutcome, Product, AcceptRejectZone> SortingOutcomeRouted;

        [Header("Scene References")]
        [SerializeField] private AcceptRejectZone acceptZone;
        [SerializeField] private AcceptRejectZone rejectZone;
        [SerializeField] private EpisodeStats episodeStats;
        [SerializeField] private ProductSpawner productSpawner;

        [Header("Runtime Behavior")]
        [SerializeField] private bool despawnProcessedProducts = true;

        [Header("Training Stats")]
        [Tooltip("Records per-outcome counters to TensorBoard through ML-Agents StatsRecorder.")]
        [SerializeField] private bool recordTrainingStats = true;

        private const string GoodAcceptedStat = "RoboSort/GoodAccepted";
        private const string DefectRejectedStat = "RoboSort/DefectRejected";
        private const string GoodRejectedStat = "RoboSort/GoodRejected";
        private const string DefectMissedStat = "RoboSort/DefectMissed";
        private const string TotalOutcomesStat = "RoboSort/TotalOutcomes";
        private const string AccuracyStat = "RoboSort/Accuracy";

        private void Awake()
        {
            ValidateReferences();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ValidateReferences()
        {
            if (acceptZone == null)
            {
                Debug.LogWarning($"{nameof(SortingEventRouter)}: Accept Zone is not assigned.", this);
            }

            if (rejectZone == null)
            {
                Debug.LogWarning($"{nameof(SortingEventRouter)}: Reject Zone is not assigned.", this);
            }

            if (episodeStats == null)
            {
                Debug.LogWarning($"{nameof(SortingEventRouter)}: EpisodeStats is not assigned.", this);
            }

            if (productSpawner == null)
            {
                Debug.LogWarning($"{nameof(SortingEventRouter)}: ProductSpawner is not assigned.", this);
            }
        }

        private void Subscribe()
        {
            if (acceptZone != null)
            {
                acceptZone.ProductProcessed -= HandleProductProcessed;
                acceptZone.ProductProcessed += HandleProductProcessed;
            }

            if (rejectZone != null)
            {
                rejectZone.ProductProcessed -= HandleProductProcessed;
                rejectZone.ProductProcessed += HandleProductProcessed;
            }
        }

        private void Unsubscribe()
        {
            if (acceptZone != null)
            {
                acceptZone.ProductProcessed -= HandleProductProcessed;
            }

            if (rejectZone != null)
            {
                rejectZone.ProductProcessed -= HandleProductProcessed;
            }
        }

        private void HandleProductProcessed(SortingOutcome outcome, Product product, AcceptRejectZone zone)
        {
            if (episodeStats != null)
            {
                episodeStats.RegisterOutcome(outcome);
            }

            RecordTrainingOutcome(outcome);

            SortingOutcomeRouted?.Invoke(outcome, product, zone);

            if (despawnProcessedProducts && productSpawner != null && product != null)
            {
                productSpawner.DespawnProduct(product);
            }
        }

        private void RecordTrainingOutcome(SortingOutcome outcome)
        {
            if (!recordTrainingStats)
            {
                return;
            }

            bool isCorrect =
                outcome == SortingOutcome.GoodAccepted ||
                outcome == SortingOutcome.DefectRejected;

            Academy.Instance.StatsRecorder.Add(TotalOutcomesStat, 1f, StatAggregationMethod.Sum);
            Academy.Instance.StatsRecorder.Add(AccuracyStat, isCorrect ? 1f : 0f, StatAggregationMethod.Average);

            switch (outcome)
            {
                case SortingOutcome.GoodAccepted:
                    Academy.Instance.StatsRecorder.Add(GoodAcceptedStat, 1f, StatAggregationMethod.Sum);
                    break;

                case SortingOutcome.DefectRejected:
                    Academy.Instance.StatsRecorder.Add(DefectRejectedStat, 1f, StatAggregationMethod.Sum);
                    break;

                case SortingOutcome.GoodRejected:
                    Academy.Instance.StatsRecorder.Add(GoodRejectedStat, 1f, StatAggregationMethod.Sum);
                    break;

                case SortingOutcome.DefectMissed:
                    Academy.Instance.StatsRecorder.Add(DefectMissedStat, 1f, StatAggregationMethod.Sum);
                    break;

                default:
                    Debug.LogWarning($"{nameof(SortingEventRouter)}: Unhandled sorting outcome: {outcome}", this);
                    break;
            }
        }
    }
}
