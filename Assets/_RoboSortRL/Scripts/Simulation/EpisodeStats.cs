using UnityEngine;

namespace RoboSortRL.Simulation
{
    public class EpisodeStats : MonoBehaviour
    {
        [Header("Counters")]
        [SerializeField] private int goodAccepted;
        [SerializeField] private int defectRejected;
        [SerializeField] private int goodRejected;
        [SerializeField] private int defectMissed;

        public int GoodAccepted => goodAccepted;
        public int DefectRejected => defectRejected;
        public int GoodRejected => goodRejected;
        public int DefectMissed => defectMissed;

        public int TotalProcessed => goodAccepted + defectRejected + goodRejected + defectMissed;

        public void ResetStats()
        {
            goodAccepted = 0;
            defectRejected = 0;
            goodRejected = 0;
            defectMissed = 0;
        }

        public void RegisterOutcome(SortingOutcome outcome)
        {
            switch (outcome)
            {
                case SortingOutcome.GoodAccepted:
                    goodAccepted++;
                    break;

                case SortingOutcome.DefectRejected:
                    defectRejected++;
                    break;

                case SortingOutcome.GoodRejected:
                    goodRejected++;
                    break;

                case SortingOutcome.DefectMissed:
                    defectMissed++;
                    break;

                default:
                    Debug.LogWarning($"{nameof(EpisodeStats)}: Unknown sorting outcome: {outcome}", this);
                    break;
            }
        }
    }
}