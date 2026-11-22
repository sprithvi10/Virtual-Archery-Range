using System;
using UnityEngine;

namespace VirtualArchery
{
    public enum RangeState { MainMenu, Practice, Competitive, Results }

    public class GameManager : MonoBehaviour
    {
        public BowController bow;
        public PracticeManager practice;
        public ScoreManager score;
        public ChallengeManager challenge;
        public DifficultyManager difficulty;
        public ResultsManager results;

        public RangeState State { get; private set; } = RangeState.MainMenu;
        public event Action Changed;
        bool awaitingPracticeResult;

        void OnEnable()
        {
            if (practice != null) practice.PracticeEnded += CompletePractice;
            if (challenge != null) challenge.Completed += CompleteChallenge;
            if (score != null) score.Changed += TryCompletePractice;
        }

        void Start()
        {
            ReturnToMenu();
        }

        public void StartPractice()
        {
            if (State != RangeState.MainMenu && State != RangeState.Results) return;
            ResetSession();
            difficulty.SetPracticeLayout();
            State = RangeState.Practice;
            score.BeginSession();
            practice.StartPractice();
            Changed?.Invoke();
        }

        public void StartCompetitive()
        {
            if (State != RangeState.MainMenu && State != RangeState.Results) return;
            ResetSession();
            State = RangeState.Competitive;
            score.BeginSession();
            challenge.StartChallenge();
            Changed?.Invoke();
        }

        public void EndPractice()
        {
            if (State == RangeState.Practice) practice.EndPractice();
        }

        public void ReturnToMenu()
        {
            State = RangeState.MainMenu;
            ResetSession();
            difficulty.SetPracticeLayout();
            Changed?.Invoke();
        }

        public void Retry()
        {
            if (State != RangeState.Results) return;
            if (results.IsCompetitive) StartCompetitive();
            else StartPractice();
        }

        void CompletePractice(PracticeSummary summary)
        {
            if (State != RangeState.Practice) return;
            awaitingPracticeResult = true;
            TryCompletePractice();
        }

        void TryCompletePractice()
        {
            // Practice and score subscribe independently; either callback may run first.
            if (!awaitingPracticeResult || State != RangeState.Practice || score.PendingShots != 0) return;
            awaitingPracticeResult = false;
            Complete(false, false);
        }

        void CompleteChallenge()
        {
            if (State != RangeState.Competitive) return;
            Complete(true, challenge.Won);
        }

        void Complete(bool competitive, bool won)
        {
            bow.SetShootingEnabled(false);
            score.EndSession();
            results.Capture(competitive, won, score);
            State = RangeState.Results;
            Changed?.Invoke();
        }

        void ResetSession()
        {
            awaitingPracticeResult = false;
            // Detach tracking before destroying old arrows: their OnDestroy resolves misses.
            score.CancelSession();
            challenge.CancelChallenge();
            practice.CancelPractice();
            bow.SetShootingEnabled(false);
            foreach (Arrow arrow in FindObjectsByType<Arrow>())
                if (arrow.HasLaunched) Destroy(arrow.gameObject);
        }

        void OnDisable()
        {
            if (practice != null) practice.PracticeEnded -= CompletePractice;
            if (challenge != null) challenge.Completed -= CompleteChallenge;
            if (score != null) score.Changed -= TryCompletePractice;
            if (bow != null) bow.SetShootingEnabled(false);
        }
    }
}
