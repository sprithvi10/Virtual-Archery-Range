using UnityEngine;

namespace VirtualArchery
{
    public class Target : MonoBehaviour, IArrowHitReceiver
    {
        [Header("Scoring face (local XY)")]
        [Min(0.001f)] public float radius = 0.65f;
        public Renderer feedbackRenderer;

        [Header("Hit feedback")]
        public Color feedbackColor = new Color(0.2f, 1f, 0.55f);
        [Min(0f)] public float feedbackDuration = 0.18f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly int ColorProperty = Shader.PropertyToID("_Color");
        MaterialPropertyBlock originalProperties;
        MaterialPropertyBlock flashProperties;
        Renderer flashedRenderer;
        float restoreAt;
        bool isFlashing;

        void Awake()
        {
            originalProperties = new MaterialPropertyBlock();
            flashProperties = new MaterialPropertyBlock();
        }

        public int CalculatePoints(Vector3 worldPoint)
        {
            if (radius <= 0f) return 0;
            Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
            float squaredDistance = localPoint.x * localPoint.x + localPoint.y * localPoint.y;
            if (!(squaredDistance <= radius * radius)) return 0;

            float ringRadius = radius * 0.2f;
            if (squaredDistance <= ringRadius * ringRadius) return 10;
            ringRadius = radius * 0.4f;
            if (squaredDistance <= ringRadius * ringRadius) return 8;
            ringRadius = radius * 0.6f;
            if (squaredDistance <= ringRadius * ringRadius) return 6;
            ringRadius = radius * 0.8f;
            if (squaredDistance <= ringRadius * ringRadius) return 4;
            return 2;
        }

        public void OnArrowHit(ArrowHitInfo hit)
        {
            // The receiver runs before BowController.ShotResolved. Scoring and stage
            // movement belong to the session managers, never to this feedback hook.
            if (!isActiveAndEnabled || feedbackRenderer == null || CalculatePoints(hit.Point) == 0)
                return;

            if (isFlashing && flashedRenderer != feedbackRenderer) RestoreFeedback();
            if (!isFlashing)
            {
                feedbackRenderer.GetPropertyBlock(originalProperties);
                feedbackRenderer.GetPropertyBlock(flashProperties);
            }
            flashedRenderer = feedbackRenderer;
            flashProperties.SetColor(BaseColor, feedbackColor);
            flashProperties.SetColor(ColorProperty, feedbackColor);
            flashedRenderer.SetPropertyBlock(flashProperties);
            restoreAt = Time.time + Mathf.Max(0f, feedbackDuration);
            isFlashing = true;
        }

        void Update()
        {
            if (isFlashing && Time.time >= restoreAt) RestoreFeedback();
        }

        void RestoreFeedback()
        {
            if (isFlashing && flashedRenderer != null)
                flashedRenderer.SetPropertyBlock(originalProperties);
            isFlashing = false;
            flashedRenderer = null;
        }

        void OnDisable()
        {
            RestoreFeedback();
        }
    }
}
