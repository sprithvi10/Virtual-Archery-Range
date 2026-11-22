using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VirtualArchery;

namespace VirtualArcheryEditor
{
    // Run in Unity so the generated assets use the actual installed package GUIDs.
    public static class Developer1Setup
    {
        const string Prefabs = "Assets/Prefabs/";
        const string Materials = "Assets/Materials/Developer1/";
        const string TestScene = "Assets/Scenes/VRTesting.unity";

        [MenuItem("Archery/Developer 1/Create Assets and Test Scene")]
        public static void CreateAssetsAndTestScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("Stop Play Mode before running Developer 1 setup.");
                return;
            }
            GameObject starterRig = FindStarterRig();
            if (starterRig == null)
            {
                Debug.LogError("Import XR Interaction Toolkit > Samples > Starter Assets in Package Manager, " +
                    "wait for compilation, then run Archery > Developer 1 > Create Assets and Test Scene again.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolders();
            CreateArrow();
            CreateBow();
            CreatePlayer(starterRig);
            CreatePracticeUI();
            AssetDatabase.SaveAssets();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScene) == null)
                CreateTestScene();
            else
                EditorSceneManager.OpenScene(TestScene);

            ValidateScene();
            Debug.Log("Developer 1 setup finished. Open VRTesting and press Play. " +
                "Without a headset: P starts practice, hold/release Space shoots, right mouse aims, E ends practice.");
        }

        static GameObject FindStarterRig()
        {
            return AssetDatabase.FindAssets("t:Prefab")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.Contains("Starter Assets/") &&
                    Path.GetFileNameWithoutExtension(path) == "XR Origin (XR Rig)")
                .OrderByDescending(path => path.Contains("/3.6.1/"))
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .FirstOrDefault();
        }

        static void EnsureFolders()
        {
            foreach (string directory in new[] { "Assets/Scenes", "Assets/Prefabs", "Assets/UI",
                "Assets/Materials/Developer1", "Assets/Audio/Developer1" })
                Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        static Material MakeMaterial(string name, Color colour, bool unlit = false)
        {
            string path = Materials + name + ".mat";
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No supported material shader was found.");
            Material material = new Material(shader) { name = name, color = colour };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static GameObject Shape(PrimitiveType type, Transform parent, string name,
            Vector3 position, Vector3 scale, Material material)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
            return item;
        }

        static Transform Point(Transform parent, string name, Vector3 position)
        {
            Transform point = new GameObject(name).transform;
            point.SetParent(parent, false);
            point.localPosition = position;
            return point;
        }

        static void CreateArrow()
        {
            string path = Prefabs + "Arrow.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject root = new GameObject("Arrow");
            try
            {
                Rigidbody body = root.AddComponent<Rigidbody>();
                body.mass = 0.03f;
                body.useGravity = false;
                body.isKinematic = true;
                SphereCollider tip = root.AddComponent<SphereCollider>();
                tip.radius = 0.014f;
                tip.enabled = false;
                root.AddComponent<Arrow>();

                Material shaft = MakeMaterial("ArrowShaft", new Color(0.48f, 0.3f, 0.14f));
                Material metal = MakeMaterial("ArrowTip", new Color(0.55f, 0.62f, 0.65f));
                Material feather = MakeMaterial("ArrowFeather", new Color(0.12f, 0.7f, 0.95f));
                GameObject stick = Shape(PrimitiveType.Cylinder, root.transform, "Shaft",
                    new Vector3(0, 0, -0.325f), new Vector3(0.01f, 0.325f, 0.01f), shaft);
                stick.transform.localRotation = Quaternion.Euler(90, 0, 0);
                Shape(PrimitiveType.Sphere, root.transform, "Tip", new Vector3(0, 0, -0.01f),
                    new Vector3(0.025f, 0.025f, 0.065f), metal);
                for (int i = 0; i < 3; i++)
                {
                    GameObject fin = Shape(PrimitiveType.Cube, root.transform, "Feather " + i,
                        new Vector3(0, 0, -0.55f), new Vector3(0.05f, 0.003f, 0.1f), feather);
                    fin.transform.localRotation = Quaternion.Euler(0, 0, i * 120);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void CreateBow()
        {
            string path = Prefabs + "Bow.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject root = new GameObject("Bow");
            try
            {
                Rigidbody body = root.AddComponent<Rigidbody>();
                body.mass = 0.5f;
                body.isKinematic = true;
                body.useGravity = false;
                CapsuleCollider handle = root.AddComponent<CapsuleCollider>();
                handle.height = 0.23f;
                handle.radius = 0.045f;
                handle.direction = 1;
                XRGrabInteractable grab = root.AddComponent<XRGrabInteractable>();
                grab.movementType = XRBaseInteractable.MovementType.Instantaneous;
                grab.throwOnDetach = false;
                grab.useDynamicAttach = false;
                grab.attachTransform = Point(root.transform, "GripAttach", Vector3.zero);
                grab.colliders.Add(handle);

                Material wood = MakeMaterial("BowWood", new Color(0.36f, 0.19f, 0.09f));
                Material grip = MakeMaterial("BowGrip", new Color(0.07f, 0.1f, 0.12f));
                Material stringMaterial = MakeMaterial("BowString", new Color(0.9f, 0.9f, 0.8f), true);
                Shape(PrimitiveType.Cube, root.transform, "Handle", Vector3.zero,
                    new Vector3(0.045f, 0.2f, 0.05f), grip);
                LineRenderer limbs = new GameObject("BowLimbs").AddComponent<LineRenderer>();
                limbs.transform.SetParent(root.transform, false);
                limbs.useWorldSpace = false;
                limbs.sharedMaterial = wood;
                limbs.widthMultiplier = 0.028f;
                limbs.numCapVertices = 4;
                limbs.numCornerVertices = 3;
                limbs.generateLightingData = true;
                limbs.positionCount = 11;
                for (int i = 0; i < limbs.positionCount; i++)
                {
                    float y = Mathf.Lerp(-0.5f, 0.5f, i / 10f);
                    limbs.SetPosition(i, new Vector3(0, y, 0.1f - 0.18f * Mathf.Pow(y / 0.5f, 2)));
                }
                LineRenderer bowstring = new GameObject("BowString").AddComponent<LineRenderer>();
                bowstring.transform.SetParent(root.transform, false);
                bowstring.useWorldSpace = false;
                bowstring.sharedMaterial = stringMaterial;
                bowstring.widthMultiplier = 0.004f;
                bowstring.positionCount = 3;

                BowController bow = root.AddComponent<BowController>();
                bow.arrowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Arrow.prefab").GetComponent<Arrow>();
                bow.stringTop = Point(root.transform, "StringTop", new Vector3(0, 0.5f, -0.08f));
                bow.stringBottom = Point(root.transform, "StringBottom", new Vector3(0, -0.5f, -0.08f));
                bow.restNock = Point(root.transform, "RestNock", new Vector3(0, 0, -0.08f));
                bow.bowstring = bowstring;
                bowstring.SetPosition(0, bow.stringTop.position);
                bowstring.SetPosition(1, bow.restNock.position);
                bowstring.SetPosition(2, bow.stringBottom.position);
                bow.leftTrigger = new InputActionProperty(new InputAction("LeftDraw", InputActionType.Value,
                    "<XRController>{LeftHand}/trigger"));
                bow.rightTrigger = new InputActionProperty(new InputAction("RightDraw", InputActionType.Value,
                    "<XRController>{RightHand}/trigger"));
                AudioSource audio = root.AddComponent<AudioSource>();
                audio.playOnAwake = false;
                audio.spatialBlend = 1;
                audio.volume = 0.45f;
                audio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Developer1/BowRelease.wav");
                bow.releaseAudio = audio;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void CreatePlayer(GameObject source)
        {
            string path = Prefabs + "VRPlayer.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
                root.name = "VRPlayer";
                root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                foreach (Behaviour component in root.GetComponentsInChildren<Behaviour>(true))
                {
                    string space = component.GetType().Namespace ?? "";
                    string name = component.GetType().Name;
                    if (space.Contains(".Locomotion") || name == "XRGazeAssistance" ||
                        name == "XRGazeInteractor" || name == "GazeInputManager") component.enabled = false;
                }
                Transform locomotion = FindNamed(root.transform, "Locomotion");
                if (locomotion != null) locomotion.gameObject.SetActive(false);
                Transform gaze = FindNamed(root.transform, "Gaze Interactor");
                if (gaze != null) gaze.gameObject.SetActive(false);
                VRPlayerSetup rig = root.AddComponent<VRPlayerSetup>();
                Camera camera = root.GetComponentInChildren<Camera>(true);
                rig.headset = camera != null ? camera.transform : null;
                rig.leftController = FindNamed(root.transform, "Left Controller");
                rig.rightController = FindNamed(root.transform, "Right Controller");
                if (rig.headset == null || rig.leftController == null || rig.rightController == null)
                    throw new InvalidOperationException("Starter rig is missing Main Camera or controller transforms.");
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 100;
                Material hand = MakeMaterial("PracticeHands", new Color(0.9f, 0.72f, 0.52f));
                CreateHand(rig.leftController, hand);
                CreateHand(rig.rightController, hand);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static Transform FindNamed(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true).FirstOrDefault(item => item.name == name);
        }

        static void CreateHand(Transform controller, Material material)
        {
            Transform hand = Point(controller, "PracticeHand", new Vector3(0, -0.015f, 0.025f));
            Shape(PrimitiveType.Sphere, hand, "Palm", Vector3.zero,
                new Vector3(0.065f, 0.045f, 0.085f), material);
            for (int i = 0; i < 4; i++)
                Shape(PrimitiveType.Sphere, hand, "Finger " + i,
                    new Vector3(-0.023f + i * 0.015f, -0.005f, 0.045f),
                    new Vector3(0.013f, 0.025f, 0.045f), material);
            Shape(PrimitiveType.Sphere, hand, "Thumb", new Vector3(0.033f, 0, 0.015f),
                new Vector3(0.025f, 0.025f, 0.04f), material);
        }

        static void CreatePracticeUI()
        {
            string path = "Assets/UI/PracticeUI.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject root = new GameObject("PracticeUI", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(TrackedDeviceGraphicRaycaster));
            try
            {
                Canvas canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                RectTransform rect = root.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(800, 650);
                root.transform.localScale = Vector3.one * 0.0015f;
                PracticeManager practice = root.AddComponent<PracticeManager>();
                TutorialManager tutorial = root.AddComponent<TutorialManager>();
                tutorial.practice = practice;
                PracticeUI ui = root.AddComponent<PracticeUI>();
                ui.practice = practice;
                ui.tutorial = tutorial;

                GameObject background = Rect("Background", root.transform, new Vector2(800, 650), Vector2.zero);
                background.AddComponent<Image>().color = new Color(0.035f, 0.06f, 0.075f, 0.97f);
                Label(root.transform, "Heading", "VIRTUAL ARCHERY | PRACTICE", new Vector2(720, 55), new Vector2(0, 265), 32);
                ui.statusText = Label(root.transform, "Status", "Ready to practise", new Vector2(720, 65), new Vector2(0, 195), 24);
                ui.statsText = Label(root.transform, "Statistics", "Shots: 0    Hits: 0    Misses: 0\nAccuracy: 0%    In flight: 0",
                    new Vector2(720, 80), new Vector2(0, 110), 25);
                ui.instructionText = Label(root.transform, "Instructions", "Choose Start Practice.",
                    new Vector2(720, 130), new Vector2(0, -5), 26);
                Label(root.transform, "DrawLabel", "DRAW STRENGTH", new Vector2(720, 35), new Vector2(0, -110), 20);
                GameObject meter = Rect("DrawStrength", root.transform, new Vector2(680, 22), new Vector2(0, -145));
                meter.AddComponent<Image>().color = new Color(0.15f, 0.2f, 0.23f);
                GameObject fill = Rect("Fill", meter.transform, new Vector2(680, 22), Vector2.zero);
                fill.AddComponent<Image>().color = new Color(0.15f, 0.85f, 0.65f);
                RectTransform fillRect = fill.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.sizeDelta = Vector2.zero;
                Slider slider = meter.AddComponent<Slider>();
                slider.interactable = false;
                slider.fillRect = fill.GetComponent<RectTransform>();
                slider.direction = Slider.Direction.LeftToRight;
                slider.minValue = 0;
                slider.maxValue = 1;
                slider.value = 0;
                ui.drawMeter = slider;
                ui.startButton = MakeButton(root.transform, "Start Practice", new Vector2(0, -215));
                ui.endButton = MakeButton(root.transform, "End Practice", new Vector2(0, -215));
                ui.endButton.gameObject.SetActive(false);
                ui.desktopHelp = Label(root.transform, "PreviewControls", "Editor: P start | hold Space, release to shoot | E end\nRight mouse + move to aim",
                    new Vector2(720, 65), new Vector2(0, -285), 19).gameObject;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static GameObject Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            GameObject root = new GameObject(name, typeof(RectTransform));
            RectTransform rect = root.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return root;
        }

        static Text Label(Transform parent, string name, string value, Vector2 size, Vector2 position, int fontSize)
        {
            GameObject root = Rect(name, parent, size, position);
            Text label = root.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.text = value;
            label.color = new Color(0.93f, 0.97f, 0.98f);
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        static Button MakeButton(Transform parent, string name, Vector2 position)
        {
            GameObject root = Rect(name, parent, new Vector2(420, 64), position);
            Image image = root.AddComponent<Image>();
            image.color = new Color(0.05f, 0.5f, 0.38f);
            Button button = root.AddComponent<Button>();
            button.targetGraphic = image;
            Label(root.transform, "Label", name, new Vector2(400, 60), Vector2.zero, 27);
            return button;
        }

        static void CreateTestScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "VRPlayer.prefab"));
            GameObject bowObject = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "Bow.prefab"));
            bowObject.transform.position = new Vector3(0, 1.15f, 0.55f);
            BowController bow = bowObject.GetComponent<BowController>();
            GameObject panel = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/PracticeUI.prefab"));
            panel.transform.position = new Vector3(-1.05f, 1.5f, 1.65f);
            panel.transform.rotation = Quaternion.Euler(0, -30f, 0);
            PracticeManager practice = panel.GetComponent<PracticeManager>();
            practice.bow = bow;
            practice.autoStart = true;
            panel.GetComponent<TutorialManager>().bow = bow;
            Camera camera = player.GetComponentInChildren<Camera>(true);
            panel.GetComponent<Canvas>().worldCamera = camera;
            DesktopPracticePreview preview = new GameObject("Editor Preview Controls").AddComponent<DesktopPracticePreview>();
            preview.bow = bow;
            preview.practice = practice;
            preview.headset = camera.transform;

            Material floor = MakeMaterial("TestFloor", new Color(0.16f, 0.24f, 0.18f));
            GameObject ground = Shape(PrimitiveType.Cube, null, "Test Ground", new Vector3(0, -0.1f, 4),
                new Vector3(12, 0.2f, 16), floor);
            ground.AddComponent<BoxCollider>();
            Material marker = MakeMaterial("ShootingLine", new Color(0.85f, 0.85f, 0.5f));
            Shape(PrimitiveType.Cube, null, "Shooting Line", new Vector3(0, 0.006f, 0.6f), new Vector3(2, 0.01f, 0.035f), marker);
            CreateTestTarget(new Vector3(0, 1.5f, 6));
            Light light = new GameObject("Test Sun").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(45, -30, 0);
            RenderSettings.ambientLight = new Color(0.45f, 0.5f, 0.55f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.38f, 0.58f, 0.7f);
            foreach (UnityEngine.Object changed in new UnityEngine.Object[] {
                bowObject.transform, panel.transform, practice, panel.GetComponent<TutorialManager>(),
                panel.GetComponent<Canvas>(), camera })
                PrefabUtility.RecordPrefabInstancePropertyModifications(changed);
            EditorSceneManager.SaveScene(scene, TestScene);
            Selection.activeGameObject = panel;
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0, 1, 3), new Vector3(5, 4, 8)), false);
        }

        static void CreateTestTarget(Vector3 position)
        {
            GameObject root = new GameObject("Developer1 Test Target");
            root.transform.position = position;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.3f, 1.3f, 0.15f);
            PracticeTestTarget target = root.AddComponent<PracticeTestTarget>();
            Color[] colours = { new Color(0.92f, 0.92f, 0.85f), new Color(0.12f, 0.4f, 0.8f),
                new Color(0.8f, 0.12f, 0.1f), new Color(1f, 0.73f, 0.1f) };
            for (int i = 0; i < colours.Length; i++)
            {
                float size = 1.2f - i * 0.29f;
                GameObject disc = Shape(PrimitiveType.Cylinder, root.transform, "Ring " + i,
                    new Vector3(0, 0, -0.06f - i * 0.008f), new Vector3(size, 0.008f, size),
                    MakeMaterial("TestTargetRing" + i, colours[i]));
                disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
                if (i == 0) target.feedbackRenderer = disc.GetComponent<Renderer>();
            }
            target.impactAudio = root.AddComponent<AudioSource>();
            target.impactAudio.playOnAwake = false;
            target.impactAudio.spatialBlend = 1;
            target.impactAudio.volume = 0.6f;
            target.impactAudio.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Developer1/TestHit.wav");
        }

        [MenuItem("Archery/Developer 1/Validate Current Scene")]
        public static void ValidateScene()
        {
            int problems = 0;
            VRPlayerSetup rig = UnityEngine.Object.FindFirstObjectByType<VRPlayerSetup>();
            BowController bow = UnityEngine.Object.FindFirstObjectByType<BowController>();
            PracticeManager practice = UnityEngine.Object.FindFirstObjectByType<PracticeManager>();
            if (rig == null || rig.headset == null || rig.leftController == null || rig.rightController == null)
            { Debug.LogError("VRPlayer or its camera/controller references are missing."); problems++; }
            if (bow == null || bow.arrowPrefab == null || bow.restNock == null || bow.bowstring == null)
            { Debug.LogError("Bow or one of its prefab references is missing."); problems++; }
            if (practice == null || practice.bow == null)
            { Debug.LogError("PracticeUI/PracticeManager needs the scene's Bow reference."); problems++; }
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) > 0)
                    { Debug.LogError("Missing script on " + item.name, item); problems++; }
            if (problems == 0) Debug.Log("Developer 1 scene references passed validation.");
        }
    }
}
