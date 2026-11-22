using UnityEngine;

namespace VirtualArchery
{
    // Only for VRTesting.unity. Developer 2's real Target replaces this in ArcheryRange.
    public class PracticeTestTarget : MonoBehaviour, IArrowHitReceiver
    {
        public Renderer feedbackRenderer;
        public AudioSource impactAudio;
        MaterialPropertyBlock properties;
        Color original;

        void Awake()
        {
            properties = new MaterialPropertyBlock();
            if (feedbackRenderer != null) original = feedbackRenderer.sharedMaterial.color;
        }

        public void OnArrowHit(ArrowHitInfo hit)
        {
            impactAudio?.Play();
            if (feedbackRenderer == null) return;
            properties.SetColor("_BaseColor", new Color(0.2f, 1f, 0.55f));
            properties.SetColor("_Color", new Color(0.2f, 1f, 0.55f));
            feedbackRenderer.SetPropertyBlock(properties);
            CancelInvoke(nameof(RestoreColour));
            Invoke(nameof(RestoreColour), 0.18f);
        }

        void RestoreColour()
        {
            if (feedbackRenderer == null) return;
            properties.SetColor("_BaseColor", original);
            properties.SetColor("_Color", original);
            feedbackRenderer.SetPropertyBlock(properties);
        }
    }
}
