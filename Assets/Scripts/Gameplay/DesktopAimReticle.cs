using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace VirtualArchery
{
    // Desktop draw-power feedback, not a crosshair or a headset overlay.
    public class DesktopAimReticle : MonoBehaviour
    {
        public GameManager game;
        public BowController bow;
        public Camera viewCamera;
        public Canvas canvas;
        public RectTransform sight;
        public Image chargeFill;
        public Text hintText;

        public bool IsVisible { get; private set; }

        static readonly Color RestColor = new Color(.96f, .98f, 1f);
        static readonly Color ChargedColor = new Color(1f, .79f, .28f);

        void OnEnable()
        {
            if (game != null) game.Changed += RefreshVisibility;
            if (bow != null) bow.DrawChanged += ShowDraw;
            ShowDraw(bow != null ? bow.DrawNormalized : 0f);
            RefreshVisibility();
        }

        void LateUpdate()
        {
            RefreshVisibility();
            if (!IsVisible || sight == null || bow.restNock == null || viewCamera == null) return;
            // Follow the bow's actual direction, including its offset from the desktop camera.
            // Gravity is intentionally not compensated: learning the arc remains part of aiming.
            Vector3 point = viewCamera.WorldToViewportPoint(bow.restNock.position + bow.transform.forward * 10f);
            if (point.z <= 0f || point.x < 0f || point.x > 1f || point.y < 0f || point.y > 1f)
            {
                SetVisible(false);
                return;
            }
            Vector2 anchor = new Vector2(point.x, point.y);
            sight.anchorMin = anchor;
            sight.anchorMax = anchor;
            sight.anchoredPosition = Vector2.zero;
        }

        void RefreshVisibility()
        {
#if UNITY_EDITOR
            bool playing = game != null && (game.State == RangeState.Practice || game.State == RangeState.Competitive);
            SetVisible(playing && !XRSettings.isDeviceActive && bow != null && bow.ShootingEnabled);
#else
            SetVisible(false);
#endif
        }

        void SetVisible(bool visible)
        {
            IsVisible = visible && canvas != null;
            if (canvas != null && canvas.enabled != IsVisible) canvas.enabled = IsVisible;
        }

        void ShowDraw(float amount)
        {
            amount = Mathf.Clamp01(amount);
            if (chargeFill != null)
            {
                chargeFill.color = Color.Lerp(RestColor, ChargedColor, amount);
                chargeFill.rectTransform.anchorMax = new Vector2(amount, 1f);
            }
            if (hintText != null)
                hintText.text = amount >= .98f ? "FULL DRAW | release Space" : "POWER | hold Space";
        }

        void OnDisable()
        {
            if (game != null) game.Changed -= RefreshVisibility;
            if (bow != null) bow.DrawChanged -= ShowDraw;
            SetVisible(false);
        }
    }
}
