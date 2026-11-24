using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VirtualArchery
{
    public class ResultsUI : MonoBehaviour
    {
        public GameManager game;
        public ResultsManager results;
        public GameObject panel;
        public Text outcomeText;
        public Text statsText;
        public Button retryButton;
        public Button menuButton;
        public PracticeResultsUI practiceResults;

        Canvas canvas;
        TrackedDeviceGraphicRaycaster raycaster;
        GraphicRaycaster mouseRaycaster;

        void OnEnable()
        {
            canvas = GetComponent<Canvas>();
            raycaster = GetComponent<TrackedDeviceGraphicRaycaster>();
            mouseRaycaster = GetComponent<GraphicRaycaster>();
            if (game != null) game.Changed += Refresh;
            if (retryButton != null) retryButton.onClick.AddListener(Retry);
            if (menuButton != null) menuButton.onClick.AddListener(ReturnToMenu);
            Refresh();
        }

        void Retry() { game.Retry(); }
        void ReturnToMenu() { game.ReturnToMenu(); }

        public void Refresh()
        {
            bool visible = game != null && game.State == RangeState.Results;
            if (canvas != null) canvas.enabled = visible;
            if (raycaster != null) raycaster.enabled = visible;
            if (mouseRaycaster != null) mouseRaycaster.enabled = visible;
            bool practiceVisible = visible && results != null && !results.IsCompetitive;
            if (panel != null) panel.SetActive(visible && !practiceVisible);
            if (practiceResults != null) practiceResults.gameObject.SetActive(practiceVisible);
            if (!visible || results == null) return;
            if (practiceVisible)
            {
                if (practiceResults != null) practiceResults.Show(results, game);
                return;
            }
            if (outcomeText != null) outcomeText.text = results.Outcome;
            if (statsText != null)
                statsText.text = $"Final score: {results.Points}\nAccuracy: {results.Accuracy:0}%\n" +
                    $"Targets hit: {results.Hits} | Misses: {results.Misses}\nArrows fired: {results.Shots}\n" +
                    $"Goal: {game.challenge.winningScore} points\n" +
                    $"Range reached: {game.difficulty.Distance:0.#} m (stage {game.difficulty.Stage})";
        }

        void OnDisable()
        {
            if (game != null) game.Changed -= Refresh;
            if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
            if (menuButton != null) menuButton.onClick.RemoveListener(ReturnToMenu);
        }
    }
}
