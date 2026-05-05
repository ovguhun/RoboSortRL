using System;
using UnityEngine;

namespace RoboSortRL.Simulation
{
    [RequireComponent(typeof(Collider))]
    public class AcceptRejectZone : MonoBehaviour
    {
        public event Action<SortingOutcome, Product, AcceptRejectZone> ProductProcessed;

        [Header("Zone Settings")]
        [SerializeField] private ZoneType zoneType = ZoneType.Accept;

        [Header("Debug")]
        [SerializeField] private bool logEvents = false;

        private Collider zoneCollider;

        public ZoneType Type => zoneType;

        private void Awake()
        {
            zoneCollider = GetComponent<Collider>();
            zoneCollider.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            Product product = other.GetComponentInParent<Product>();

            if (product == null)
            {
                return;
            }

            if (!product.TryMarkProcessed())
            {
                return;
            }

            SortingOutcome outcome = GetOutcome(product);

            ProductProcessed?.Invoke(outcome, product, this);

            if (logEvents)
            {
                Debug.Log($"{nameof(AcceptRejectZone)}: {outcome} | Product={product.name} | Zone={zoneType}", this);
            }
        }

        private SortingOutcome GetOutcome(Product product)
        {
            if (zoneType == ZoneType.Accept)
            {
                return product.IsGood
                    ? SortingOutcome.GoodAccepted
                    : SortingOutcome.DefectMissed;
            }

            return product.IsDefective
                ? SortingOutcome.DefectRejected
                : SortingOutcome.GoodRejected;
        }
    }
}