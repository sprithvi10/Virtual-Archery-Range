using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VirtualArchery
{
    [Serializable]
    public struct PracticeSummary
    {
        public int shots;
        public int hits;
        public int misses;
        public float Accuracy => hits + misses == 0 ? 0f : 100f * hits / (hits + misses);
    }

    public class PracticeManager : MonoBehaviour
    {
        public BowController bow;
        public bool autoStart;
        public UnityEvent onPracticeStarted = new UnityEvent();
        public UnityEvent onPracticeEnded = new UnityEvent();

        public event Action Changed;
        public event Action PracticeStarted;
        public event Action<PracticeSummary> PracticeEnded;
        public bool IsPracticing { get; private set; }
        public bool IsFinishing { get; private set; }
        public bool HasCompleted { get; private set; }
        public PracticeSummary Summary { get; private set; }
        public int PendingShots => pending.Count;

        readonly HashSet<Arrow> pending = new HashSet<Arrow>();

        void Awake()
        {
            if (bow == null) bow = FindFirstObjectByType<BowController>();
            if (bow != null) bow.SetShootingEnabled(false);
        }

        void OnEnable()
        {
            if (bow == null) return;
            bow.ShotFired += CountShot;
            bow.ShotResolved += CountResult;
        }

        void Start()
        {
            if (autoStart) StartPractice();
        }

        public void StartPractice()
        {
            if (bow == null)
            {
                Debug.LogError("Assign the scene's Bow to PracticeManager.", this);
                return;
            }
            if (IsPracticing || IsFinishing) return;
            pending.Clear();
            Summary = new PracticeSummary();
            HasCompleted = false;
            IsPracticing = true;
            bow.SetShootingEnabled(true);
            PracticeStarted?.Invoke();
            onPracticeStarted.Invoke();
            Changed?.Invoke();
        }

        public void EndPractice()
        {
            if (!IsPracticing || IsFinishing) return;
            bow.SetShootingEnabled(false);
            IsFinishing = true;
            Changed?.Invoke();
            // Finish after the last shot hits something or reaches its flight timeout.
            if (pending.Count == 0) Finish();
        }

        void CountShot(Arrow arrow)
        {
            if (!IsPracticing || IsFinishing || !pending.Add(arrow)) return;
            PracticeSummary summary = Summary;
            summary.shots++;
            Summary = summary;
            Changed?.Invoke();
        }

        void CountResult(ArrowHitInfo result)
        {
            // Ignore arrows from a previous session and duplicate collision notifications.
            if (!IsPracticing || !pending.Remove(result.Arrow)) return;
            PracticeSummary summary = Summary;
            if (result.IsTarget) summary.hits++;
            else summary.misses++;
            Summary = summary;
            Changed?.Invoke();
            if (IsFinishing && pending.Count == 0) Finish();
        }

        void Finish()
        {
            IsPracticing = false;
            IsFinishing = false;
            HasCompleted = true;
            Changed?.Invoke();
            PracticeEnded?.Invoke(Summary);
            onPracticeEnded.Invoke();
        }

        // Developer 2 calls this when switching to a different mode without a practice result.
        public void CancelPractice()
        {
            IsPracticing = false;
            IsFinishing = false;
            HasCompleted = false;
            pending.Clear();
            if (bow != null) bow.SetShootingEnabled(false);
            Changed?.Invoke();
        }

        void OnDisable()
        {
            CancelPractice();
            if (bow == null) return;
            bow.ShotFired -= CountShot;
            bow.ShotResolved -= CountResult;
        }
    }
}
