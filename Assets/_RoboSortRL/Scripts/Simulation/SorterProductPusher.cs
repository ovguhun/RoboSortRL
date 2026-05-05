using UnityEngine;

namespace RoboSortRL.Simulation
{
    /// <summary>
    /// Scripted pusher-product interaction.
    ///
    /// This component does not move product Rigidbodies directly.
    /// It detects products inside a local push volume and sends lateral push requests to ConveyorMover.
    ///
    /// This avoids fragile kinematic-vs-kinematic physics impulses and keeps product motion centralized.
    /// </summary>
    public class SorterProductPusher : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SorterController sorterController;
        [SerializeField] private Transform pusherProxy;
        [SerializeField] private Collider sortingZone;

        [Header("Push Direction")]
        [Tooltip("Usually SortingCellRoot or SorterProxy. Its local +X is treated as reject direction.")]
        [SerializeField] private Transform rejectDirectionReference;

        [Header("Push Activation")]
        [SerializeField, Range(0f, 1f)] private float minExtensionAmount = 0.20f;
        [SerializeField, Range(0f, 1f)] private float minPushStrength = 0.50f;
        [SerializeField] private float pushSpeed = 3.0f;

        [Header("Push Detection Volume")]
        [Tooltip("Local offset from PusherProxy used as the center of the scripted push detection box.")]
        [SerializeField] private Vector3 pushVolumeLocalOffset = new Vector3(0.55f, 0f, 0f);

        [Tooltip("Half extents of the scripted push detection box in world units.")]
        [SerializeField] private Vector3 pushVolumeHalfExtents = new Vector3(1.35f, 0.35f, 0.55f);

        [Header("Layer Mask")]
        [SerializeField] private LayerMask productLayerMask;

        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;
        [SerializeField] private int productsPushedThisFrame;

        private readonly Collider[] overlapResults = new Collider[8];

        private void Reset()
        {
            sorterController = GetComponent<SorterController>();
            rejectDirectionReference = transform;
            productLayerMask = LayerMask.GetMask(SimLayers.Product);
        }

        private void Awake()
        {
            if (sorterController == null)
            {
                sorterController = GetComponent<SorterController>();
            }

            if (rejectDirectionReference == null)
            {
                rejectDirectionReference = transform;
            }

            if (sorterController == null)
            {
                Debug.LogError($"{nameof(SorterProductPusher)} on {name}: SorterController reference is missing.", this);
                enabled = false;
                return;
            }

            if (pusherProxy == null)
            {
                Debug.LogError($"{nameof(SorterProductPusher)} on {name}: PusherProxy reference is missing.", this);
                enabled = false;
                return;
            }

            if (sortingZone == null)
            {
                Debug.LogError($"{nameof(SorterProductPusher)} on {name}: SortingZone collider reference is missing.", this);
                enabled = false;
                return;
            }

            if (productLayerMask.value == 0)
            {
                Debug.LogError($"{nameof(SorterProductPusher)} on {name}: Product Layer Mask is empty.", this);
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            productsPushedThisFrame = 0;

            if (!IsPushActive())
            {
                return;
            }

            ApplyScriptedPushToDetectedProducts();
        }

        private bool IsPushActive()
        {
            return sorterController.ExtensionAmount >= minExtensionAmount
                   && sorterController.PushStrength >= minPushStrength;
        }

        private void ApplyScriptedPushToDetectedProducts()
        {
            Vector3 center = GetPushVolumeWorldCenter();
            Quaternion rotation = pusherProxy.rotation;

            int hitCount = Physics.OverlapBoxNonAlloc(
                center,
                pushVolumeHalfExtents,
                overlapResults,
                rotation,
                productLayerMask,
                QueryTriggerInteraction.Ignore
            );

            Vector3 rejectDirection = GetRejectDirection();
            float effectivePushSpeed = pushSpeed * sorterController.PushStrength;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = overlapResults[i];

                if (hit == null)
                {
                    continue;
                }

                Product product = hit.GetComponentInParent<Product>();

                if (product == null)
                {
                    continue;
                }

                if (!IsInsideSortingZone(product.transform.position))
                {
                    continue;
                }

                ConveyorMover mover = product.GetComponent<ConveyorMover>();

                if (mover == null)
                {
                    continue;
                }

                mover.RequestLateralPush(rejectDirection, effectivePushSpeed);
                productsPushedThisFrame++;
            }
        }

        private Vector3 GetRejectDirection()
        {
            Vector3 direction = rejectDirectionReference.right;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return Vector3.right;
            }

            return direction.normalized;
        }

        private Vector3 GetPushVolumeWorldCenter()
        {
            return pusherProxy.TransformPoint(pushVolumeLocalOffset);
        }

        private bool IsInsideSortingZone(Vector3 worldPosition)
        {
            return sortingZone.bounds.Contains(worldPosition);
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos || pusherProxy == null)
            {
                return;
            }

            Gizmos.matrix = Matrix4x4.TRS(
                GetPushVolumeWorldCenter(),
                pusherProxy.rotation,
                Vector3.one
            );

            Gizmos.DrawWireCube(Vector3.zero, pushVolumeHalfExtents * 2f);
        }
    }
}