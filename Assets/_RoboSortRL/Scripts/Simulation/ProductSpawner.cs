using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoboSortRL.Simulation
{
    public class ProductSpawner : MonoBehaviour
    {
        [Header("Prefab References")]
        [SerializeField] private GameObject productPrefab;

        [Header("Scene References")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Transform productContainer;

        [Header("Spawn Settings")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private bool autoSpawn = false;
        [SerializeField] private float spawnIntervalSeconds = 2.5f;

        [Tooltip("Probability that a spawned product is defective. 0.5 means balanced good/defective products.")]
        [SerializeField, Range(0f, 1f)] private float defectProbability = 0.5f;

        [Header("Spawn Randomization")]
        [Tooltip("If enabled, each product spawns with a random lateral offset along the spawn point's right vector.")]
        [SerializeField] private bool randomizeSpawnX = false;

        [Tooltip("Maximum absolute lateral offset from the spawn point.")]
        [SerializeField, Min(0f)] private float spawnXRange = 0.35f;

        [Header("Reproducibility")]
        [SerializeField] private bool useFixedSeed = false;
        [SerializeField] private int randomSeed = 0;

        [Header("Conveyor Settings")]
        [SerializeField] private float conveyorSpeed = 1.25f;
        [SerializeField] private Vector3 conveyorDirection = Vector3.forward;

        [Header("Safety")]
        [SerializeField, Min(1)] private int maxActiveProducts = 1;

        private readonly List<Product> activeProducts = new List<Product>();
        private System.Random rng;
        private float spawnTimer;

        public int ActiveProductCount => activeProducts.Count;
        public IReadOnlyList<Product> ActiveProducts => activeProducts;

        /// <summary>
        /// Returns the single active product used by the current training setup.
        /// This is intended for the one-product-at-a-time environment where maxActiveProducts is 1.
        /// If multi-product training is added later, use ActiveProducts and define product-selection logic explicitly.
        /// </summary>
        public Product CurrentProduct => activeProducts.Count > 0 ? activeProducts[0] : null;

        private void Awake()
        {
            InitializeRng();
        }

        private void Start()
        {
            spawnTimer = 0f;

            if (spawnOnStart)
            {
                SpawnRandomProduct();
            }
        }

        private void Update()
        {
            CleanupNullReferences();

            if (!autoSpawn)
            {
                return;
            }

            spawnTimer += Time.deltaTime;

            if (spawnTimer >= spawnIntervalSeconds)
            {
                spawnTimer = 0f;
                SpawnRandomProduct();
            }
        }

        public Product SpawnRandomProduct()
        {
            ProductType type = rng.NextDouble() < defectProbability
                ? ProductType.Defective
                : ProductType.Good;

            return SpawnProduct(type);
        }

        public Product SpawnProduct(ProductType productType)
        {
            if (productPrefab == null)
            {
                Debug.LogError($"{nameof(ProductSpawner)}: Product prefab is not assigned.", this);
                return null;
            }

            if (spawnPoint == null)
            {
                Debug.LogError($"{nameof(ProductSpawner)}: Spawn point is not assigned.", this);
                return null;
            }

            CleanupNullReferences();

            if (activeProducts.Count >= maxActiveProducts)
            {
                return null;
            }

            Transform parent = productContainer != null ? productContainer : transform;

            GameObject productObject = Instantiate(
                productPrefab,
                GetSpawnPosition(),
                spawnPoint.rotation,
                parent
            );

            Product product = productObject.GetComponent<Product>();
            if (product == null)
            {
                Debug.LogError($"{nameof(ProductSpawner)}: Spawned prefab has no Product component.", productObject);
                Destroy(productObject);
                return null;
            }

            ConveyorMover mover = productObject.GetComponent<ConveyorMover>();
            if (mover == null)
            {
                Debug.LogError($"{nameof(ProductSpawner)}: Spawned prefab has no ConveyorMover component.", productObject);
                Destroy(productObject);
                return null;
            }

            product.Initialize(productType);

            mover.LifetimeExpired -= HandleProductLifetimeExpired;
            mover.LifetimeExpired += HandleProductLifetimeExpired;
            mover.Initialize(conveyorSpeed, conveyorDirection);

            activeProducts.Add(product);

            return product;
        }

        public void DespawnProduct(Product product)
        {
            if (product == null)
            {
                return;
            }

            activeProducts.Remove(product);
            SafelyDestroyProduct(product);
        }

        public void ClearProducts()
        {
            for (int i = activeProducts.Count - 1; i >= 0; i--)
            {
                Product product = activeProducts[i];

                if (product != null)
                {
                    SafelyDestroyProduct(product);
                }
            }

            activeProducts.Clear();
            spawnTimer = 0f;
        }

        public void ResetSpawnTimer()
        {
            spawnTimer = 0f;
        }

        private void InitializeRng()
        {
            rng = useFixedSeed
                ? new System.Random(randomSeed)
                : new System.Random(Guid.NewGuid().GetHashCode());
        }

        private Vector3 GetSpawnPosition()
        {
            if (!randomizeSpawnX || spawnXRange <= 0f)
            {
                return spawnPoint.position;
            }

            float randomOffsetX = Mathf.Lerp(
                -spawnXRange,
                spawnXRange,
                (float)rng.NextDouble()
            );

            return spawnPoint.position + spawnPoint.right * randomOffsetX;
        }

        private void SafelyDestroyProduct(Product product)
        {
            ConveyorMover mover = product.GetComponent<ConveyorMover>();

            if (mover != null)
            {
                mover.LifetimeExpired -= HandleProductLifetimeExpired;
            }

            // Disable immediately so the product cannot trigger zones during reset cleanup.
            product.gameObject.SetActive(false);
            Destroy(product.gameObject);
        }

        private void HandleProductLifetimeExpired(ConveyorMover mover)
        {
            if (mover == null)
            {
                return;
            }

            Product product = mover.GetComponent<Product>();

            if (product == null)
            {
                return;
            }

            DespawnProduct(product);
        }

        private void CleanupNullReferences()
        {
            for (int i = activeProducts.Count - 1; i >= 0; i--)
            {
                if (activeProducts[i] == null)
                {
                    activeProducts.RemoveAt(i);
                }
            }
        }
    }
}