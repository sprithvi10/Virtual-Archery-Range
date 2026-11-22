using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace VirtualArchery
{
    // Complements Developer 1's mouse/Space preview without bypassing the game state machine.
    public class DesktopRangeControls : MonoBehaviour
    {
        public GameManager game;

        void Update()
        {
#if UNITY_EDITOR
            if (game == null || XRSettings.isDeviceActive || Keyboard.current == null) return;
            Keyboard keyboard = Keyboard.current;
            if (keyboard.escapeKey.wasPressedThisFrame) game.ReturnToMenu();
            else if (keyboard.digit1Key.wasPressedThisFrame) game.StartPractice();
            else if (keyboard.digit2Key.wasPressedThisFrame) game.StartCompetitive();
            else if (keyboard.eKey.wasPressedThisFrame) game.EndPractice();
            else if (keyboard.rKey.wasPressedThisFrame) game.Retry();
#endif
        }
    }
}
