using System;
using UnityEngine;
using UnityEngine.UI;

namespace VirtualArchery
{
    /// <summary>A noninteractive target face using recorded target-local impact coordinates.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Shot Grouping Graphic")]
    public sealed class ShotGroupingGraphic : MaskableGraphic
    {
        const int FaceSegments = 64;
        const int MarkerSegments = 12;
        const int MarkerGridSize = 48;
        static readonly Vector2[] FaceCircle = CreateCircle(FaceSegments);
        static readonly Vector2[] MarkerCircle = CreateCircle(MarkerSegments);
        static readonly Color32 Outline = new Color32(12, 20, 26, 255);
        static readonly Color32 ImpactColor = new Color32(255, 255, 255, 255);
        static readonly Color32 AnalysisColor = new Color32(75, 245, 220, 255);
        static readonly Color32[] RingColors =
        {
            new Color32(240, 240, 227, 255),
            new Color32(6, 8, 9, 255),
            new Color32(20, 107, 217, 255),
            new Color32(219, 18, 23, 255),
            new Color32(255, 189, 15, 255)
        };

        readonly bool[] occupiedCells = new bool[MarkerGridSize * MarkerGridSize];
        PracticeDistanceShots shots;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnEnable()
        {
            raycastTarget = false;
            base.OnEnable();
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            raycastTarget = false;
            base.OnValidate();
        }
#endif

        public void SetShots(PracticeDistanceShots value)
        {
            // Completed session data is immutable; a repeated Show needs no mesh rebuild.
            if (ReferenceEquals(shots, value)) return;
            shots = value;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float halfSize = Mathf.Min(rect.width, rect.height) * .5f;
            if (halfSize <= 0f) return;

            float margin = halfSize * .045f;
            float plotRadius = halfSize - margin;
            float extent = 1f;
            if (shots != null)
            {
                for (int i = 0; i < shots.Impacts.Count; i++)
                {
                    Vector2 point = shots.Impacts[i];
                    extent = Mathf.Max(extent, Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.y)));
                }
                if (shots.Impacts.Count >= 3)
                {
                    extent = Mathf.Max(extent, Mathf.Max(Mathf.Abs(shots.Centroid.x) + shots.Spread,
                        Mathf.Abs(shots.Centroid.y) + shots.Spread));
                }
            }
            // Fit real outlying target hits and the RMS circle rather than clamping dots onto the face.
            float targetRadius = plotRadius / extent;
            Vector2 centre = rect.center;
            float stroke = halfSize * .007f;
            AddDisk(mesh, centre, targetRadius + stroke, Outline, FaceCircle);
            for (int ring = 0; ring < RingColors.Length; ring++)
                AddDisk(mesh, centre, targetRadius * (5 - ring) / 5f, RingColors[ring], FaceCircle);

            if (shots == null) return;
            Array.Clear(occupiedCells, 0, occupiedCells.Length);
            float markerRadius = Mathf.Min(halfSize * .018f, 5f);
            // A 48x48 display-space grid coalesces visually coincident dots. The FIRST real impact in
            // each cell retains its exact position (never a cell centre). Every impact participates in
            // the recorded count/centroid/RMS; only overlapping display marks are combined. At most
            // 2,304 outlined 12-sided dots use 59,904 vertices, keeping the complete uGUI mesh below
            // its 65,535-vertex limit even for an unlimited session. No sample/statistics are truncated.
            for (int i = 0; i < shots.Impacts.Count; i++)
            {
                Vector2 point = shots.Impacts[i];
                int x = Mathf.Clamp(Mathf.FloorToInt((point.x / extent + 1f) * .5f * MarkerGridSize), 0, MarkerGridSize - 1);
                int y = Mathf.Clamp(Mathf.FloorToInt((point.y / extent + 1f) * .5f * MarkerGridSize), 0, MarkerGridSize - 1);
                int cell = y * MarkerGridSize + x;
                if (occupiedCells[cell]) continue;
                occupiedCells[cell] = true;
                Vector2 position = centre + point * targetRadius;
                AddDisk(mesh, position, markerRadius + stroke, Outline, MarkerCircle);
                AddDisk(mesh, position, markerRadius, ImpactColor, MarkerCircle);
            }

            if (shots.Impacts.Count < 3) return;
            Vector2 mean = centre + shots.Centroid * targetRadius;
            float spreadRadius = shots.Spread * targetRadius;
            if (spreadRadius > 0f)
            {
                AddCircle(mesh, mean, spreadRadius, stroke * 3f, Outline);
                AddCircle(mesh, mean, spreadRadius, stroke * 1.5f, AnalysisColor);
            }
            float crossSize = halfSize * .042f;
            AddCross(mesh, mean, crossSize + stroke, stroke * 3.5f, Outline);
            AddCross(mesh, mean, crossSize, stroke * 1.5f, AnalysisColor);
        }

        static Vector2[] CreateCircle(int segments)
        {
            var circle = new Vector2[segments];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                circle[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }
            return circle;
        }

        static void AddVertex(VertexHelper mesh, Vector2 position, Color32 tint)
        {
            mesh.AddVert(new Vector3(position.x, position.y, 0f), tint, Vector2.zero);
        }

        static void AddDisk(VertexHelper mesh, Vector2 centre, float radius, Color32 tint, Vector2[] circle)
        {
            int start = mesh.currentVertCount;
            AddVertex(mesh, centre, tint);
            for (int i = 0; i < circle.Length; i++) AddVertex(mesh, centre + circle[i] * radius, tint);
            for (int i = 0; i < circle.Length; i++)
                mesh.AddTriangle(start, start + 1 + i, start + 1 + (i + 1) % circle.Length);
        }

        static void AddCircle(VertexHelper mesh, Vector2 centre, float radius, float width, Color32 tint)
        {
            int start = mesh.currentVertCount;
            float inner = Mathf.Max(0f, radius - width * .5f);
            float outer = radius + width * .5f;
            for (int i = 0; i < FaceSegments; i++)
            {
                AddVertex(mesh, centre + FaceCircle[i] * inner, tint);
                AddVertex(mesh, centre + FaceCircle[i] * outer, tint);
            }
            for (int i = 0; i < FaceSegments; i++)
            {
                int current = start + i * 2;
                int next = start + (i + 1) % FaceSegments * 2;
                mesh.AddTriangle(current, current + 1, next + 1);
                mesh.AddTriangle(current, next + 1, next);
            }
        }

        static void AddCross(VertexHelper mesh, Vector2 centre, float size, float width, Color32 tint)
        {
            AddRectangle(mesh, centre, size, width * .5f, tint);
            AddRectangle(mesh, centre, width * .5f, size, tint);
        }

        static void AddRectangle(VertexHelper mesh, Vector2 centre, float halfWidth, float halfHeight, Color32 tint)
        {
            int start = mesh.currentVertCount;
            AddVertex(mesh, centre + new Vector2(-halfWidth, -halfHeight), tint);
            AddVertex(mesh, centre + new Vector2(-halfWidth, halfHeight), tint);
            AddVertex(mesh, centre + new Vector2(halfWidth, halfHeight), tint);
            AddVertex(mesh, centre + new Vector2(halfWidth, -halfHeight), tint);
            mesh.AddTriangle(start, start + 1, start + 2);
            mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
