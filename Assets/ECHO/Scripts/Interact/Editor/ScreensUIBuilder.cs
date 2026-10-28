#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.Interact.Editor
{
    /// <summary>
    /// Editor tool to build default UI hierarchy for Screens (Title, Pause, Caught, Ending)
    /// and save it as Assets/ECHO/Prefabs/UI/ScreensUI.prefab.
    /// Access via menu: ECHO > Build > Screens UI.
    /// </summary>
    public static class ScreensUIBuilder
    {
        private const string PrefabDir = "Assets/ECHO/Prefabs/UI";
        private const string PrefabPath = "Assets/ECHO/Prefabs/UI/ScreensUI.prefab";

        [MenuItem("ECHO/Build/Screens UI", priority = 110)]
        public static void BuildScreensUIPrefab()
        {
            if (!Directory.Exists(PrefabDir))
            {
                Directory.CreateDirectory(PrefabDir);
                AssetDatabase.Refresh();
            }

            // Root Canvas
            var canvasGO = new GameObject("ScreensCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();
            var screenManager = canvasGO.AddComponent<ScreenManager>();

            // ── 1. TITLE PANEL ────────────────────────────────────────────────
            var titlePanel = CreatePanel("TitlePanel", canvasGO.transform, new Color(0.01f, 0.02f, 0.03f, 0.98f));
            CreateLabel("TitleText", titlePanel.transform, "ECHO", 64, TextAnchor.MiddleCenter, Color.cyan, new Vector2(0.2f, 0.70f), new Vector2(0.8f, 0.85f));
            CreateLabel("SubtitleText", titlePanel.transform, "THE LAST SIGNAL", 28, TextAnchor.MiddleCenter, Color.white, new Vector2(0.2f, 0.62f), new Vector2(0.8f, 0.70f));

            var playBtn = CreateButton("PlayButton", titlePanel.transform, "ENTER THE STATION", new Vector2(0.38f, 0.40f), new Vector2(0.62f, 0.50f));
            var quitBtn = CreateButton("QuitButton", titlePanel.transform, "QUIT", new Vector2(0.42f, 0.28f), new Vector2(0.58f, 0.36f));

            playBtn.onClick.AddListener(screenManager.OnPlayClicked);
            quitBtn.onClick.AddListener(screenManager.OnQuitClicked);

            // ── 2. PAUSE PANEL ────────────────────────────────────────────────
            var pausePanel = CreatePanel("PausePanel", canvasGO.transform, new Color(0.01f, 0.02f, 0.03f, 0.90f));
            CreateLabel("PauseTitle", pausePanel.transform, "PAUSED", 36, TextAnchor.MiddleCenter, Color.white, new Vector2(0.3f, 0.82f), new Vector2(0.7f, 0.92f));

            // Graphics Mode Selector
            var gfxRoot = CreateUIElement("GraphicsModeSelector", pausePanel.transform);
            var gfxRect = gfxRoot.GetComponent<RectTransform>();
            gfxRect.anchorMin = new Vector2(0.30f, 0.65f);
            gfxRect.anchorMax = new Vector2(0.70f, 0.76f);
            gfxRect.offsetMin = Vector2.zero;
            gfxRect.offsetMax = Vector2.zero;

            var gfxSelector = gfxRoot.AddComponent<GraphicsModeSelector>();
            var btnPerf = CreateButton("BtnPerf", gfxRoot.transform, "PERFORMANCE", new Vector2(0f, 0f), new Vector2(0.31f, 1f));
            var btnBal = CreateButton("BtnBal", gfxRoot.transform, "BALANCED", new Vector2(0.34f, 0f), new Vector2(0.65f, 1f));
            var btnCine = CreateButton("BtnCine", gfxRoot.transform, "CINEMATIC", new Vector2(0.68f, 0f), new Vector2(1f, 1f));

            // Audio Sliders
            var masterSlider = CreateLabeledSlider("MasterSlider", pausePanel.transform, "MASTER VOLUME", new Vector2(0.32f, 0.52f), new Vector2(0.68f, 0.58f));
            var sfxSlider = CreateLabeledSlider("SfxSlider", pausePanel.transform, "SFX VOLUME", new Vector2(0.32f, 0.44f), new Vector2(0.68f, 0.50f));
            var musicSlider = CreateLabeledSlider("MusicSlider", pausePanel.transform, "MUSIC / AMBIENCE", new Vector2(0.32f, 0.36f), new Vector2(0.68f, 0.42f));

            var resumeBtn = CreateButton("ResumeButton", pausePanel.transform, "RESUME", new Vector2(0.38f, 0.22f), new Vector2(0.62f, 0.30f));
            var pauseQuitBtn = CreateButton("PauseTitleButton", pausePanel.transform, "RETURN TO TITLE", new Vector2(0.38f, 0.12f), new Vector2(0.62f, 0.20f));

            resumeBtn.onClick.AddListener(screenManager.Resume);
            pauseQuitBtn.onClick.AddListener(screenManager.OnReturnToTitleClicked);

            // ── 3. CAUGHT PANEL ───────────────────────────────────────────────
            var caughtPanel = CreatePanel("CaughtPanel", canvasGO.transform, new Color(0.08f, 0.01f, 0.01f, 0.95f));
            CreateLabel("CaughtTitle", caughtPanel.transform, "THE DARK FOUND YOU", 44, TextAnchor.MiddleCenter, Color.red, new Vector2(0.2f, 0.60f), new Vector2(0.8f, 0.75f));
            CreateLabel("CaughtPrompt", caughtPanel.transform, "The station goes quiet. Every echo led it closer.", 22, TextAnchor.MiddleCenter, Color.gray, new Vector2(0.2f, 0.50f), new Vector2(0.8f, 0.60f));
            var retryBtn = CreateButton("RetryButton", caughtPanel.transform, "TRY AGAIN  [SPACE]", new Vector2(0.38f, 0.35f), new Vector2(0.62f, 0.45f));
            retryBtn.onClick.AddListener(screenManager.OnRetryClicked);

            // ── 4. ENDING PANEL ───────────────────────────────────────────────
            var endingPanel = CreatePanel("EndingPanel", canvasGO.transform, new Color(0.01f, 0.01f, 0.02f, 1.0f));
            var endingTitle = CreateLabel("EndingTitle", endingPanel.transform, "GOOD ENDING", 36, TextAnchor.MiddleCenter, Color.cyan, new Vector2(0.1f, 0.82f), new Vector2(0.9f, 0.92f));
            var endingBody = CreateLabel("EndingBody", endingPanel.transform, "Ending storyline...", 20, TextAnchor.UpperCenter, Color.white, new Vector2(0.15f, 0.20f), new Vector2(0.85f, 0.80f));
            var endingReturnBtn = CreateButton("ReturnTitleButton", endingPanel.transform, "RETURN TO TITLE", new Vector2(0.38f, 0.06f), new Vector2(0.62f, 0.14f));
            endingReturnBtn.onClick.AddListener(screenManager.OnReturnToTitleClicked);

            // ── Wire SerializedObject references ──────────────────────────────
            var so = new SerializedObject(screenManager);
            so.FindProperty("titlePanel").objectReferenceValue = titlePanel;
            so.FindProperty("pausePanel").objectReferenceValue = pausePanel;
            so.FindProperty("caughtPanel").objectReferenceValue = caughtPanel;
            so.FindProperty("endingPanel").objectReferenceValue = endingPanel;

            so.FindProperty("masterVolumeSlider").objectReferenceValue = masterSlider;
            so.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider;
            so.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider;

            so.FindProperty("endingTitleText").objectReferenceValue = endingTitle;
            so.FindProperty("endingBodyText").objectReferenceValue = endingBody;
            so.FindProperty("endingReturnButton").objectReferenceValue = endingReturnBtn;

            string[] tuningGuids = AssetDatabase.FindAssets("t:InteractTuning");
            if (tuningGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(tuningGuids[0]);
                var tuningAsset = AssetDatabase.LoadAssetAtPath<InteractTuning>(path);
                so.FindProperty("tuning").objectReferenceValue = tuningAsset;
            }

            so.ApplyModifiedProperties();

            // Wire GraphicsModeSelector references
            var soGfx = new SerializedObject(gfxSelector);
            soGfx.FindProperty("performanceButton").objectReferenceValue = btnPerf;
            soGfx.FindProperty("balancedButton").objectReferenceValue = btnBal;
            soGfx.FindProperty("cinematicButton").objectReferenceValue = btnCine;
            if (tuningGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(tuningGuids[0]);
                var tuningAsset = AssetDatabase.LoadAssetAtPath<InteractTuning>(path);
                soGfx.FindProperty("tuning").objectReferenceValue = tuningAsset;
            }
            soGfx.ApplyModifiedProperties();

            // Deactivate subpanels by default except title
            pausePanel.SetActive(false);
            caughtPanel.SetActive(false);
            endingPanel.SetActive(false);
            titlePanel.SetActive(true);

            // Save Prefab
            PrefabUtility.SaveAsPrefabAsset(canvasGO, PrefabPath);
            Object.DestroyImmediate(canvasGO);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Selection.activeObject = savedPrefab;
            EditorGUIUtility.PingObject(savedPrefab);

            Debug.Log($"[ScreensUIBuilder] Successfully created Screens UI prefab at: {PrefabPath}");
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            var go = CreateUIElement(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = color;
            return go;
        }

        private static Text CreateLabel(string name, Transform parent, string text, int fontSize, TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = CreateUIElement(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return txt;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = CreateUIElement(name, parent);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.15f, 0.22f, 0.30f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.25f, 0.38f, 0.50f);
            colors.pressedColor = new Color(0.08f, 0.12f, 0.18f);
            btn.colors = colors;

            var txt = CreateLabel("Text", go.transform, label, 16, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one);
            return btn;
        }

        private static Slider CreateLabeledSlider(string name, Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var root = CreateUIElement(name, parent);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            CreateLabel("Label", root.transform, label, 14, TextAnchor.MiddleLeft, Color.gray, new Vector2(0f, 0f), new Vector2(0.35f, 1f));

            var sliderGO = CreateUIElement("Slider", root.transform);
            var sliderRect = sliderGO.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.38f, 0.25f);
            sliderRect.anchorMax = new Vector2(1f, 0.75f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            var bg = sliderGO.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.15f, 0.2f);

            var slider = sliderGO.AddComponent<Slider>();
            slider.minValue = 0.0001f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }
    }
}
#endif

