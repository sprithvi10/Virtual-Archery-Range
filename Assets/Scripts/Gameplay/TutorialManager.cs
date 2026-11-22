using System;
using UnityEngine;

namespace VirtualArchery
{
    public class TutorialManager : MonoBehaviour
    {
        public BowController bow;
        public PracticeManager practice;
        public event Action<string> MessageChanged;
        public string Message { get; private set; } = "Choose Start Practice to begin.";
        public bool IsComplete => stage == 5;
        int stage;

        void Awake()
        {
            if (practice == null) practice = GetComponent<PracticeManager>();
            if (bow == null) bow = FindFirstObjectByType<BowController>();
        }

        void OnEnable()
        {
            if (bow != null)
            {
                bow.HoldingChanged += OnHolding;
                bow.DrawStarted += OnDraw;
                bow.ShotFired += OnShot;
                bow.ShotResolved += OnResult;
            }
            if (practice != null) practice.PracticeStarted += RestartTutorial;
        }

        public void RestartTutorial()
        {
            stage = 0;
            SetMessage("1. Reach for the bow and hold your controller's grip button.");
            if (bow != null && bow.IsHeld) OnHolding(true);
        }

        void OnHolding(bool holding)
        {
            if (!Active || !holding || stage != 0) return;
            stage = 1;
            SetMessage("2. Bring your other hand to the string. Hold its trigger, then pull back.");
        }

        void OnDraw()
        {
            if (!Active || stage > 1) return;
            stage = 2;
            SetMessage("3. Aim the arrow at the target. Release the drawing hand's trigger to shoot.");
        }

        void OnShot(Arrow arrow)
        {
            if (!Active) return;
            if (stage <= 2)
            {
                stage = 3;
                SetMessage("4. Watch where your arrow lands. Compare the hit position with your aim.");
            }
            else if (stage == 4)
            {
                stage = 5;
                SetMessage("Tutorial complete. Keep practising and compare your next shots.");
            }
        }

        void OnResult(ArrowHitInfo hit)
        {
            if (!Active || stage != 3) return;
            stage = 4;
            SetMessage(hit.IsTarget
                ? "5. Target hit! Inspect its position. What will you change? Adjust your aim and shoot again."
                : "5. That shot missed. Adjust your aim or draw farther, then try another shot.");
        }

        bool Active => practice != null && practice.IsPracticing && !practice.IsFinishing;

        void SetMessage(string value)
        {
            Message = value;
            MessageChanged?.Invoke(value);
        }

        void OnDisable()
        {
            if (bow != null)
            {
                bow.HoldingChanged -= OnHolding;
                bow.DrawStarted -= OnDraw;
                bow.ShotFired -= OnShot;
                bow.ShotResolved -= OnResult;
            }
            if (practice != null) practice.PracticeStarted -= RestartTutorial;
        }
    }
}
