using System;
using UnityEngine;

namespace VirtualArchery
{
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class Arrow : MonoBehaviour
    {
        [Tooltip("Distance from the arrow's rear to its tip. Root is at the tip; +Z points forward.")]
        public float length = 0.65f;
        public float maximumFlightTime = 8f;
        public float stuckLifetime = 15f;
        public LayerMask hitLayers = ~0;

        public event Action<ArrowHitInfo> Resolved;
        public bool HasLaunched { get; private set; }
        public bool HasResolved { get; private set; }

        Rigidbody body;
        SphereCollider tipCollider;
        Collider[] ignoredColliders = Array.Empty<Collider>();
        Vector3 previousTip;
        float launchedAt;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            tipCollider = GetComponent<SphereCollider>();
            PrepareForBow();
        }

        public void PrepareForBow()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (tipCollider == null) tipCollider = GetComponent<SphereCollider>();
            body.isKinematic = true;
            body.useGravity = false;
            tipCollider.enabled = false;
        }

        public void Launch(Vector3 velocity, Collider[] shooterColliders)
        {
            if (HasLaunched) return;
            transform.SetParent(null, true);
            ignoredColliders = shooterColliders ?? Array.Empty<Collider>();
            tipCollider.enabled = true;
            foreach (Collider other in ignoredColliders)
                if (other != null && other != tipCollider)
                    Physics.IgnoreCollision(tipCollider, other);

            HasLaunched = true;
            launchedAt = Time.time;
            previousTip = transform.position;
            body.isKinematic = false;
            body.useGravity = true;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = velocity;
        }

        void FixedUpdate()
        {
            if (!HasLaunched || HasResolved) return;

            // Sweep the tip as a backup for fast arrows passing through thin targets.
            Vector3 travel = transform.position - previousTip;
            if (travel.sqrMagnitude > 0.000001f)
            {
                float radius = tipCollider.radius * Mathf.Abs(transform.lossyScale.x);
                RaycastHit[] hits = Physics.SphereCastAll(previousTip, radius,
                    travel.normalized, travel.magnitude, hitLayers, QueryTriggerInteraction.Ignore);
                float nearest = float.PositiveInfinity;
                RaycastHit closest = default;
                bool found = false;
                foreach (RaycastHit hit in hits)
                {
                    if (Ignore(hit.collider) || hit.distance >= nearest) continue;
                    nearest = hit.distance;
                    closest = hit;
                    found = true;
                }
                if (found)
                {
                    ResolveImpact(closest.collider, closest.point, closest.normal);
                    return;
                }
            }
            previousTip = transform.position;
            Vector3 velocity = body.linearVelocity;
            if (velocity.sqrMagnitude > 0.1f)
                body.MoveRotation(Quaternion.LookRotation(velocity.normalized, Vector3.up));

            if (Time.time - launchedAt >= maximumFlightTime || transform.position.y < -10f)
            {
                ResolveMiss();
                Destroy(gameObject);
            }
        }

        bool Ignore(Collider other)
        {
            if (other == null || other == tipCollider) return true;
            if (other.GetComponentInParent<Arrow>() != null) return true;
            foreach (Collider ignored in ignoredColliders)
                if (other == ignored) return true;
            return false;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!HasLaunched || HasResolved || Ignore(collision.collider)) return;
            if ((hitLayers.value & (1 << collision.collider.gameObject.layer)) == 0) return;
            ContactPoint contact = collision.GetContact(0);
            ResolveImpact(collision.collider, contact.point, contact.normal);
        }

        void ResolveImpact(Collider other, Vector3 point, Vector3 normal)
        {
            if (HasResolved) return;
            HasResolved = true;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.isKinematic = true;
            tipCollider.enabled = false;
            transform.position = point - normal * 0.02f;
            transform.SetParent(other.transform, true);

            IArrowHitReceiver receiver = null;
            foreach (MonoBehaviour component in other.GetComponentsInParent<MonoBehaviour>())
            {
                if (component is IArrowHitReceiver candidate)
                {
                    receiver = candidate;
                    break;
                }
            }
            var result = new ArrowHitInfo(this, other, point, normal, receiver != null);
            if (receiver != null)
            {
                try { receiver.OnArrowHit(result); }
                catch (Exception exception) { Debug.LogException(exception, this); }
            }
            Resolved?.Invoke(result);
            Destroy(gameObject, stuckLifetime);
        }

        void ResolveMiss()
        {
            if (HasResolved || !HasLaunched) return;
            HasResolved = true;
            Resolved?.Invoke(new ArrowHitInfo(this, null, transform.position, Vector3.zero, false));
        }

        void OnDestroy()
        {
            // A target removed while an arrow is in flight must not leave a round waiting forever.
            ResolveMiss();
        }
    }
}
