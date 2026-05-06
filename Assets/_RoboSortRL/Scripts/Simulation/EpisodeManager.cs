using UnityEngine;

namespace RoboSortRL.Simulation
{
    /// <summary>
    /// Central reset authority for one RoboSortRL sorting cell.
    ///
    /// Manual testing and the future RoboSortAgent should call BeginEpisode()
    /// instead of resetting ProductSpawner, EpisodeStats, or SorterController separately.
    ///
    /// This keeps episode reset deterministic and prepares the scene for ML-Agents.
    /// </summary>
    public class EpisodeManager : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private ProductSpawner productSpawner;
        [SerializeField] private EpisodeStats episodeStats;
        [SerializeField] private SorterController sorterController;

        [Header("Episode Settings")]
        // For manual Day 5 testing, this can stay true.
        // When RoboSortAgent is added, set this to false and let Agent.OnEpisodeBegin() call BeginEpisode().
        [SerializeField] private bool beginEpisodeOnStart = false;
        [SerializeField] private bool spawnProductOnEpisodeBegin = true;

        [Header("Debug")]
        [SerializeField] private int episodeIndex;

        public int EpisodeIndex => episodeIndex;

        private void Awake()
        {
            ValidateReferences();
        }

        private void Start()
        {
            if (beginEpisodeOnStart)
            {
                BeginEpisode();
            }
        }

        /// <summary>
        /// Clears products, resets stats, resets sorter pose, and optionally spawns a new product.
        /// Future RoboSortAgent.OnEpisodeBegin() should call this method.
        /// </summary>
        public void BeginEpisode()
        {
            episodeIndex++;

            if (productSpawner != null)
            {
                productSpawner.ClearProducts();
                productSpawner.ResetSpawnTimer();
            }

            if (episodeStats != null)
            {
                episodeStats.ResetStats();
            }

            if (sorterController != null)
            {
                sorterController.ResetSorter();
            }

            if (spawnProductOnEpisodeBegin && productSpawner != null)
            {
                productSpawner.SpawnRandomProduct();
            }
        }

        private void ValidateReferences()
        {
            if (productSpawner == null)
            {
                Debug.LogError($"{nameof(EpisodeManager)} on {name}: ProductSpawner reference is missing.", this);
            }

            if (episodeStats == null)
            {
                Debug.LogError($"{nameof(EpisodeManager)} on {name}: EpisodeStats reference is missing.", this);
            }

            if (sorterController == null)
            {
                Debug.LogError($"{nameof(EpisodeManager)} on {name}: SorterController reference is missing.", this);
            }
        }
    }
}