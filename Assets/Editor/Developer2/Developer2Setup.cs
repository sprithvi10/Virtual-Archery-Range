using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;
using VirtualArchery;

namespace VirtualArcheryEditor
{
    public static class Developer2Setup
    {
        const string ScenePath = "Assets/Scenes/ArcheryRange.unity";
        const string TargetPath = "Assets/Prefabs/Target.prefab";
        const string UIPath = "Assets/UI/GameUI.prefab";
        const string ResultsPath = "Assets/UI/ResultsUI.prefab";
        const string ReticlePath = "Assets/UI/DesktopAimReticle.prefab";
        const string Materials = "Assets/Materials/Developer2/";
        static readonly Color Ink = new Color(.035f, .065f, .09f, 1f);

        [MenuItem("Archery/Developer 2/Create Range and Game Assets")]
        public static void CreateAssetsAndScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before generating range assets.");
            foreach (string path in new[] { "Assets/Prefabs/VRPlayer.prefab", "Assets/Prefabs/Bow.prefab" })
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                    throw new InvalidOperationException("Run Developer 1 setup first. Missing " + path);
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string directory in new[] { "Assets/Scenes", "Assets/Prefabs", "Assets/UI", Materials }) Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
            CreateTarget();
            CreateGameUI();
            CreateResultsUI();
            CreateDesktopAimReticle();
            AssetDatabase.SaveAssets();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                var scene = EditorSceneManager.OpenScene(ScenePath);
                ApplyRangePresentation(scene);
                var ui = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameUI>(true)).Single();
                EnsurePracticeControls(ui);
                RecordOverride(ui);
                ApplyPracticeResults(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            else
                CreateScene();
            AssetDatabase.SaveAssets();
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            scenes.AddRange(EditorBuildSettings.scenes.Where(scene => scene.path != ScenePath));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("Developer 2 assets and ArcheryRange are ready. Existing assets were preserved.");
        }

        static Material Material(string name, Color color, bool unlit = false)
        {
            string path = Materials + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("No supported shader available.");
            var material = new Material(shader) { name = name, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static GameObject Shape(PrimitiveType type, Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool solid = false)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) UnityEngine.Object.DestroyImmediate(item.GetComponent<Collider>());
            return item;
        }

        static void CreateTarget()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(TargetPath) != null) return;
            var root = new GameObject("Target");
            try
            {
                var target = root.AddComponent<Target>();
                target.radius = .65f;
                Color[] colors = { new Color(.94f, .94f, .89f), new Color(.025f, .03f, .035f), new Color(.08f, .42f, .85f), new Color(.86f, .07f, .09f), new Color(1f, .74f, .06f) };
                string[] names = { "White", "Black", "Blue", "Red", "Gold" };
                for (int i = 0; i < 5; i++)
                {
                    float diameter = 1.3f * (5 - i) / 5f;
                    var disk = Shape(PrimitiveType.Cylinder, root.transform, names[i] + " scoring ring", new Vector3(0, 0, -.009f - i * .002f), new Vector3(diameter, .004f, diameter), Material("Target" + names[i], colors[i]));
                    disk.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    if (i == 0) target.feedbackRenderer = disk.GetComponent<Renderer>();
                }
                // A saved closed circular mesh is the physical face; no rectangular corner hits.
                const string meshPath = "Assets/Prefabs/TargetFace.asset";
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (mesh == null)
                {
                    const int segments = 256;
                    var vertices = new Vector3[segments * 2 + 2];
                    var triangles = new int[segments * 12];
                    vertices[segments * 2] = new Vector3(0, 0, -.021f);
                    vertices[segments * 2 + 1] = new Vector3(0, 0, .035f);
                    for (int i = 0; i < segments; i++)
                    {
                        float angle = i * Mathf.PI * 2 / segments;
                        vertices[i] = new Vector3(Mathf.Cos(angle) * .65f, Mathf.Sin(angle) * .65f, -.021f);
                        vertices[i + segments] = new Vector3(vertices[i].x, vertices[i].y, .035f);
                        int next = (i + 1) % segments;
                        int offset = i * 12;
                        int[] face = { segments * 2, next, i, segments * 2 + 1, i + segments, next + segments, i, next, next + segments, i, next + segments, i + segments };
                        Array.Copy(face, 0, triangles, offset, 12);
                    }
                    mesh = new Mesh { name = "Circular Target Face", vertices = vertices, triangles = triangles };
                    mesh.RecalculateNormals();
                    mesh.RecalculateBounds();
                    AssetDatabase.CreateAsset(mesh, meshPath);
                }
                root.AddComponent<MeshCollider>().sharedMesh = mesh;
                var wood = Material("TargetStand", new Color(.28f, .19f, .11f));
                Shape(PrimitiveType.Cube, root.transform, "Stand", new Vector3(0, -.83f, .13f), new Vector3(.09f, 1.32f, .12f), wood);
                Shape(PrimitiveType.Cube, root.transform, "Feet", new Vector3(0, -1.43f, .13f), new Vector3(.85f, .1f, .55f), wood);
                PrefabUtility.SaveAsPrefabAsset(root, TargetPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static GameObject Rect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            var item = new GameObject(name, typeof(RectTransform));
            var rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return item;
        }

        static Text Label(Transform parent, string name, string value, Vector2 size, Vector2 position, int fontSize)
        {
            var text = Rect(name, parent, size, position).AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.text = value;
            text.color = new Color(.94f, .97f, 1f);
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }


        static GameObject CanvasRoot(string name)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(TrackedDeviceGraphicRaycaster), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            root.GetComponent<Canvas>().sortingOrder = 10;
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 700);
            root.transform.localScale = Vector3.one * .0015f;
            return root;
        }

        static GameObject Panel(Transform parent, string name)
        {
            var panel = Rect(name, parent, new Vector2(1000, 700), Vector2.zero);
            panel.AddComponent<Image>().color = Ink;
            return panel;
        }

        static Button Button(Transform parent, string name, Vector2 position, float width = 700)
        {
            var item = Rect(name, parent, new Vector2(width, 76), position);
            var image = item.AddComponent<Image>();
            image.color = new Color(.055f, .42f, .35f);
            var button = item.AddComponent<Button>();
            button.targetGraphic = image;
            Label(item.transform, "Label", name, new Vector2(width - 30, 70), Vector2.zero, 29);
            return button;
        }

        static void EnsurePracticeControls(GameUI ui)
        {
            if (ui == null || ui.gameplayPanel == null)
                throw new InvalidOperationException("GameUI needs its existing session panel.");
            if (PrefabUtility.IsPartOfPrefabInstance(ui.gameObject))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(ui.gameObject),
                    PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            if (ui.practiceDistanceSlider != null)
                UnityEngine.Object.DestroyImmediate(ui.practiceDistanceSlider.gameObject);
            var oldLabel = ui.gameplayPanel.transform.Find("Practice Distance");
            if (oldLabel != null) UnityEngine.Object.DestroyImmediate(oldLabel.gameObject);
            ui.practiceDistanceText = Label(ui.gameplayPanel.transform, "Practice Distance", "Target distance · 6 m",
                new Vector2(900, 42), new Vector2(0, -74), 26);
            var sliderRoot = Rect("Practice Distance Slider", ui.gameplayPanel.transform, new Vector2(820, 38), new Vector2(0, -120));
            var track = sliderRoot.AddComponent<Image>();
            track.color = new Color(.17f, .23f, .28f);
            var handle = Rect("Handle", sliderRoot.transform, new Vector2(38, 52), Vector2.zero);
            var handleImage = handle.AddComponent<Image>();
            handleImage.color = new Color(.2f, .85f, .62f);
            var slider = sliderRoot.AddComponent<Slider>();
            slider.targetGraphic = handleImage;
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.minValue = 6;
            slider.maxValue = 12;
            slider.wholeNumbers = false;
            slider.value = 6;
            ui.practiceDistanceSlider = slider;
            Label(sliderRoot.transform, "Minimum", "6 m", new Vector2(120, 35), new Vector2(-380, -42), 22);
            Label(sliderRoot.transform, "Maximum", "12 m", new Vector2(120, 35), new Vector2(380, -42), 22);
            ui.scoreText.rectTransform.sizeDelta = new Vector2(920, 110);
            ui.scoreText.rectTransform.anchoredPosition = new Vector2(0, 130);
            ui.feedbackText.rectTransform.anchoredPosition = new Vector2(0, 45);
            ui.drawMeter.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -6);
            ui.helpText.rectTransform.sizeDelta = new Vector2(920, 65);
            ui.helpText.rectTransform.anchoredPosition = new Vector2(0, -207);
            ui.helpText.fontSize = 20;
            ui.endPracticeButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-225, -300);
            ui.menuButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(225, -300);
            foreach (var button in new[] { ui.endPracticeButton, ui.menuButton })
            {
                button.GetComponent<RectTransform>().sizeDelta = new Vector2(430, 70);
                var label = button.GetComponentInChildren<Text>(true);
                label.rectTransform.sizeDelta = new Vector2(410, 65);
                label.fontSize = 26;
            }
        }

        static Dropdown DistanceDropdown(Transform parent)
        {
            var root = Rect("Recorded Distances", parent, new Vector2(650, 62), new Vector2(-410, -345));
            var image = root.AddComponent<Image>();
            image.color = new Color(.055f, .42f, .35f);
            var dropdown = root.AddComponent<Dropdown>();
            dropdown.targetGraphic = image;
            dropdown.captionText = Label(root.transform, "Selected Distance", "6 m · 0 impacts  ▾", new Vector2(610, 58), Vector2.zero, 26);
            var template = Rect("Template", root.transform, new Vector2(650, 260), new Vector2(0, -162));
            template.AddComponent<Image>().color = Ink;
            var scroll = template.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 35;
            var viewport = Rect("Viewport", template.transform, new Vector2(630, 250), Vector2.zero);
            viewport.AddComponent<Image>().color = Color.white;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var content = Rect("Content", viewport.transform, new Vector2(630, 54), Vector2.zero);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = Vector2.one;
            contentRect.pivot = new Vector2(.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 54);
            var item = Rect("Item", content.transform, new Vector2(630, 54), Vector2.zero);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0, .5f);
            itemRect.anchorMax = new Vector2(1, .5f);
            itemRect.sizeDelta = new Vector2(0, 54);
            var itemImage = item.AddComponent<Image>();
            itemImage.color = new Color(.08f, .24f, .25f);
            var toggle = item.AddComponent<Toggle>();
            toggle.targetGraphic = itemImage;
            var check = Rect("Selected", item.transform, new Vector2(12, 38), new Vector2(-300, 0)).AddComponent<Image>();
            check.color = new Color(.2f, .85f, .62f);
            toggle.graphic = check;
            dropdown.itemText = Label(item.transform, "Distance", "6 m · 0 impacts", new Vector2(570, 52), Vector2.zero, 25);
            scroll.viewport = viewport.GetComponent<RectTransform>();
            scroll.content = contentRect;
            dropdown.template = template.GetComponent<RectTransform>();
            template.SetActive(false);
            return dropdown;
        }

        [MenuItem("Archery/Developer 2/Upgrade Practice Mode")]
        public static void UpgradePracticeMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before upgrading Practice mode.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Create ArcheryRange before upgrading Practice mode.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CreateGameUI();
            CreateResultsUI();
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var game = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameManager>(true)).Single();
            MigratePracticeScene(scene, game);
            var ui = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<GameUI>(true)).Single();
            EnsurePracticeControls(ui);
            ui.game = game;
            RecordOverride(ui);
            ApplyPracticeResults(scene);
            ReplacePracticeScenery(scene, game.difficulty);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Practice mode upgraded: one central target, right distance slider and scrollable distance results. Competitive rules and panel preserved.");
        }

        static void ReplacePracticeScenery(Scene scene, DifficultyManager difficulty)
        {
            var oldEnvironment = scene.GetRootGameObjects().Where(root => root.name == "Range Environment").ToArray();
            var environment = RangePresentationArt.Create(out var practiceDecor, out var competitiveDecor);
            if (environment.scene != scene) SceneManager.MoveGameObjectToScene(environment, scene);
            foreach (var oldRoot in oldEnvironment) UnityEngine.Object.DestroyImmediate(oldRoot);
            difficulty.practiceDecor = practiceDecor;
            difficulty.competitiveDecor = competitiveDecor;
            RecordOverride(difficulty);
        }

        static void MigratePracticeScene(Scene scene, GameManager game)
        {
            var targets = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Target>(true)).ToArray();
            var target = game.difficulty.target;
            if (target == null)
                target = targets.OrderBy(item => Mathf.Abs(item.transform.position.x)).FirstOrDefault();
            if (target == null) throw new InvalidOperationException("Practice upgrade needs an existing central target.");
            game.difficulty.target = target;
            foreach (var obsolete in targets)
                if (obsolete != target) UnityEngine.Object.DestroyImmediate(obsolete.gameObject);
            target.transform.position = new Vector3(0, target.transform.position.y, game.difficulty.PracticeDistance);
            target.gameObject.SetActive(true);
            RecordOverride(target.transform);
            RecordOverride(target.gameObject);
            RecordOverride(game.difficulty);
            var practice = game.practice;
            if (practice == null) throw new InvalidOperationException("Practice upgrade needs the existing PracticeManager.");
            var tutorial = practice.GetComponent<TutorialManager>();
            if (tutorial == null) throw new InvalidOperationException("Practice upgrade needs the existing TutorialManager.");
            // Unpack the scene instance only: never alter Developer 1's standalone assets.
            if (PrefabUtility.IsPartOfPrefabInstance(practice.gameObject))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(practice.gameObject),
                    PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            var oldUI = practice.GetComponent<PracticeUI>();
            if (oldUI != null) UnityEngine.Object.DestroyImmediate(oldUI);
            foreach (Transform child in practice.transform.Cast<Transform>().ToArray())
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            foreach (var raycaster in practice.GetComponents<BaseRaycaster>())
                UnityEngine.Object.DestroyImmediate(raycaster);
            var scaler = practice.GetComponent<CanvasScaler>();
            if (scaler != null) UnityEngine.Object.DestroyImmediate(scaler);
            var canvas = practice.GetComponent<Canvas>();
            if (canvas != null) UnityEngine.Object.DestroyImmediate(canvas);
            practice.gameObject.name = "Practice Gameplay";
            practice.gameObject.SetActive(true);
            practice.bow = game.bow;
            practice.autoStart = false;
            tutorial.bow = game.bow;
            tutorial.practice = practice;
            RecordOverride(practice);
            RecordOverride(tutorial);
        }

        static void CreateGameUI()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(UIPath) != null)
            {
                var existing = PrefabUtility.LoadPrefabContents(UIPath);
                try
                {
                    EnsurePracticeControls(existing.GetComponent<GameUI>());
                    PrefabUtility.SaveAsPrefabAsset(existing, UIPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(existing); }
                return;
            }
            var root = CanvasRoot("GameUI");
            try
            {
                var ui = root.AddComponent<GameUI>();
                ui.menuPanel = Panel(root.transform, "Main Menu");
                ui.menuPanel.transform.localScale = Vector3.one * 1.2f;
                Label(ui.menuPanel.transform, "Brand", "VIRTUAL ARCHERY", new Vector2(920, 90), new Vector2(0, 260), 49);
                ui.rulesText = Label(ui.menuPanel.transform, "Rules", "", new Vector2(900, 260), new Vector2(0, 70), 28);
                ui.practiceButton = Button(ui.menuPanel.transform, "Start Practice", new Vector2(0, -135));
                ui.competitiveButton = Button(ui.menuPanel.transform, "Start Competitive", new Vector2(0, -235));
                Label(ui.menuPanel.transform, "Menu Help", "Controller ray: choose a mode | Editor: 1 Practice, 2 Competitive",
                    new Vector2(920, 45), new Vector2(0, -310), 20);
                ui.gameplayPanel = Panel(root.transform, "Live Session");
                // The menu is central; the live panel is wholly to the right of the aiming lane.
                ui.gameplayPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(1050, -80);
                ui.gameplayPanel.transform.localRotation = Quaternion.Euler(0, 32, 0);
                ui.titleText = Label(ui.gameplayPanel.transform, "Title", "SESSION", new Vector2(920, 65), new Vector2(0, 285), 36);
                ui.statusText = Label(ui.gameplayPanel.transform, "Status", "", new Vector2(920, 65), new Vector2(0, 220), 26);
                ui.scoreText = Label(ui.gameplayPanel.transform, "Score", "", new Vector2(920, 155), new Vector2(0, 100), 28);
                ui.feedbackText = Label(ui.gameplayPanel.transform, "Feedback", "Aim at the gold centre", new Vector2(920, 65), new Vector2(0, -5), 28);
                ui.helpText = Label(ui.gameplayPanel.transform, "Help", "Hold Space and release to shoot · Right mouse to aim\nP practice · C competitive · E end · Esc menu", new Vector2(920, 90), new Vector2(0, -115), 23);
                var meter = Rect("Draw Strength", ui.gameplayPanel.transform, new Vector2(850, 22), new Vector2(0, -65));
                meter.AddComponent<Image>().color = new Color(.17f, .23f, .28f);
                var fill = Rect("Fill", meter.transform, new Vector2(850, 22), Vector2.zero);
                fill.AddComponent<Image>().color = new Color(.2f, .85f, .62f);
                var fillRect = fill.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.sizeDelta = Vector2.zero;
                ui.drawMeter = meter.AddComponent<Slider>();
                ui.drawMeter.interactable = false;
                ui.drawMeter.fillRect = fillRect;
                ui.drawMeter.minValue = 0;
                ui.drawMeter.maxValue = 1;
                ui.endPracticeButton = Button(ui.gameplayPanel.transform, "End Practice", new Vector2(0, -220));
                ui.menuButton = Button(ui.gameplayPanel.transform, "Return to Menu", new Vector2(0, -305));
                ui.gameplayPanel.SetActive(false);
                EnsurePracticeControls(ui);
                PrefabUtility.SaveAsPrefabAsset(root, UIPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void CreateResultsUI()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ResultsPath) != null)
            {
                var existing = PrefabUtility.LoadPrefabContents(ResultsPath);
                try
                {
                    var ui = existing.GetComponent<ResultsUI>();
                    if (ui == null) throw new InvalidOperationException("ResultsUI prefab is missing its ResultsUI component.");
                    EnsurePracticeResults(ui);
                    PrefabUtility.SaveAsPrefabAsset(existing, ResultsPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(existing); }
                return;
            }
            var root = CanvasRoot("ResultsUI");
            try
            {
                var ui = root.AddComponent<ResultsUI>();
                ui.panel = Panel(root.transform, "Results Panel");
                ui.panel.transform.localScale = Vector3.one * 1.2f;
                Label(ui.panel.transform, "Heading", "SESSION RESULTS", new Vector2(920, 70), new Vector2(0, 280), 42);
                ui.outcomeText = Label(ui.panel.transform, "Outcome", "Session Complete", new Vector2(920, 90), new Vector2(0, 180), 38);
                ui.statsText = Label(ui.panel.transform, "Statistics", "Shots 0\nHits 0 · Misses 0\nPoints 0 · Accuracy 0%", new Vector2(900, 230), new Vector2(0, 15), 32);
                ui.retryButton = Button(ui.panel.transform, "Try Again", new Vector2(0, -170));
                ui.menuButton = Button(ui.panel.transform, "Return to Menu", new Vector2(0, -265));
                ui.panel.SetActive(false);
                EnsurePracticeResults(ui);
                PrefabUtility.SaveAsPrefabAsset(root, ResultsPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        static void EnsurePracticeResults(ResultsUI ui)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(ui.gameObject))
                PrefabUtility.UnpackPrefabInstance(PrefabUtility.GetOutermostPrefabInstanceRoot(ui.gameObject),
                    PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            foreach (var oldPanel in ui.GetComponentsInChildren<PracticeResultsUI>(true))
                UnityEngine.Object.DestroyImmediate(oldPanel.gameObject);

            var panel = Rect("Practice Results", ui.transform, new Vector2(1600, 960), Vector2.zero);
            panel.transform.localScale = Vector3.one * 1.2f;
            panel.AddComponent<Image>().color = Ink;
            // Keep this independent of the original Competitive panel and all its references.
            panel.SetActive(false);
            var practice = panel.AddComponent<PracticeResultsUI>();
            ui.practiceResults = practice;
            Label(panel.transform, "Heading", "PRACTICE RESULTS", new Vector2(1500, 70), new Vector2(0, 410), 42);
            Label(panel.transform, "Caption", "Shot grouping by distance · plotted target impacts only",
                new Vector2(1500, 48), new Vector2(0, 350), 26);

            practice.distanceText = Label(panel.transform, "Distance", "6 m · 0 impacts",
                new Vector2(650, 60), new Vector2(-410, 280), 30);
            practice.diagram = Rect("Shot Grouping", panel.transform, new Vector2(440, 440), new Vector2(-410, 5))
                .AddComponent<ShotGroupingGraphic>();
            practice.diagram.raycastTarget = false;
            Label(panel.transform, "Diagram Legend", "White dots: impacts   +: mean impact\nCircle: RMS spread (3+ impacts)",
                new Vector2(700, 75), new Vector2(-410, -265), 23);
            practice.distanceDropdown = DistanceDropdown(panel.transform);

            practice.statsText = Label(panel.transform, "Practice Statistics", "",
                new Vector2(690, 205), new Vector2(375, 205), 30);
            practice.analysisText = Label(panel.transform, "Grouping Analysis", "",
                new Vector2(690, 270), new Vector2(375, -45), 27);
            practice.unplottedText = Label(panel.transform, "Unplotted Shots", "",
                new Vector2(690, 125), new Vector2(375, -260), 24);
            practice.retryButton = Button(panel.transform, "Try Again", new Vector2(-330, -430), 560);
            practice.menuButton = Button(panel.transform, "Return to Menu", new Vector2(330, -430), 560);
        }

        [MenuItem("Archery/Developer 2/Upgrade Practice Results")]
        public static void UpgradePracticeResults()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before upgrading Practice results.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null ||
                AssetDatabase.LoadAssetAtPath<GameObject>(ResultsPath) == null)
                throw new InvalidOperationException("Create ArcheryRange and ResultsUI before upgrading Practice results.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            CreateResultsUI();
            AssetDatabase.SaveAssets();
            // Reload the saved scene, rather than saving edits declined in the prompt above.
            var scene = EditorSceneManager.OpenScene(ScenePath);
            ApplyPracticeResults(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Practice results upgraded. The original Competitive panel, existing prefab GUID and range objects were preserved.");
        }

        static void ApplyPracticeResults(Scene scene)
        {
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true))
                .Where(component => component != null).ToArray();
            var uis = components.OfType<ResultsUI>().ToArray();
            var games = components.OfType<GameManager>().ToArray();
            if (uis.Length != 1 || games.Length != 1 || games[0].results == null)
                throw new InvalidOperationException("Practice results upgrade requires one existing ResultsUI and one GameManager with ResultsManager.");

            var ui = uis[0];
            EnsurePracticeResults(ui);
            ui.game = games[0];
            ui.results = games[0].results;
            var canvas = ui.GetComponent<Canvas>();
            if (canvas == null || ui.GetComponent<TrackedDeviceGraphicRaycaster>() == null ||
                ui.GetComponent<GraphicRaycaster>() == null)
                throw new InvalidOperationException("ResultsUI requires its existing Canvas, tracked-device raycaster and mouse graphic raycaster.");
            if (canvas.worldCamera == null)
            {
                var rig = components.OfType<VRPlayerSetup>().SingleOrDefault();
                canvas.worldCamera = rig != null ? rig.GetComponentInChildren<Camera>(true) : null;
                if (canvas.worldCamera == null)
                    throw new InvalidOperationException("Practice results upgrade requires the existing player camera.");
            }
            // Explicitly persist the scene's panel and manager bindings, including prefab-instance overrides.
            RecordOverride(ui);
            RecordOverride(canvas);
            RecordOverride(ui.practiceResults);
            RecordOverride(ui.practiceResults.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static GameObject Instance(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing prefab " + path);
            return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }

        static void CreateScene()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SceneManager.SetActiveScene(scene);
            var rig = Instance("Assets/Prefabs/VRPlayer.prefab");
            var camera = rig.GetComponentInChildren<Camera>(true);
            if (camera == null) throw new InvalidOperationException("VRPlayer needs a camera.");
            var bow = Instance("Assets/Prefabs/Bow.prefab").GetComponent<BowController>();
            bow.transform.position = new Vector3(0, 1.15f, .55f);
            var practiceObject = new GameObject("Practice Gameplay");
            var practice = practiceObject.AddComponent<PracticeManager>();
            practice.bow = bow;
            practice.autoStart = false;
            var tutorial = practiceObject.AddComponent<TutorialManager>();
            tutorial.bow = bow;
            tutorial.practice = practice;

            var managers = new GameObject("Range Managers");
            var score = managers.AddComponent<ScoreManager>();
            var difficulty = managers.AddComponent<DifficultyManager>();
            var challenge = managers.AddComponent<ChallengeManager>();
            var results = managers.AddComponent<ResultsManager>();
            var game = managers.AddComponent<GameManager>();
            score.bow = bow;
            difficulty.target = Instance(TargetPath).GetComponent<Target>();
            difficulty.target.name = "Target";
            difficulty.target.transform.position = new Vector3(0, 1.5f, 6);
            difficulty.hitsToAdvance = 3;
            difficulty.minimumQualifyingPoints = 6;
            difficulty.transitionDuration = 1;
            difficulty.distances = new[] { 6f, 9f, 12f };
            challenge.bow = bow;
            challenge.score = score;
            challenge.difficulty = difficulty;
            challenge.roundDuration = 60;
            challenge.arrowLimit = 12;
            challenge.winningScore = 50;
            game.bow = bow;
            game.practice = practice;
            game.score = score;
            game.challenge = challenge;
            game.difficulty = difficulty;
            game.results = results;

            var ui = Instance(UIPath).GetComponent<GameUI>();
            ui.transform.position = new Vector3(0, 1.65f, 2);
            ui.game = game;
            ui.GetComponent<Canvas>().worldCamera = camera;
            var resultsUI = Instance(ResultsPath).GetComponent<ResultsUI>();
            resultsUI.transform.position = new Vector3(0, 1.65f, 2);
            resultsUI.game = game;
            resultsUI.results = results;
            resultsUI.GetComponent<Canvas>().worldCamera = camera;

            var preview = new GameObject("Desktop Preview").AddComponent<DesktopPracticePreview>();
            preview.bow = bow;
            preview.practice = null;
            preview.headset = camera.transform;
            preview.gameObject.AddComponent<DesktopRangeControls>().game = game;
            var events = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EventSystem>(true)).ToArray();
            if (events.Length == 0) new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
            else
            {
                if (events.Length != 1) throw new InvalidOperationException("VRPlayer contains multiple event systems.");
                if (events[0].GetComponent<XRUIInputModule>() == null) events[0].gameObject.AddComponent<XRUIInputModule>();
                foreach (var module in events[0].GetComponents<BaseInputModule>())
                    if (!(module is XRUIInputModule)) UnityEngine.Object.DestroyImmediate(module);
            }
            ApplyRangePresentation(scene);
            // Record every instance override, including nested UI button active states.
            foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                    if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var item in root.GetComponentsInChildren<Transform>(true))
                    if (PrefabUtility.IsPartOfPrefabInstance(item.gameObject)) PrefabUtility.RecordPrefabInstancePropertyModifications(item.gameObject);
            EditorSceneManager.SaveScene(scene, ScenePath);
            SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0, 1.5f, 6), new Vector3(10, 5, 18)), false);
        }

        [MenuItem("Archery/Developer 2/Upgrade Range Presentation")]
        public static void UpgradeRangePresentation()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before upgrading the range presentation.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
                throw new InvalidOperationException("Create ArcheryRange before upgrading its presentation.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory("Assets/UI");
            AssetDatabase.Refresh();
            // Reopen even when already active so declining the save prompt never silently saves prior edits.
            var scene = EditorSceneManager.OpenScene(ScenePath);
            ApplyRangePresentation(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Woodland range presentation upgraded. Gameplay objects and existing prefab assets were preserved.");
        }

        static void ApplyRangePresentation(Scene scene)
        {
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true))
                .Where(component => component != null).ToArray();
            var games = components.OfType<GameManager>().ToArray();
            if (games.Length != 1 || games[0].difficulty == null || games[0].bow == null)
                throw new InvalidOperationException("Presentation upgrade requires one wired GameManager with difficulty and bow.");
            var game = games[0];
            var difficulty = game.difficulty;
            MigratePracticeScene(scene, game);
            var rig = components.OfType<VRPlayerSetup>().SingleOrDefault();
            var camera = rig != null ? rig.GetComponentInChildren<Camera>(true) : null;
            if (camera == null) throw new InvalidOperationException("Presentation upgrade requires the existing player camera.");
            var reticles = components.OfType<DesktopAimReticle>().ToArray();
            if (reticles.Length > 1) throw new InvalidOperationException("Presentation upgrade found multiple aim reticles.");
            CreateDesktopAimReticle();

            // Build successfully before replacing only the owned generated environment.
            var oldEnvironment = scene.GetRootGameObjects().Where(root => root.name == "Range Environment").ToArray();
            var environment = RangePresentationArt.Create(out var practiceDecor, out var competitiveDecor);
            if (environment.scene != scene) SceneManager.MoveGameObjectToScene(environment, scene);
            foreach (var oldRoot in oldEnvironment) UnityEngine.Object.DestroyImmediate(oldRoot);
            difficulty.practiceDecor = practiceDecor;
            difficulty.competitiveDecor = competitiveDecor;
            difficulty.hitsToAdvance = 3;
            difficulty.minimumQualifyingPoints = 6;
            difficulty.transitionDuration = 1;
            difficulty.distances = new[] { 6f, 9f, 12f };
            difficulty.target.transform.position = new Vector3(0, difficulty.target.transform.position.y, difficulty.PracticeDistance);
            difficulty.target.gameObject.SetActive(true);
            RecordOverride(difficulty.target.transform);
            RecordOverride(difficulty.target.gameObject);
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.farClipPlane = Mathf.Max(camera.farClipPlane, 180);
            var reticle = reticles.Length == 1 ? reticles[0] : Instance(ReticlePath).GetComponent<DesktopAimReticle>();
            if (reticle.gameObject.scene != scene) SceneManager.MoveGameObjectToScene(reticle.gameObject, scene);
            reticle.gameObject.SetActive(true);
            reticle.game = game;
            reticle.bow = game.bow;
            reticle.viewCamera = camera;
            RecordOverride(difficulty);
            RecordOverride(camera);
            RecordOverride(reticle);
            RecordOverride(reticle.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        static void RecordOverride(UnityEngine.Object item)
        {
            EditorUtility.SetDirty(item);
            if (PrefabUtility.IsPartOfPrefabInstance(item))
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
        }

        static void CreateDesktopAimReticle()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ReticlePath) != null) return;
            var root = new GameObject("Desktop Aim Reticle", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            try
            {
                var reticle = root.AddComponent<DesktopAimReticle>();
                reticle.canvas = root.GetComponent<Canvas>();
                reticle.canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                reticle.canvas.sortingOrder = 50;
                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = .5f;
                reticle.sight = Rect("Aim Guide", root.transform, new Vector2(100, 100), Vector2.zero).GetComponent<RectTransform>();
                var meter = Rect("Draw Meter Background", reticle.sight, new Vector2(44, 3), new Vector2(0, -22));
                meter.AddComponent<Image>().color = new Color(.025f, .04f, .04f, .8f);
                reticle.chargeFill = Rect("Draw Meter Fill", meter.transform, Vector2.zero, Vector2.zero).AddComponent<Image>();
                reticle.chargeFill.color = new Color(.3f, .8f, .7f);
                reticle.chargeFill.rectTransform.anchorMin = Vector2.zero;
                reticle.chargeFill.rectTransform.anchorMax = new Vector2(0, 1);
                reticle.chargeFill.rectTransform.sizeDelta = Vector2.zero;
                reticle.hintText = Label(reticle.sight, "Aim Hint", "POWER | hold Space", new Vector2(320, 22), new Vector2(0, -43), 15);
                var shadow = reticle.hintText.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, .85f);
                shadow.effectDistance = new Vector2(1, -1);
                foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                PrefabUtility.SaveAsPrefabAsset(root, ReticlePath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [MenuItem("Archery/Developer 2/Validate Current Scene")]
        public static void ValidateScene()
        {
            var errors = new List<string>();
            var scene = SceneManager.GetActiveScene();
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)).Where(c => c != null).ToArray();
            var rigs = components.OfType<VRPlayerSetup>().ToArray();
            var bows = components.OfType<BowController>().ToArray();
            var games = components.OfType<GameManager>().ToArray();
            if (rigs.Length != 1 || rigs[0].headset == null || rigs[0].leftController == null || rigs[0].rightController == null) errors.Add("Exactly one fully wired VRPlayer is required.");
            if (bows.Length != 1 || bows[0].arrowPrefab == null || bows[0].restNock == null || bows[0].bowstring == null) errors.Add("Exactly one fully wired Bow is required.");
            if (components.OfType<EventSystem>().Count() != 1 || components.OfType<XRUIInputModule>().Count() != 1) errors.Add("Exactly one EventSystem with XRUIInputModule is required.");
            if (games.Length != 1) errors.Add("Exactly one GameManager is required.");
            else
            {
                var game = games[0];
                if (game.bow == null || game.practice == null || game.score == null || game.challenge == null || game.difficulty == null || game.results == null) errors.Add("GameManager references are incomplete.");
                else
                {
                    if (game.practice.bow != game.bow || game.practice.autoStart || !game.practice.gameObject.activeSelf) errors.Add("PracticeManager must reference Bow, remain active, and not auto-start.");
                    if (game.score.bow != game.bow || game.challenge.bow != game.bow || game.challenge.score != game.score || game.challenge.difficulty != game.difficulty) errors.Add("Scoring/challenge manager references are broken.");
                    if (game.difficulty.target == null) errors.Add("Difficulty needs the single central target.");
                    var difficulty = game.difficulty;
                    if (difficulty.distances == null || !difficulty.distances.SequenceEqual(new[] { 6f, 9f, 12f }) ||
                        difficulty.hitsToAdvance < 1 || difficulty.minimumQualifyingPoints < 0 || difficulty.transitionDuration <= 0)
                        errors.Add("Difficulty needs the 6/9/12 m course, a positive streak length, nonnegative qualifying points and a positive move duration.");
                    if (difficulty.practiceDecor == null || difficulty.competitiveDecor == null ||
                        difficulty.practiceDecor == difficulty.competitiveDecor ||
                        difficulty.practiceDecor.transform.parent == null ||
                        difficulty.practiceDecor.transform.parent != difficulty.competitiveDecor.transform.parent ||
                        difficulty.practiceDecor.transform.parent.name != "Range Environment")
                        errors.Add("Distinct Practice Distance Guide and Competitive Course roots must belong to the generated Range Environment.");
                    var tutorial = game.practice.GetComponent<TutorialManager>();
                    if (tutorial == null || tutorial.bow != game.bow || tutorial.practice != game.practice) errors.Add("Tutorial references are broken.");
                }
            }
            var targets = components.OfType<Target>().ToArray();
            if (targets.Length != 1) errors.Add("Exactly one scoring target is required.");
            foreach (var target in targets)
                if (target.feedbackRenderer == null || target.radius <= 0 || target.GetComponent<MeshCollider>()?.sharedMesh == null || target.transform.localScale != Vector3.one) errors.Add("Target needs feedback renderer, circular mesh collider, positive radius and unit scale: " + target.name);
            var gameUIs = components.OfType<GameUI>().ToArray();
            if (gameUIs.Length != 1) errors.Add("Exactly one GameUI is required.");
            else
            {
                var ui = gameUIs[0];
                if (ui.game == null || ui.titleText == null || ui.statusText == null || ui.rulesText == null || ui.scoreText == null || ui.feedbackText == null || ui.helpText == null || ui.menuPanel == null || ui.gameplayPanel == null || ui.practiceButton == null || ui.competitiveButton == null || ui.endPracticeButton == null || ui.menuButton == null || ui.drawMeter == null || !ui.gameObject.activeSelf) errors.Add("GameUI references/root activity are incomplete.");
                if (ui.practiceDistanceSlider == null || ui.practiceDistanceText == null ||
                    ui.practiceDistanceSlider.minValue != 6 || ui.practiceDistanceSlider.maxValue != 12 ||
                    ui.practiceDistanceSlider.wholeNumbers || ui.practiceDistanceSlider.handleRect == null ||
                    ui.gameplayPanel == null ||
                    !ui.practiceDistanceSlider.transform.IsChildOf(ui.gameplayPanel.transform) ||
                    !ui.practiceDistanceText.transform.IsChildOf(ui.gameplayPanel.transform))
                    errors.Add("Right session panel needs a continuous 6–12 m Practice slider, handle and distance label.");
                if (components.OfType<PracticeUI>().Any() ||
                    (ui.game != null && ui.game.practice != null && ui.game.practice.GetComponentInChildren<Canvas>(true) != null))
                    errors.Add("Integrated left Practice UI must be removed while retaining gameplay managers.");
            }
            var resultUIs = components.OfType<ResultsUI>().ToArray();
            if (resultUIs.Length != 1) errors.Add("Exactly one ResultsUI is required.");
            else
            {
                var ui = resultUIs[0];
                if (ui.game == null || ui.results == null || ui.panel == null || ui.outcomeText == null || ui.statsText == null || ui.retryButton == null || ui.menuButton == null || !ui.gameObject.activeSelf) errors.Add("ResultsUI references/root activity are incomplete.");
                var practice = ui.practiceResults;
                if (practice == null || components.OfType<PracticeResultsUI>().Count() != 1)
                    errors.Add("Exactly one PracticeResultsUI must be bound to ResultsUI.");
                if (practice != null)
                {
                    if (practice.transform.parent != ui.transform || practice.gameObject == ui.panel ||
                        (ui.panel != null && practice.transform.IsChildOf(ui.panel.transform)))
                        errors.Add("Practice results must be a separate panel directly under the ResultsUI canvas, not inside Competitive results.");
                    if (practice.diagram == null || practice.statsText == null || practice.distanceText == null ||
                        practice.analysisText == null || practice.unplottedText == null ||
                        practice.retryButton == null || practice.menuButton == null ||
                        practice.distanceDropdown == null || practice.distanceDropdown.template == null ||
                        practice.distanceDropdown.template.GetComponent<ScrollRect>() == null ||
                        practice.distanceDropdown.itemText == null ||
                        practice.distanceDropdown.template.GetComponentInChildren<Toggle>(true) == null)
                        errors.Add("Practice results needs diagram, distance/analysis/statistics text, scrollable distance dropdown and retry/menu controls.");
                    else
                    {
                        if (practice.diagram.raycastTarget)
                            errors.Add("Practice shot grouping diagram must not receive raycasts.");
                        if (practice.diagram.GetComponent<CanvasRenderer>() == null)
                            errors.Add("Practice shot grouping diagram needs a CanvasRenderer to display its mesh.");
                        var boundControls = new Component[]
                        {
                            practice.diagram, practice.statsText, practice.distanceText, practice.analysisText,
                            practice.unplottedText, practice.retryButton, practice.menuButton,
                            practice.distanceDropdown
                        };
                        if (boundControls.Any(control => !control.transform.IsChildOf(practice.transform)))
                            errors.Add("Practice results controls must belong only to the separate Practice panel.");
                        if (practice.distanceDropdown.targetGraphic == null || !practice.distanceDropdown.targetGraphic.raycastTarget ||
                            practice.retryButton.targetGraphic == null || !practice.retryButton.targetGraphic.raycastTarget ||
                            practice.menuButton.targetGraphic == null || !practice.menuButton.targetGraphic.raycastTarget)
                            errors.Add("Practice results buttons need raycast-target graphics for mouse and XR interaction.");
                    }
                }
            }
            var reticles = components.OfType<DesktopAimReticle>().ToArray();
            if (reticles.Length != 1) errors.Add("Exactly one desktop aim reticle is required.");
            else
            {
                var reticle = reticles[0];
                if (reticle.game == null || reticle.bow == null || reticle.viewCamera == null ||
                    reticle.canvas == null || reticle.canvas != reticle.GetComponent<Canvas>() ||
                    reticle.sight == null || reticle.chargeFill == null || reticle.hintText == null ||
                    !reticle.gameObject.activeSelf)
                    errors.Add("Desktop aim reticle references/root activity are incomplete.");
                else if (reticle.game.bow != reticle.bow || (rigs.Length == 1 && reticle.viewCamera != rigs[0].GetComponentInChildren<Camera>(true)))
                    errors.Add("Desktop aim reticle must reference the existing game bow and player camera.");
                var scaler = reticle.GetComponent<CanvasScaler>();
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize ||
                    scaler.referenceResolution != new Vector2(1920, 1080))
                    errors.Add("Desktop aim reticle needs a 1920x1080 scale-with-screen canvas.");
                if (reticle.canvas != null && (reticle.canvas.renderMode != RenderMode.ScreenSpaceOverlay || reticle.canvas.sortingOrder != 50))
                    errors.Add("Desktop aim reticle must use screen-space overlay at sorting order 50.");
            }
            foreach (var canvas in components.OfType<Canvas>())
            {
                bool interactive = canvas.GetComponentsInChildren<Selectable>(true).Length > 0;
                bool aimOverlay = canvas.GetComponent<DesktopAimReticle>() != null;
                if (aimOverlay)
                {
                    if (interactive || canvas.GetComponentsInChildren<BaseRaycaster>(true).Length > 0 ||
                        canvas.GetComponentsInChildren<Graphic>(true).Any(graphic => graphic.raycastTarget))
                        errors.Add("Aim reticle must be entirely noninteractive with no raycasters or raycast-target graphics.");
                }
                else
                {
                    if (canvas.renderMode != RenderMode.WorldSpace) errors.Add("Range UI/sign canvas must use world space: " + canvas.name);
                    if (interactive && (canvas.worldCamera == null || canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null ||
                        canvas.GetComponent<GraphicRaycaster>() == null))
                        errors.Add("Interactive canvas needs rig camera, tracked-device raycaster and mouse graphic raycaster: " + canvas.name);
                }
                if (canvas.GetComponentsInChildren<Collider>(true).Length != 0) errors.Add("UI must not contain physics colliders: " + canvas.name);
            }
            var previews = components.OfType<DesktopPracticePreview>().ToArray();
            if (previews.Length != 1 || previews[0].bow == null || previews[0].headset == null || previews[0].practice != null) errors.Add("One desktop preview must reference Bow/camera and have practice=null.");
            var controls = components.OfType<DesktopRangeControls>().ToArray();
            if (controls.Length != 1 || controls[0].game == null) errors.Add("DesktopRangeControls needs GameManager.");
            foreach (var transform in components.OfType<Transform>())
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0) errors.Add("Missing script on " + transform.name);
            if (errors.Count != 0)
            {
                foreach (string error in errors) Debug.LogError(error);
                throw new InvalidOperationException("ArcheryRange validation failed:\n" + string.Join("\n", errors));
            }
            Debug.Log("ArcheryRange validation passed: rig, managers, target faces, mode-specific scenery, desktop aim guide and UI references are complete.");
        }
    }
}
