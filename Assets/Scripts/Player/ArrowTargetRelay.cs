using System;
using UnityEngine;
using UnityEngine.Events;

namespace VirtualArchery
{
    // Optional bridge if Developer 2 prefers an Inspector event over implementing the interface.
    public class ArrowTargetRelay : MonoBehaviour, IArrowHitReceiver
    {
        [Serializable]
        public class HitPointEvent : UnityEvent<Vector3> { }
        public HitPointEvent onHit = new HitPointEvent();

        public void OnArrowHit(ArrowHitInfo hit)
        {
            onHit.Invoke(hit.Point);
        }
    }
}
