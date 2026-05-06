using System;
using UnityEngine;

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

            SortingOutcomeRouted?.Invoke(outcome, product, zone);

            if (despawnProcessedProducts && productSpawner != null && product != null)
            {
                productSpawner.DespawnProduct(product);
            }
        }
    }
}