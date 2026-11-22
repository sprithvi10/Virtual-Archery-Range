using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VirtualArchery
{
    public class GameUI : MonoBehaviour
    {
        public GameManager game;
        public Text titleText;
        public Text statusText;
        public Text rulesText;
        public Text scoreText;
        public Text feedbackText;
        public Text helpText;
        public GameObject menuPanel;
        public GameObject gameplayPanel;
        public Canvas practiceCanvas;
        public Button practiceButton;
        public Button competitiveButton;
        public Button endPracticeButton;
        public Button menuButton;
        public Slider drawMeter;

        PracticeUI practiceUI;
        TrackedDeviceGraphicRaycaster practiceRaycaster;
        GraphicRaycaster practiceMouseRaycaster;
        Canvas gameCanvas;
        TrackedDeviceGraphicRaycaster gameRaycaster;
        GraphicRaycaster gameMouseRaycaster;
        int displayedSeconds = -1;
        bool hasResolvedShot;
        int previousResolved;
#if UNITY_EDITOR
        readonly List<Renderer> desktopBowRenderers = new List<Renderer>();
        bool desktopBowVisible = true;
#endif

        void OnEnable()
        {
            if (game != null)
            {
                gameCanvas = GetComponent<Canvas>();
                gameRaycaster = GetComponent<TrackedDeviceGraphicRaycaster>();
                gameMouseRaycaster = GetComponent<GraphicRaycaster>();
                game.Changed += Refresh;
                game.score.Changed += Refresh;
                game.practice.Changed += Refresh;
                game.challenge.Changed += Refresh;
                game.bow.DrawChanged += ShowDraw;
            }
            if (practiceButton != null) practiceButton.onClick.AddListener(StartPractice);
            if (competitiveButton != null) competitiveButton.onClick.AddListener(StartCompetitive);
            if (endPracticeButton != null) endPracticeButton.onClick.AddListener(EndPractice);
            if (menuButton != null) menuButton.onClick.AddListener(ReturnToMenu);
            if (practiceCanvas != null)
            {
                practiceUI = practiceCanvas.GetComponent<PracticeUI>();
                practiceRaycaster = practiceCanvas.GetComponent<TrackedDeviceGraphicRaycaster>();
                practiceMouseRaycaster = practiceCanvas.GetComponent<GraphicRaycaster>();
            }
            Refresh();
        }

        void StartPractice() { game.StartPractice(); }
        void StartCompetitive() { game.StartCompetitive(); }
        void EndPractice() { game.EndPractice(); }
        void ReturnToMenu() { game.ReturnToMenu(); }
        void ShowDraw(float amount) { if (drawMeter != null) drawMeter.value = amount; }

        public void Refresh()
        {
            if (game == null) return;
            bool menu = game.State == RangeState.MainMenu;
            bool practicing = game.State == RangeState.Practice;
            bool competing = game.State == RangeState.Competitive;
            bool playing = practicing || competing;
            if (menuPanel != null) menuPanel.SetActive(menu);
            if (gameplayPanel != null) gameplayPanel.SetActive(playing);
            if (practiceCanvas != null) practiceCanvas.enabled = practicing;
            if (practiceRaycaster != null) practiceRaycaster.enabled = practicing;
            if (practiceMouseRaycaster != null) practiceMouseRaycaster.enabled = practicing;
#if UNITY_EDITOR
            SetDesktopBowVisibility(playing || UnityEngine.XR.XRSettings.isDeviceActive);
#endif

            bool showGameUI = game.State != RangeState.Results;
            if (gameCanvas != null) gameCanvas.enabled = showGameUI;
            if (gameRaycaster != null) gameRaycaster.enabled = showGameUI;
            if (gameMouseRaycaster != null) gameMouseRaycaster.enabled = showGameUI;
            if (titleText != null)
                titleText.text = menu ? "VIRTUAL ARCHERY RANGE" : practicing ? "PRACTICE" : "COMPETITIVE";
            if (menu && rulesText != null)
                rulesText.text = "PRACTICE | Three fixed lanes, unlimited arrows, no timer.\n\n" +
                    $"COMPETITIVE | {game.challenge.roundDuration:0}s | {game.challenge.arrowLimit} arrows | " +
                    $"{game.challenge.winningScore} points to win.\n" +
                    $"{game.difficulty.hitsToAdvance} consecutive hits worth {game.difficulty.minimumQualifyingPoints}+ points earn the next distance.\n" +
                    "A weak hit or miss breaks the streak.\n" +
                    "Gold 10 | Red 8 | Blue 6 | Black 4 | White 2";
            if (statusText != null)
            {
                DifficultyManager difficulty = game.difficulty;
                statusText.text = practicing ? "Choose a lane and practise at your own pace." :
                    difficulty.IsTransitioning ? $"NEXT RANGE EARNED | {difficulty.Distance:0.#} m\nTarget moving - timer paused" :
                    difficulty.AdvancePending ? "NEXT RANGE EARNED\nWaiting for airborne arrows to land" :
                    difficulty.Stage >= difficulty.distances.Length ? "EXPERT RANGE\nKeep your aim steady and finish strong." :
                    $"PRECISION STREAK {difficulty.PrecisionStreak}/{difficulty.hitsToAdvance}\n" +
                    $"Land {difficulty.minimumQualifyingPoints}+ points per hit to earn the next range.";
                statusText.color = competing && (difficulty.IsTransitioning || difficulty.AdvancePending)
                    ? new Color(1f, .79f, .28f) : new Color(.94f, .97f, 1f);
            }
            if (menuButton != null) menuButton.gameObject.SetActive(playing);
            if (endPracticeButton != null)
            {
                endPracticeButton.gameObject.SetActive(practicing);
                endPracticeButton.interactable = !game.practice.IsFinishing;
            }
            if (scoreText != null)
            {
                ScoreManager score = game.score;
                string rules = practicing ? "Unlimited arrows | No timer" :
                    $"Time: {Mathf.CeilToInt(game.challenge.RemainingTime)}s | Arrows left: {game.challenge.ArrowsRemaining}\n" +
                    $"Range {game.difficulty.Stage}/{game.difficulty.distances.Length} | {game.difficulty.Distance:0.#} m | " +
                    $"{Mathf.Max(0, game.challenge.winningScore - score.Points)} points to goal";
                scoreText.text = rules + $"\nScore: {score.Points} | Hits: {score.Hits} | Misses: {score.Misses}\n" +
                    $"Shots: {score.Shots} | Accuracy: {score.Accuracy:0}% | In flight: {score.PendingShots}";
                scoreText.color = competing && game.challenge.RemainingTime <= 10f
                    ? new Color(1f, .54f, .36f) : new Color(.94f, .97f, 1f);
            }
            bool finishing = practicing ? game.practice.IsFinishing : competing && game.challenge.IsFinishing;
            int resolved = game.score.Hits + game.score.Misses;
            if (game.score.Shots == 0) hasResolvedShot = false;
            if (resolved > previousResolved) hasResolvedShot = true;
            previousResolved = resolved;
            if (feedbackText != null)
                feedbackText.text = finishing ? "Finishing airborne arrows..." :
                    competing && game.difficulty.IsTransitioning ? "Good precision. Take a breath and prepare for the longer shot." :
                    competing && game.difficulty.AdvancePending ? "Streak complete! The next range is unlocked." :
                    !hasResolvedShot ? "Aim near the centre. A stronger draw gives a flatter shot." :
                    competing && game.score.LastPoints < game.difficulty.minimumQualifyingPoints ?
                        (game.score.LastPoints > 0 ? $"+{game.score.LastPoints} points | Streak broken - aim closer to gold." :
                        "Miss | Streak reset. Steady your aim and try again.") :
                    game.score.LastPoints > 0 ? $"+{game.score.LastPoints} points! Keep that accuracy." :
                    "Miss. Aim slightly higher or increase draw strength.";
            displayedSeconds = Mathf.CeilToInt(game.challenge.RemainingTime);
            HidePracticeLifecycleButtons();
        }

#if UNITY_EDITOR
        void SetDesktopBowVisibility(bool visible)
        {
            if (visible == desktopBowVisible || game == null || game.bow == null) return;
            game.bow.GetComponentsInChildren(true, desktopBowRenderers);
            foreach (Renderer bowRenderer in desktopBowRenderers) bowRenderer.enabled = visible;
            desktopBowVisible = visible;
        }
#endif

        void LateUpdate()
        {
            if (game == null) return;
            if (game.State == RangeState.Competitive && displayedSeconds != Mathf.CeilToInt(game.challenge.RemainingTime))
                Refresh();
            // Developer 1's panel refreshes independently; route its lifecycle controls through this HUD.
            HidePracticeLifecycleButtons();
            if (helpText != null)
            {
                bool desktop = Application.isEditor && !UnityEngine.XR.XRSettings.isDeviceActive;
                helpText.gameObject.SetActive(desktop);
                if (desktop) helpText.text = game.State == RangeState.MainMenu ?
                    "Editor: 1 Practice | 2 Competitive\nRight mouse + move to aim | Hold Space, release to shoot" :
                    "Editor: Right mouse to aim | Hold/release Space to shoot\nE End Practice | Esc Main Menu | R Retry";
            }
        }

        void HidePracticeLifecycleButtons()
        {
            if (practiceUI == null) return;
            if (practiceUI.startButton != null) practiceUI.startButton.gameObject.SetActive(false);
            if (practiceUI.endButton != null) practiceUI.endButton.gameObject.SetActive(false);
            if (practiceUI.desktopHelp != null) practiceUI.desktopHelp.SetActive(false);
        }

        void OnDisable()
        {
            #if UNITY_EDITOR
            SetDesktopBowVisibility(true);
            #endif
            if (game != null)
            {
                game.Changed -= Refresh;
                game.score.Changed -= Refresh;
                game.practice.Changed -= Refresh;
                game.challenge.Changed -= Refresh;
                game.bow.DrawChanged -= ShowDraw;
            }
            if (practiceButton != null) practiceButton.onClick.RemoveListener(StartPractice);
            if (competitiveButton != null) competitiveButton.onClick.RemoveListener(StartCompetitive);
            if (endPracticeButton != null) endPracticeButton.onClick.RemoveListener(EndPractice);
            if (menuButton != null) menuButton.onClick.RemoveListener(ReturnToMenu);
        }
    }
}
