using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualArchery
{
    public sealed class PracticeResultsUI : MonoBehaviour
    {
        public ShotGroupingGraphic diagram;
        public Text statsText;
        public Text distanceText;
        public Text analysisText;
        public Text unplottedText;
        public Dropdown distanceDropdown;
        public Button retryButton;
        public Button menuButton;

        public int SelectedGroup { get; private set; } = -1;

        PracticeShotSummary summary;
        GameManager currentGame;
        Dropdown registeredDropdown;
        Button registeredRetryButton;
        Button registeredMenuButton;

        void OnEnable()
        {
            registeredDropdown = distanceDropdown;
            registeredRetryButton = retryButton;
            registeredMenuButton = menuButton;
            if (registeredDropdown != null) registeredDropdown.onValueChanged.AddListener(SelectGroup);
            if (registeredRetryButton != null) registeredRetryButton.onClick.AddListener(Retry);
            if (registeredMenuButton != null) registeredMenuButton.onClick.AddListener(ReturnToMenu);
        }

        public void Show(ResultsManager results, GameManager game)
        {
            if (results == null || results.IsCompetitive) return;
            currentGame = game;
            PracticeShotSummary next = results.PracticeShots;
            if (!ReferenceEquals(summary, next) || SelectedGroup < 0)
            {
                summary = next;
                SelectedGroup = MostPopulatedGroup();
                PopulateDistances();
            }
            if (statsText != null)
                statsText.text = $"Final score: {results.Points}\nAccuracy: {results.Accuracy:0}%\n" +
                    $"Targets hit: {results.Hits} | Misses: {results.Misses}\nArrows fired: {results.Shots}";
            if (unplottedText != null)
                unplottedText.text = $"Off-target / unplotted (all distances): {(summary != null ? summary.UnplottedShots : 0)}\n" +
                    "No target-face position; excluded from distance grouping.";
            RefreshSelection();
        }

        int MostPopulatedGroup()
        {
            if (summary == null || summary.Groups.Count == 0) return -1;
            int selected = 0;
            for (int i = 1; i < summary.Groups.Count; i++)
                if (summary.Groups[i].Impacts.Count > summary.Groups[selected].Impacts.Count) selected = i;
            return selected;
        }

        void PopulateDistances()
        {
            if (distanceDropdown == null) return;
            distanceDropdown.Hide();
            distanceDropdown.ClearOptions();
            int count = summary != null ? summary.Groups.Count : 0;
            var options = new List<Dropdown.OptionData>(Mathf.Max(1, count));
            for (int i = 0; i < count; i++)
            {
                PracticeDistanceShots group = summary.Groups[i];
                options.Add(new Dropdown.OptionData($"{group.Distance:0.#} m | {group.Impacts.Count} impacts"));
            }
            if (count == 0)
                options.Add(new Dropdown.OptionData($"{(summary != null ? summary.CurrentDistance : 6f):0.#} m | 0 impacts"));
            distanceDropdown.AddOptions(options);
            distanceDropdown.interactable = count > 1;
            distanceDropdown.SetValueWithoutNotify(Mathf.Max(0, SelectedGroup));
            distanceDropdown.RefreshShownValue();
        }

        public void SelectGroup(int index)
        {
            if (summary == null || index < 0 || index >= summary.Groups.Count) return;
            SelectedGroup = index;
            RefreshSelection();
        }

        void RefreshSelection()
        {
            int count = summary != null ? summary.Groups.Count : 0;
            PracticeDistanceShots selected = SelectedGroup >= 0 && SelectedGroup < count
                ? summary.Groups[SelectedGroup] : null;
            if (distanceDropdown != null)
                distanceDropdown.SetValueWithoutNotify(Mathf.Max(0, SelectedGroup));
            if (diagram != null) diagram.SetShots(selected);
            float distance = selected != null ? selected.Distance : summary != null ? summary.CurrentDistance : 6f;
            int impacts = selected != null ? selected.Impacts.Count : 0;
            if (distanceText != null)
                distanceText.text = $"{distance:0.#} m | {impacts} plotted impacts";
            if (analysisText == null) return;
            if (impacts < 3)
            {
                analysisText.text = $"Insufficient sample: {impacts} plotted impacts.\n" +
                    "Need at least 3 at this distance to assess grouping.\n" +
                    "Based on this distance's plotted impacts only.";
                return;
            }
            float offset = selected.Centroid.magnitude;
            string consistency = selected.Spread <= .2f
                ? "Tight group - consistent impact placement."
                : "Wide group - less consistent impact placement.";
            string aiming = offset <= .2f
                ? "Average impact is near centre."
                : "Group is off-centre - review aiming offset.";
            analysisText.text = consistency + "\n" + aiming + "\n" +
                $"RMS spread: {selected.Spread * 100f:0.#}% of target radius\n" +
                $"Centre offset: {offset * 100f:0.#}% of target radius\n" +
                "Based on this distance's plotted impacts only.";
        }

        void Retry() { if (currentGame != null) currentGame.Retry(); }
        void ReturnToMenu() { if (currentGame != null) currentGame.ReturnToMenu(); }

        void OnDisable()
        {
            if (registeredDropdown != null) registeredDropdown.onValueChanged.RemoveListener(SelectGroup);
            if (registeredRetryButton != null) registeredRetryButton.onClick.RemoveListener(Retry);
            if (registeredMenuButton != null) registeredMenuButton.onClick.RemoveListener(ReturnToMenu);
            registeredDropdown = null;
            registeredRetryButton = null;
            registeredMenuButton = null;
        }
    }
}
