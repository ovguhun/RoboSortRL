using System;
using UnityEngine;

namespace RoboSortRL.Simulation
{
    [RequireComponent(typeof(Product))]
    [RequireComponent(typeof(Rigidbody))]
    public class ConveyorMover : MonoBehaviour
    {
        public event Action<ConveyorMover> LifetimeExpired;

        [Header("Movement Settings")]
        [SerializeField] private Vector3 conveyorDirection = Vector3.forward;
        [SerializeField] private float moveSpeed = 1.25f;

        [Header("Safety")]
        [SerializeField] private float maxLifetimeSeconds = 15f;
        [SerializeField] private bool destroyOnLifetimeExpired = true;

        private Rigidbody rb;
        private float lifetime;

        public float MoveSpeed => moveSpeed;
        public Vector3 ConveyorDirection => conveyorDirection.normalized;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            lifetime = 0f;
        }

        private void FixedUpdate()
        {
            MoveAlongConveyor();
            TrackLifetime(Time.fixedDeltaTime);
        }

        public void Initialize(float speed, Vector3 direction)
        {
            moveSpeed = Mathf.Max(0f, speed);
            conveyorDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.forward;

            lifetime = 0f;
        }

        private void MoveAlongConveyor()
        {
            Vector3 step = ConveyorDirection * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + step);
        }

        private void TrackLifetime(float deltaTime)
        {
            lifetime += deltaTime;

            if (lifetime <= maxLifetimeSeconds)
            {
                return;
            }

            LifetimeExpired?.Invoke(this);

            if (destroyOnLifetimeExpired)
            {
                Debug.LogWarning($"{nameof(ConveyorMover)}: Product exceeded lifetime and will be destroyed. Product={name}", this);
                Destroy(gameObject);
            }
        }
    }
}