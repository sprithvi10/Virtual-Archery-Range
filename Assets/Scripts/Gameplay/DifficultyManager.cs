using System;
using System.Collections.Generic;
using UnityEngine;

namespace VirtualArchery
{
    public class DifficultyManager : MonoBehaviour
    {
        public Target[] targets = Array.Empty<Target>();
        public float[] distances = { 6f, 9f, 12f };
        public GameObject practiceLanes;
        public GameObject competitiveDecor;

        [Header("Precision progression")]
        [Min(1)] public int hitsToAdvance = 3;
        [Min(0)] public int minimumQualifyingPoints = 6;
        [Min(0.01f)] public float transitionDuration = 1f;

        public event Action Changed;
        public int Stage { get; private set; } = 1;
        public float Distance { get; private set; } = 6f;
        public int PrecisionStreak { get; private set; }
        public bool IsTransitioning { get; private set; }
        public bool AdvancePending { get; private set; }
        public bool IsCompetitive { get; private set; }
        public Target ActiveTarget => targets != null && targets.Length > 0
            ? targets[targets.Length / 2] : null;

        readonly List<Arrow> embeddedArrows = new List<Arrow>();
        Target movingTarget;
        Vector3 transitionStart;
        Vector3 transitionDestination;
        float transitionElapsed;

        int StageCount => distances != null && distances.Length > 0 ? distances.Length : 1;

        float DistanceAt(int index)
        {
            return distances != null && distances.Length > 0
                ? distances[Mathf.Clamp(index, 0, distances.Length - 1)] : 6f;
        }

        public void SetPracticeLayout()
        {
            ResetLayout(false);
            if (practiceLanes != null) practiceLanes.SetActive(true);
            if (competitiveDecor != null) competitiveDecor.SetActive(false);
            if (targets != null)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    Target target = targets[i];
                    if (target == null) continue;
                    ClearEmbeddedArrows(target);
                    Vector3 position = target.transform.position;
                    position.x = (i - targets.Length / 2) * 2.2f;
                    position.z = DistanceAt(i);
                    target.transform.position = position;
                    target.gameObject.SetActive(true);
                }
            }
            Changed?.Invoke();
        }

        public void StartCompetitiveLayout()
        {
            ResetLayout(true);
            if (practiceLanes != null) practiceLanes.SetActive(false);
            if (competitiveDecor != null) competitiveDecor.SetActive(true);
            Target activeTarget = ActiveTarget;
            if (targets != null)
            {
                foreach (Target target in targets)
                {
                    if (target == null) continue;
                    ClearEmbeddedArrows(target);
                    bool active = target == activeTarget;
                    if (active)
                    {
                        Vector3 position = target.transform.position;
                        position.x = 0f;
                        position.z = Distance;
                        target.transform.position = position;
                    }
                    target.gameObject.SetActive(active);
                }
            }
            Changed?.Invoke();
        }

        void ResetLayout(bool competitive)
        {
            // Publish only the finished layout, never an intermediate cancellation.
            movingTarget = null;
            transitionElapsed = 0f;
            IsTransitioning = false;
            AdvancePending = false;
            IsCompetitive = competitive;
            Stage = 1;
            Distance = DistanceAt(0);
            PrecisionStreak = 0;
        }

        public void RecordResolvedShot(int points)
        {
            if (!IsCompetitive || IsTransitioning) return;
            int required = Mathf.Max(1, hitsToAdvance);
            int streak = points >= minimumQualifyingPoints
                ? Mathf.Min(PrecisionStreak + 1, required) : 0;
            bool advance = AdvancePending || (streak == required && Stage < StageCount);
            if (streak == PrecisionStreak && advance == AdvancePending) return;
            PrecisionStreak = streak;
            AdvancePending = advance;
            // Once earned, later old-distance arrows cannot revoke or stack an advance.
            Changed?.Invoke();
        }

        // Challenge calls this only after every airborne arrow has resolved.
        public void AdvanceIfReady()
        {
            Target target = ActiveTarget;
            if (!isActiveAndEnabled || !IsCompetitive || !AdvancePending || IsTransitioning ||
                Stage >= StageCount || target == null || !target.gameObject.activeInHierarchy)
                return;

            ClearEmbeddedArrows(target);
            Stage++;
            Distance = DistanceAt(Stage - 1);
            PrecisionStreak = 0;
            AdvancePending = false;
            movingTarget = target;
            transitionStart = target.transform.position;
            transitionDestination = transitionStart;
            transitionDestination.z = Distance;
            transitionElapsed = 0f;
            IsTransitioning = true;
            Changed?.Invoke();
        }

        void Update()
        {
            if (!IsTransitioning) return;
            if (movingTarget == null || !movingTarget.gameObject.activeInHierarchy)
            {
                CancelTransition();
                return;
            }

            transitionElapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(transitionElapsed / Mathf.Max(0.01f, transitionDuration));
            movingTarget.transform.position = Vector3.LerpUnclamped(transitionStart,
                transitionDestination, Mathf.SmoothStep(0f, 1f, progress));
            if (progress < 1f) return;

            movingTarget = null;
            IsTransitioning = false;
            Changed?.Invoke();
        }

        public void CancelTransition()
        {
            bool changed = IsTransitioning || AdvancePending;
            movingTarget = null;
            transitionElapsed = 0f;
            IsTransitioning = false;
            AdvancePending = false;
            if (changed) Changed?.Invoke();
        }

        void ClearEmbeddedArrows(Target target)
        {
            embeddedArrows.Clear();
            target.GetComponentsInChildren(true, embeddedArrows);
            foreach (Arrow arrow in embeddedArrows)
            {
                if (arrow == null || !arrow.HasLaunched || !arrow.HasResolved) continue;
                // Destroy is deferred: detach now so resolved arrows do not ride the target.
                arrow.transform.SetParent(null, true);
                Destroy(arrow.gameObject);
            }
            embeddedArrows.Clear();
        }

        void OnDisable()
        {
            CancelTransition();
        }
    }
}
