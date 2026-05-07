using System;
using UnityEngine;

namespace RoboSortRL.Simulation
{
    [RequireComponent(typeof(Product))]
    [RequireComponent(typeof(Rigidbody))]
    [DefaultExecutionOrder(-100)]
    public class ConveyorMover : MonoBehaviour
    {
        public event Action<ConveyorMover> LifetimeExpired;

        [Header("Movement Settings")]
        [SerializeField] private Vector3 conveyorDirection = Vector3.forward;
        [SerializeField] private float moveSpeed = 1.25f;

        [Header("External Scripted Motion")]
        [SerializeField] private float maxExternalSpeed = 4f;

        [Header("Safety")]
        [SerializeField] private float maxLifetimeSeconds = 15f;

        private Rigidbody rb;
        private float lifetime;
        private bool lifetimeExpiredReported;

        private Vector3 requestedExternalVelocity;

        public float MoveSpeed => moveSpeed;
        public Vector3 ConveyorDirection => conveyorDirection.normalized;
        public Vector3 LastFrameVelocity { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            lifetime = 0f;
            lifetimeExpiredReported = false;
            requestedExternalVelocity = Vector3.zero;
            LastFrameVelocity = Vector3.zero;
        }

        private void FixedUpdate()
        {
            MoveProduct();
            TrackLifetime(Time.fixedDeltaTime);
        }

        public void Initialize(float speed, Vector3 direction)
        {
            moveSpeed = Mathf.Max(0f, speed);
            conveyorDirection = direction.sqrMagnitude > 0.0001f
                ? direction.normalized
                : Vector3.forward;

            lifetime = 0f;
            lifetimeExpiredReported = false;
            requestedExternalVelocity = Vector3.zero;
            LastFrameVelocity = Vector3.zero;
        }

        /// <summary>
        /// Requests extra scripted movement for this physics step.
        ///
        /// This is used by the sorter/pusher interaction.
        /// ConveyorMover remains the only script that actually moves the Rigidbody.
        /// </summary>
        public void RequestExternalVelocity(Vector3 velocity)
        {
            velocity.y = 0f;

            if (velocity.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            requestedExternalVelocity += velocity;
            requestedExternalVelocity = Vector3.ClampMagnitude(requestedExternalVelocity, maxExternalSpeed);
        }

        /// <summary>
        /// Convenience method for lateral pusher motion.
        /// Direction is flattened to XZ so push logic cannot accidentally launch products vertically.
        /// </summary>
        public void RequestLateralPush(Vector3 direction, float speed)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            RequestExternalVelocity(direction.normalized * Mathf.Max(0f, speed));
        }

        private void MoveProduct()
        {
            Vector3 conveyorVelocity = ConveyorDirection * moveSpeed;
            Vector3 totalVelocity = conveyorVelocity + requestedExternalVelocity;

            LastFrameVelocity = totalVelocity;

            Vector3 step = totalVelocity * Time.fixedDeltaTime;
            rb.MovePosition(rb.position + step);

            // External motion is request-based.
            // If no pusher script requests it next step, the product returns to normal conveyor motion.
            requestedExternalVelocity = Vector3.zero;
        }

        private void TrackLifetime(float deltaTime)
        {
            if (lifetimeExpiredReported)
            {
                return;
            }

            lifetime += deltaTime;

            if (lifetime <= maxLifetimeSeconds)
            {
                return;
            }

            lifetimeExpiredReported = true;

            if (LifetimeExpired == null)
            {
                Debug.LogWarning(
                    $"{nameof(ConveyorMover)}: Lifetime expired but no listener handled cleanup. Product={name}",
                    this
                );
                return;
            }

            LifetimeExpired.Invoke(this);
        }
    }
}