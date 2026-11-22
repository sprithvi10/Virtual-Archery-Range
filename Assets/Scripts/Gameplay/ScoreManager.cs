using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualArchery
{
    public class ScoreManager : MonoBehaviour
    {
        public BowController bow;

        public event Action Changed;
        public bool IsTracking { get; private set; }
        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public int Misses { get; private set; }
        public int Points { get; private set; }
        public int PendingShots => pending.Count;
        public float Accuracy => Hits + Misses == 0 ? 0f : 100f * Hits / (Hits + Misses);
        public int LastPoints { get; private set; }

        readonly HashSet<Arrow> pending = new HashSet<Arrow>();
        BowController subscribedBow;
        bool isEnding;

        void Awake()
        {
            if (bow == null) bow = FindAnyObjectByType<BowController>();
        }

        void OnEnable()
        {
            SubscribeBow();
        }

        void SubscribeBow()
        {
            if (subscribedBow == bow) return;
            UnsubscribeBow();
            subscribedBow = bow;
            if (subscribedBow == null) return;
            subscribedBow.ShotFired += CountShot;
            subscribedBow.ShotResolved += CountResult;
        }

        void UnsubscribeBow()
        {
            if (subscribedBow != null)
            {
                subscribedBow.ShotFired -= CountShot;
                subscribedBow.ShotResolved -= CountResult;
            }
            subscribedBow = null;
        }

        public void BeginSession()
        {
            if (!isActiveAndEnabled) return;
            SubscribeBow();
            if (bow == null)
            {
                Debug.LogError("Assign the scene's Bow to ScoreManager.", this);
                return;
            }

            pending.Clear();
            Shots = 0;
            Hits = 0;
            Misses = 0;
            Points = 0;
            LastPoints = 0;
            isEnding = false;
            IsTracking = true;
            Changed?.Invoke();
        }

        public void EndSession()
        {
            if (!IsTracking || isEnding) return;
            isEnding = true;
            if (pending.Count == 0) IsTracking = false;
            if (bow != null) bow.SetShootingEnabled(false);
            Changed?.Invoke();
        }

        public void CancelSession()
        {
            IsTracking = false;
            isEnding = false;
            pending.Clear();
            if (bow != null) bow.SetShootingEnabled(false);
            Changed?.Invoke();
        }

        void CountShot(Arrow arrow)
        {
            if (!IsTracking || isEnding || arrow == null || arrow.HasResolved || !pending.Add(arrow)) return;
            Shots++;
            // A subscriber may end, cancel, or replace this session synchronously.
            // Do not make any session mutations after publishing Changed.
            Changed?.Invoke();
        }

        void CountResult(ArrowHitInfo result)
        {
            if (!IsTracking || !pending.Remove(result.Arrow)) return;

            Target target = result.Collider != null
                ? result.Collider.GetComponentInParent<Target>() : null;
            int points = target != null ? target.CalculatePoints(result.Point) : 0;
            LastPoints = points;
            Points += points;
            if (points > 0) Hits++;
            else Misses++;
            if (isEnding && pending.Count == 0) IsTracking = false;
            Changed?.Invoke();
        }

        void OnDisable()
        {
            UnsubscribeBow();
            CancelSession();
        }
    }
}
