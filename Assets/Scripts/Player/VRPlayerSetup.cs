using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VirtualArchery
{
    public class VRPlayerSetup : MonoBehaviour
    {
        public Transform headset;
        public Transform leftController;
        public Transform rightController;

        void Awake()
        {
            // XRI's Starter Assets provide tracking and actions. This component binds our bow.
            if (FindFirstObjectByType<XRInteractionManager>() == null)
                new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
            if (EventSystem.current == null && FindFirstObjectByType<EventSystem>() == null)
            {
                GameObject events = new GameObject("XR EventSystem");
                events.AddComponent<EventSystem>();
                XRUIInputModule input = events.AddComponent<XRUIInputModule>();
                input.enableXRInput = true;
                input.enableMouseInput = true;
                input.enableBuiltinActionsAsFallback = true;
            }
        }

        void Start()
        {
            foreach (BowController bow in FindObjectsByType<BowController>(FindObjectsSortMode.None))
                bow.BindPlayer(this);
        }
    }
}
