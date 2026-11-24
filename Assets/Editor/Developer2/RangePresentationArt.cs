using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace VirtualArcheryEditor
{
    // Editor-only, generated scenery. Gameplay objects always remain outside this root.
    internal static class RangePresentationArt
    {
        const string AssetFolder = "Assets/Materials/Developer2/Woodland";

        internal static GameObject Create(out GameObject practiceDecor, out GameObject competitiveDecor)
        {
            Directory.CreateDirectory(AssetFolder);
            AssetDatabase.Refresh();
            var grass = Surface("Meadow", new Color(.25f, .36f, .24f));
            var path = Surface("Range Sand", new Color(.53f, .47f, .34f));
            var wood = Surface("Cedar", new Color(.32f, .21f, .13f));
            var lightWood = Surface("Cut Cedar", new Color(.54f, .38f, .23f));
            var roof = Surface("Shelter Roof", new Color(.14f, .23f, .22f));
            var net = Surface("Backstop Cloth", new Color(.16f, .23f, .22f));
            var gold = Surface("Ochre Markings", new Color(.87f, .69f, .32f));
            var blue = Surface("Course Blue", new Color(.24f, .53f, .64f));
            var foliage = Surface("Pine Needles", new Color(.14f, .28f, .22f));
            var foliageLight = Surface("Sunlit Needles", new Color(.24f, .37f, .24f));
            var stone = Surface("Weathered Stone", new Color(.40f, .44f, .40f));
            var distant = Surface("Distant Ridge", new Color(.29f, .41f, .38f));
            var farRidge = Surface("Far Ridge", new Color(.38f, .48f, .44f));
            var cone = FacetedMesh("Eight Sided Crown", true);
            var mound = FacetedMesh("Low Poly Mound", false);
            var root = new GameObject("Range Environment").transform;
            Box(root, "Woodland Floor", new Vector3(0, -.25f, 36), new Vector3(150, .4f, 150), grass, true);
            Box(root, "Open Shooting Ground", new Vector3(0, -.045f, 8.6f), new Vector3(10.4f, .08f, 18), path);
            Box(root, "Timber Shooting Deck", new Vector3(0, -.025f, -.1f), new Vector3(8.6f, .12f, 4), lightWood, true);
            Box(root, "Shared Firing Line", new Vector3(0, .045f, 1.05f), new Vector3(8.2f, .014f, .075f), gold);

            // A roof above head/reach height, with the supports well outside every shot corridor.
            foreach (float x in new[] { -4.05f, 4.05f })
                foreach (float z in new[] { -1.8f, 1.65f })
                    Box(root, "Shelter Cedar Post", new Vector3(x, 1.5f, z), new Vector3(.16f, 3, .16f), wood, true);
            foreach (int side in new[] { -1, 1 })
            {
                var slope = Box(root, "Pitched Shelter Roof", new Vector3(side * 2.15f, 3.38f, -.08f), new Vector3(4.45f, .16f, 4.3f), roof);
                slope.transform.localRotation = Quaternion.Euler(0, 0, -side * 10);
                Box(root, "Shelter Eave", new Vector3(side * 4.12f, 3.0f, -.08f), new Vector3(.16f, .22f, 3.85f), wood);
                Box(root, "Bench Seat", new Vector3(side * 3.55f, .47f, -.65f), new Vector3(.65f, .12f, 1.9f), lightWood, true);
                foreach (float z in new[] { -1.3f, 0f })
                    Box(root, "Bench Foot", new Vector3(side * 3.55f, .22f, z), new Vector3(.5f, .42f, .14f), wood);
            }
            Box(root, "Shelter Front Beam", new Vector3(0, 2.94f, 1.65f), new Vector3(8.25f, .24f, .18f), wood);
            Sign(root, "Grounds Welcome", "CEDAR HOLLOW  /  ARCHERY GROUNDS", new Vector3(0, 2.95f, 1.54f), new Vector2(4.5f, .30f), 32);

            // Sparse perimeter geometry leaves open access and a clear central sightline.
            foreach (int side in new[] { -1, 1 })
            {
                for (int i = 0; i < 5; i++)
                    Box(root, "Boundary Post", new Vector3(side * 5.4f, .62f, 1.5f + i * 4.1f), new Vector3(.15f, 1.3f, .15f), wood, true);
                for (int rail = 0; rail < 2; rail++)
                    Box(root, "Boundary Rail", new Vector3(side * 5.4f, .43f + rail * .48f, 9.7f), new Vector3(.1f, .11f, 16.5f), lightWood);
            }
            Box(root, "Continuous Arrow Backstop", new Vector3(0, 1.85f, 14.25f), new Vector3(9.7f, 3.7f, .22f), net, true);
            foreach (float x in new[] { -4.9f, 4.9f })
                Box(root, "Backstop Frame Post", new Vector3(x, 1.92f, 14.2f), new Vector3(.22f, 3.9f, .3f), wood, true);
            Box(root, "Backstop Top Beam", new Vector3(0, 3.77f, 14.2f), new Vector3(10, .18f, .3f), lightWood);

            // Reused faceted meshes, rather than high-poly primitive spheres, build a layered horizon.
            var hillPositions = new[] { new Vector3(-40, -.2f, 65), new Vector3(2, -.2f, 82), new Vector3(43, -.2f, 68),
                new Vector3(-23, -.15f, 39), new Vector3(23, -.15f, 43) };
            for (int i = 0; i < hillPositions.Length; i++)
                MeshObject(root, "Wooded Ridge " + (i + 1), mound, hillPositions[i],
                    i < 3 ? new Vector3(40, 15 + i * 2, 27) : new Vector3(25, 9, 18), i < 3 ? farRidge : distant, i * 29);
            var treePositions = new[] { new Vector3(-9, 0, 5), new Vector3(10, 0, 8), new Vector3(-8, 0, 15), new Vector3(8, 0, 18),
                new Vector3(-14, 0, 23), new Vector3(-5, 0, 25), new Vector3(3, 0, 27), new Vector3(13, 0, 28),
                new Vector3(-20, 0, 33), new Vector3(21, 0, 36), new Vector3(-1, 0, 36), new Vector3(8, 0, 40) };
            for (int i = 0; i < treePositions.Length; i++)
            {
                float height = 5.8f + i % 4 * 1.05f;
                var p = treePositions[i];
                Box(root, "Pine Trunk " + (i + 1), p + Vector3.up * height * .25f, new Vector3(.34f, height * .5f, .34f), wood);
                MeshObject(root, "Pine Crown " + (i + 1), cone, p + Vector3.up * height * .22f,
                    new Vector3(height * .34f, height * .83f, height * .34f), i % 3 == 0 ? foliageLight : foliage, i * 47);
            }
            for (int i = 0; i < 5; i++)
                MeshObject(root, "Field Stone " + (i + 1), mound, new Vector3((i % 2 == 0 ? -1 : 1) * (6.8f + i * .55f), 0, 4 + i * 4),
                    new Vector3(1.1f + i * .12f, .5f + i % 2 * .25f, .85f), stone, i * 39);

            practiceDecor = new GameObject("Practice Distance Guide");
            practiceDecor.transform.SetParent(root, false);
            Box(practiceDecor.transform, "Central Practice Guide", new Vector3(0, .008f, 6.6f),
                new Vector3(.035f, .016f, 10.8f), gold);
            foreach (float distance in new[] { 6f, 12f })
            {
                Box(practiceDecor.transform, "Practice Endpoint", new Vector3(0, .014f, distance),
                    new Vector3(2.1f, .024f, .06f), gold);
                Sign(practiceDecor.transform, "Practice Distance", distance + " m",
                    new Vector3(-1.4f, .20f, distance), new Vector2(.55f, .3f), 38);
            }

            competitiveDecor = new GameObject("Competitive Course");
            competitiveDecor.transform.SetParent(root, false);
            foreach (int side in new[] { -1, 1 })
                Box(competitiveDecor.transform, "Precision Track Edge", new Vector3(side * 1.05f, .012f, 8.8f), new Vector3(.055f, .02f, 8), blue);
            for (int i = 0; i < 3; i++)
            {
                float distance = 6 + i * 3;
                Box(competitiveDecor.transform, "Stage " + (i + 1) + " Track Mark", new Vector3(0, .014f, distance), new Vector3(2.1f, .024f, .06f), gold);
                Sign(competitiveDecor.transform, "Stage " + (i + 1) + " Distance", distance + " m", new Vector3(-1.4f, .20f, distance), new Vector2(.55f, .3f), 38);
            }
            Box(competitiveDecor.transform, "Precision Banner", new Vector3(0, 3.15f, 13.95f), new Vector3(6, .55f, .06f), roof);
            Sign(competitiveDecor.transform, "Precision Course Title", "PRECISION COURSE    /    6 - 9 - 12 m", new Vector3(0, 3.15f, 13.91f), new Vector2(5.7f, .43f), 36);
            competitiveDecor.SetActive(false);
            Lighting(root);
            return root.gameObject;
        }

        static Material Surface(string name, Color color)
        {
            string path = AssetFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("A supported lit shader is required for woodland art.");
            material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", .12f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider = false)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = size;
            item.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!collider) UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
            item.isStatic = true;
            return item;
        }

        static void MeshObject(Transform parent, string name, Mesh mesh, Vector3 position, Vector3 scale, Material material, float yaw)
        {
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            item.GetComponent<MeshFilter>().sharedMesh = mesh;
            item.GetComponent<MeshRenderer>().sharedMaterial = material;
            item.isStatic = true;
        }

        static Mesh FacetedMesh(string name, bool cone)
        {
            string path = AssetFolder + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var vertices = new List<Vector3>();
            var indices = new List<int>();
            const int sides = 8;
            Action<Vector3, Vector3, Vector3> triangle = (a, b, c) =>
            {
                int first = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c);
                indices.Add(first); indices.Add(first + 1); indices.Add(first + 2);
            };
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2 / sides;
                float b = (i + 1) * Mathf.PI * 2 / sides;
                var lowerA = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
                var lowerB = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b));
                if (cone) triangle(lowerA, Vector3.up, lowerB);
                else
                {
                    var upperA = new Vector3(Mathf.Cos(a) * .42f, .72f + i % 2 * .12f, Mathf.Sin(a) * .42f);
                    var upperB = new Vector3(Mathf.Cos(b) * .42f, .72f + (i + 1) % 2 * .12f, Mathf.Sin(b) * .42f);
                    triangle(lowerA, upperA, lowerB);
                    triangle(lowerB, upperA, upperB);
                    triangle(upperA, Vector3.up, upperB);
                }
                triangle(Vector3.zero, lowerA, lowerB);
            }
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        static void Sign(Transform parent, string name, string value, Vector3 position, Vector2 metres, int fontSize)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas));
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.transform.localScale = Vector3.one * .003f;
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = metres / .003f;
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var text = new GameObject("Lettering", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.transform.SetParent(root.transform, false);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.sizeDelta = Vector2.zero;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.text = value;
            text.color = new Color(1, .93f, .73f);
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        static void Lighting(Transform root)
        {
            var sun = new GameObject("Woodland Afternoon Sun").AddComponent<Light>();
            sun.transform.SetParent(root, false);
            sun.type = LightType.Directional;
            sun.color = new Color(1, .88f, .71f);
            sun.intensity = 1.25f;
            sun.transform.rotation = Quaternion.Euler(43, -32, 0);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .7f;
            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.55f, .65f, .68f);
            RenderSettings.ambientEquatorColor = new Color(.43f, .48f, .40f);
            RenderSettings.ambientGroundColor = new Color(.24f, .24f, .19f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.61f, .69f, .67f);
            RenderSettings.fogStartDistance = 35;
            RenderSettings.fogEndDistance = 140;
            string path = AssetFolder + "/Afternoon Sky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (sky == null)
            {
                var shader = Shader.Find("Skybox/Procedural");
                if (shader == null) throw new InvalidOperationException("The built-in procedural skybox shader is required.");
                sky = new Material(shader) { name = "Afternoon Sky" };
                sky.SetFloat("_SunSize", .035f);
                sky.SetFloat("_AtmosphereThickness", 1.1f);
                sky.SetColor("_SkyTint", new Color(.57f, .66f, .72f));
                sky.SetColor("_GroundColor", new Color(.33f, .38f, .29f));
                sky.SetFloat("_Exposure", 1.15f);
                AssetDatabase.CreateAsset(sky, path);
            }
            RenderSettings.skybox = sky;
        }
    }
}
