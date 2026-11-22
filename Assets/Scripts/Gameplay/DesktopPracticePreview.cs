using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace VirtualArchery
{
    // Editor convenience only. This never supplies controls in a built VR application.
    public class DesktopPracticePreview : MonoBehaviour
    {
        public BowController bow;
        public PracticeManager practice;
        public Transform headset;
        public bool enableInEditor = true;
        bool active;
        bool wasPressed;
        float charge;
        float yaw;
        float pitch;

        void Update()
        {
#if UNITY_EDITOR
            if (!enableInEditor || bow == null || headset == null) return;
            bool desired = !XRSettings.isDeviceActive;
            if (desired != active)
            {
                active = desired;
                bow.SetDesktopPreview(active);
                if (active)
                {
                    yaw = headset.eulerAngles.y;
                    pitch = 0;
                    headset.position = headset.root.TransformPoint(new Vector3(0, 1.65f, 0));
                }
            }
            if (!active) return;
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null)
            {
                if (keyboard.pKey.wasPressedThisFrame && practice != null) practice.StartPractice();
                if (keyboard.eKey.wasPressedThisFrame && practice != null) practice.EndPractice();
            }
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * 0.08f;
                pitch = Mathf.Clamp(pitch - delta.y * 0.08f, -45f, 45f);
            }
            headset.rotation = Quaternion.Euler(pitch, yaw, 0);
            bow.transform.SetPositionAndRotation(headset.position + headset.forward * 0.5f -
                headset.up * 0.2f + headset.right * 0.12f, headset.rotation);
            bool pressed = (keyboard != null && keyboard.spaceKey.isPressed);
            if (pressed)
            {
                charge = Mathf.Clamp01(charge + Time.deltaTime);
                bow.SetDesktopDraw(charge);
            }
            if (!pressed && wasPressed)
            {
                bow.ReleaseArrow();
                charge = 0;
            }
            wasPressed = pressed;
#endif
        }

        void OnDisable()
        {
#if UNITY_EDITOR
            if (active && bow != null) bow.SetDesktopPreview(false);
#endif
            active = false;
        }
    }
}
