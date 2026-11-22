using UnityEngine;
using UnityEngine.UI;

namespace VirtualArchery
{
    public class PracticeUI : MonoBehaviour
    {
        public PracticeManager practice;
        public TutorialManager tutorial;
        public Text statusText;
        public Text statsText;
        public Text instructionText;
        public Button startButton;
        public Button endButton;
        public Slider drawMeter;
        public GameObject desktopHelp;

        void OnEnable()
        {
            if (practice != null) practice.Changed += Refresh;
            if (tutorial != null) tutorial.MessageChanged += ShowInstruction;
            if (startButton != null) startButton.onClick.AddListener(StartPractice);
            if (endButton != null) endButton.onClick.AddListener(EndPractice);
            Refresh();
        }

        void StartPractice() { if (practice != null) practice.StartPractice(); }
        void EndPractice() { if (practice != null) practice.EndPractice(); }
        void ShowInstruction(string message) { if (instructionText != null) instructionText.text = message; }

        public void Refresh()
        {
            if (practice == null) return;
            PracticeSummary summary = practice.Summary;
            if (statusText != null)
                statusText.text = practice.IsFinishing ? "Finishing the last shots..." :
                    practice.IsPracticing ? "Practice - unlimited arrows, no timer" :
                    practice.HasCompleted ? "Practice finished - choose Start to try again" : "Ready to practise";
            if (statsText != null)
                statsText.text = $"Shots: {summary.shots}    Hits: {summary.hits}    Misses: {summary.misses}\n" +
                    $"Accuracy: {summary.Accuracy:0}%    In flight: {practice.PendingShots}";
            if (startButton != null) startButton.gameObject.SetActive(!practice.IsPracticing);
            if (endButton != null)
            {
                endButton.gameObject.SetActive(practice.IsPracticing);
                endButton.interactable = !practice.IsFinishing;
            }
            if (instructionText != null)
                instructionText.text = practice.IsPracticing && tutorial != null ? tutorial.Message :
                    "Hold the bow with either hand. Use the other hand's trigger to draw and release.";
        }

        void Update()
        {
            if (desktopHelp != null)
                desktopHelp.SetActive(Application.isEditor && !UnityEngine.XR.XRSettings.isDeviceActive);
            if (drawMeter != null && practice != null && practice.bow != null)
                drawMeter.value = practice.bow.DrawNormalized;
        }

        void OnDisable()
        {
            if (practice != null) practice.Changed -= Refresh;
            if (tutorial != null) tutorial.MessageChanged -= ShowInstruction;
            if (startButton != null) startButton.onClick.RemoveListener(StartPractice);
            if (endButton != null) endButton.onClick.RemoveListener(EndPractice);
        }
    }
}
