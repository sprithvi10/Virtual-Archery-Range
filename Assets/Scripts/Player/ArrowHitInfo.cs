using UnityEngine;

namespace VirtualArchery
{
    // Developer 2 implements this on Target.cs. No dependency on their class names.
    public interface IArrowHitReceiver
    {
        void OnArrowHit(ArrowHitInfo hit);
    }

    public readonly struct ArrowHitInfo
    {
        public readonly Arrow Arrow;
        public readonly Collider Collider;
        public readonly Vector3 Point;
        public readonly Vector3 Normal;
        public readonly bool IsTarget;

        public ArrowHitInfo(Arrow arrow, Collider collider, Vector3 point,
            Vector3 normal, bool isTarget)
        {
            Arrow = arrow;
            Collider = collider;
            Point = point;
            Normal = normal;
            IsTarget = isTarget;
        }
    }
}
