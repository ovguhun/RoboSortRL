using UnityEngine;

namespace RoboSortRL.Simulation
{
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class Product : MonoBehaviour
    {
        [Header("Product State")]
        [SerializeField] private ProductType productType = ProductType.Good;

        [Header("Debug State")]
        [SerializeField] private bool hasBeenProcessed = false;

        [Header("Optional Visual Materials")]
        [SerializeField] private Material goodMaterial;
        [SerializeField] private Material defectiveMaterial;

        private Rigidbody rb;
        private Collider productCollider;
        private Renderer productRenderer;

        public ProductType Type => productType;
        public bool IsGood => productType == ProductType.Good;
        public bool IsDefective => productType == ProductType.Defective;
        public bool HasBeenProcessed => hasBeenProcessed;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            productCollider = GetComponent<Collider>();
            productRenderer = GetComponentInChildren<Renderer>();

            ConfigurePhysicsForStableTraining();
        }

        public void Initialize(ProductType newType)
        {
            productType = newType;
            hasBeenProcessed = false;

            ApplyCorrectTag();
            ApplyProductLayer();
            ApplyVisualMaterial();
        }

        public bool TryMarkProcessed()
        {
            if (hasBeenProcessed)
            {
                return false;
            }

            hasBeenProcessed = true;
            return true;
        }

        private void ConfigurePhysicsForStableTraining()
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.None;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            productCollider.isTrigger = false;
        }

        private void ApplyCorrectTag()
        {
            gameObject.tag = productType == ProductType.Good
                ? SimTags.GoodProduct
                : SimTags.DefectProduct;
        }

        private void ApplyProductLayer()
        {
            int productLayer = LayerMask.NameToLayer(SimLayers.Product);

            if (productLayer == -1)
            {
                Debug.LogWarning($"{nameof(Product)}: Product layer does not exist. Keeping current layer.", this);
                return;
            }

            gameObject.layer = productLayer;
        }

        private void ApplyVisualMaterial()
        {
            if (productRenderer == null)
            {
                return;
            }

            if (productType == ProductType.Good && goodMaterial != null)
            {
                productRenderer.sharedMaterial = goodMaterial;
            }
            else if (productType == ProductType.Defective && defectiveMaterial != null)
            {
                productRenderer.sharedMaterial = defectiveMaterial;
            }
        }
    }
}