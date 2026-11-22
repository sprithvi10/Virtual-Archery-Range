using System;
using UnityEngine;

namespace VirtualArchery
{
    public class ChallengeManager : MonoBehaviour
    {
        [Header("Scene references")]
        public BowController bow;
        public ScoreManager score;
        public DifficultyManager difficulty;

        [Header("Competitive rules")]
        [Min(0f)] public float roundDuration = 60f;
        [Min(0)] public int arrowLimit = 12;
        [Min(0)] public int winningScore = 50;

        public event Action Changed;
        public event Action Completed;
        public bool IsRunning { get; private set; }
        public bool IsFinishing { get; private set; }
        public float RemainingTime { get; private set; }
        public int ArrowsRemaining { get; private set; }
        public bool Won { get; private set; }

        ScoreManager subscribedScore;
        DifficultyManager subscribedDifficulty;
        int processedResults;
        bool startingChallenge;
        int sessionVersion;

        void Awake()
        {
            if (score == null) score = FindAnyObjectByType<ScoreManager>();
            if (bow == null) bow = score != null ? score.bow : FindAnyObjectByType<BowController>();
            if (difficulty == null) difficulty = FindAnyObjectByType<DifficultyManager>();
        }

        void OnEnable()
        {
            SubscribeScore();
        }

        void SubscribeScore()
        {
            if (subscribedScore == score) return;
            UnsubscribeScore();
            subscribedScore = score;
            if (subscribedScore != null) subscribedScore.Changed += OnScoreChanged;
        }

        void UnsubscribeScore()
        {
            if (subscribedScore != null) subscribedScore.Changed -= OnScoreChanged;
            subscribedScore = null;
        }

        void SubscribeDifficulty()
        {
            if (subscribedDifficulty == difficulty) return;
            UnsubscribeDifficulty();
            subscribedDifficulty = difficulty;
            if (subscribedDifficulty != null) subscribedDifficulty.Changed += OnDifficultyChanged;
        }

        void UnsubscribeDifficulty()
        {
            if (subscribedDifficulty != null) subscribedDifficulty.Changed -= OnDifficultyChanged;
            subscribedDifficulty = null;
        }

        public void StartChallenge()
        {
            if (!isActiveAndEnabled || IsRunning || IsFinishing) return;
            SubscribeScore();
            if (bow == null || score == null || !score.IsTracking)
            {
                SetShootingEnabled(false);
                Debug.LogError("Assign Bow and Score, then begin a score session before starting a challenge.", this);
                return;
            }

            int version = ++sessionVersion;
            // Initialize before layout/bow callbacks can publish any score changes.
            processedResults = score.Hits + score.Misses;
            RemainingTime = Mathf.Max(0f, roundDuration);
            ArrowsRemaining = Mathf.Max(0, Mathf.Max(0, arrowLimit) - score.Shots);
            Won = false;
            IsRunning = true;
            IsFinishing = false;
            startingChallenge = true;
            SubscribeDifficulty();
            SetShootingEnabled(false);
            if (version != sessionVersion || !IsRunning) return;
            if (difficulty != null) difficulty.StartCompetitiveLayout();
            if (version != sessionVersion || !IsRunning) return;
            startingChallenge = false;

            if (RemainingTime <= 0f || ArrowsRemaining == 0)
            {
                BeginFinishing();
                return;
            }
            RefreshRunningRound(version);
            if (version == sessionVersion && IsRunning) Changed?.Invoke();
        }

        void Update()
        {
            if (!IsRunning || startingChallenge || (difficulty != null && difficulty.IsTransitioning)) return;
            RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
            if (RemainingTime <= 0f) BeginFinishing();
            // UI can poll the displayed second without rebuilding its strings each frame.
        }

        void OnScoreChanged()
        {
            if (!IsRunning && !IsFinishing) return;
            if (score == null || !score.IsTracking)
            {
                CancelChallenge();
                return;
            }

            int version = sessionVersion;
            int previousArrows = ArrowsRemaining;
            ArrowsRemaining = Mathf.Max(0, Mathf.Max(0, arrowLimit) - score.Shots);
            int resolved = score.Hits + score.Misses;
            bool newResult = resolved > processedResults;
            int points = score.LastPoints;
            // Record consumption before publishing difficulty events. ShotFired and
            // reentrant Changed notifications must never count the same result twice.
            if (newResult) processedResults = resolved;

            if (IsRunning && (ArrowsRemaining == 0 || RemainingTime <= 0f))
            {
                // Score.Changed is synchronous with ShotFired: the last arrow
                // disables shooting before BowController can reload or fire again.
                SetShootingEnabled(false);
                if (version != sessionVersion || (!IsRunning && !IsFinishing)) return;
            }
            if (newResult && difficulty != null) difficulty.RecordResolvedShot(points);
            if (version != sessionVersion || (!IsRunning && !IsFinishing)) return;
            if (startingChallenge) return;

            if (IsRunning)
            {
                if (ArrowsRemaining == 0 || RemainingTime <= 0f)
                {
                    BeginFinishing();
                    return;
                }
                RefreshRunningRound(version);
                if (version != sessionVersion || !IsRunning) return;
                if (previousArrows != ArrowsRemaining) Changed?.Invoke();
                return;
            }

            // A final-arrow result still contributes precision/score, but never
            // starts a decorative transition or delays completion after the budget.
            if (score.PendingShots == 0) Complete(version);
            else if (previousArrows != ArrowsRemaining) Changed?.Invoke();
        }

        void OnDifficultyChanged()
        {
            if ((!IsRunning && !IsFinishing) || startingChallenge) return;
            int version = sessionVersion;
            if (IsRunning) RefreshRunningRound(version);
            if (version == sessionVersion && (IsRunning || IsFinishing)) Changed?.Invoke();
        }

        void RefreshRunningRound(int version)
        {
            if (version != sessionVersion || !IsRunning || startingChallenge) return;
            if (score == null || !score.IsTracking)
            {
                CancelChallenge();
                return;
            }
            if (ArrowsRemaining == 0 || RemainingTime <= 0f)
            {
                BeginFinishing();
                return;
            }

            if (difficulty != null && difficulty.AdvancePending && score.PendingShots == 0 &&
                !difficulty.IsTransitioning)
            {
                // Disable before AdvanceIfReady publishes its transition-start event,
                // including to observers subscribed ahead of this challenge.
                SetShootingEnabled(false);
                if (version != sessionVersion || !IsRunning || !score.IsTracking) return;
                if (score.PendingShots == 0) difficulty.AdvanceIfReady();
                if (version != sessionVersion || !IsRunning || !score.IsTracking) return;
            }

            bool ready = difficulty == null ||
                (difficulty.isActiveAndEnabled && difficulty.IsCompetitive &&
                 difficulty.ActiveTarget != null && difficulty.ActiveTarget.gameObject.activeInHierarchy &&
                 !difficulty.IsTransitioning);
            SetShootingEnabled(ready && ArrowsRemaining > 0 && RemainingTime > 0f);
        }

        void SetShootingEnabled(bool enabled)
        {
            // Disabling the bow publishes DrawChanged; avoid redundant reentrant callbacks.
            if (bow != null && bow.ShootingEnabled != enabled) bow.SetShootingEnabled(enabled);
        }

        void BeginFinishing()
        {
            if (!IsRunning) return;
            int version = sessionVersion;
            IsRunning = false;
            IsFinishing = true;
            SetShootingEnabled(false);
            if (version != sessionVersion || !IsFinishing) return;
            if (difficulty != null) difficulty.CancelTransition();
            if (version != sessionVersion || !IsFinishing) return;
            if (score != null && score.PendingShots == 0) Complete(version);
            else Changed?.Invoke();
        }

        void Complete(int version)
        {
            if (version != sessionVersion || !IsFinishing || score == null || score.PendingShots != 0)
                return;
            IsRunning = false;
            IsFinishing = false;
            Won = score.Points >= winningScore;
            UnsubscribeDifficulty();
            if (difficulty != null) difficulty.CancelTransition();
            if (version != sessionVersion) return;
            SetShootingEnabled(false);
            if (version != sessionVersion) return;
            Changed?.Invoke();
            // Changed listeners may cancel/retry immediately. Never complete a new
            // session using the old session's callback or overwrite its reset state.
            if (version == sessionVersion && isActiveAndEnabled && !IsRunning && !IsFinishing)
                Completed?.Invoke();
        }

        public void CancelChallenge()
        {
            int version = ++sessionVersion;
            IsRunning = false;
            IsFinishing = false;
            startingChallenge = false;
            RemainingTime = 0f;
            ArrowsRemaining = 0;
            Won = false;
            UnsubscribeDifficulty();
            SetShootingEnabled(false);
            if (version != sessionVersion) return;
            if (difficulty != null) difficulty.CancelTransition();
            if (version == sessionVersion) Changed?.Invoke();
        }

        void OnDisable()
        {
            UnsubscribeScore();
            CancelChallenge();
        }
    }
}
