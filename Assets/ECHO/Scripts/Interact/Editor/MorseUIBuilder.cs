#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Echo.Interact.Editor
{
    /// <summary>
    /// Editor builder tool that constructs the Morse Radio UI hierarchy using standard uGUI
    /// and saves it as a prefab at Assets/ECHO/Prefabs/UI/MorseRadioUI.prefab.
    /// Menu: ECHO > Build > Morse UI.
    /// </summary>
    public static class MorseUIBuilder
    {
        private const string PrefabDir = "Assets/ECHO/Prefabs/UI";
        private const string PrefabPath = "Assets/ECHO/Prefabs/UI/MorseRadioUI.prefab";

        [MenuItem("ECHO/Build/Morse UI", priority = 100)]
        public static void BuildMorseUIPrefab()
        {
            if (!Directory.Exists(PrefabDir))
            {
                Directory.CreateDirectory(PrefabDir);
                AssetDatabase.Refresh();
            }

            // Create Canvas root
            var canvasGO = new GameObject("MorseRadioCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            // MorseRadioUI controller component
            var radioUI = canvasGO.AddComponent<MorseRadioUI>();

            // Dark semi-transparent background panel
            var panelGO = CreateUIElement("PanelRoot", canvasGO.transform);
            var panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.25f, 0.2f);
            panelRect.anchorMax = new Vector2(0.75f, 0.8f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0.04f, 0.08f, 0.12f, 0.95f);

            // Title label
            var title = CreateText("Title", panelGO.transform, "LONG-RANGE MORSE TRANSMITTER", 28, TextAnchor.MiddleCenter, Color.cyan);
            title.rectTransform.anchorMin = new Vector2(0f, 0.85f);
            title.rectTransform.anchorMax = new Vector2(1f, 1f);
            title.rectTransform.offsetMin = Vector2.zero;
            title.rectTransform.offsetMax = Vector2.zero;

            // Symbols display label
            var symbols = CreateText("SymbolsDisplay", panelGO.transform, "...", 36, TextAnchor.MiddleCenter, Color.white);
            symbols.rectTransform.anchorMin = new Vector2(0.05f, 0.65f);
            symbols.rectTransform.anchorMax = new Vector2(0.95f, 0.82f);
            symbols.rectTransform.offsetMin = Vector2.zero;
            symbols.rectTransform.offsetMax = Vector2.zero;

            // Decoded preview label
            var preview = CreateText("PreviewDisplay", panelGO.transform, "DECODED: ", 22, TextAnchor.MiddleCenter, new Color(0.6f, 0.85f, 1f));
            preview.rectTransform.anchorMin = new Vector2(0.05f, 0.52f);
            preview.rectTransform.anchorMax = new Vector2(0.95f, 0.64f);
            preview.rectTransform.offsetMin = Vector2.zero;
            preview.rectTransform.offsetMax = Vector2.zero;

            // Feedback label
            var feedback = CreateText("FeedbackDisplay", panelGO.transform, "", 20, TextAnchor.MiddleCenter, Color.yellow);
            feedback.rectTransform.anchorMin = new Vector2(0.05f, 0.40f);
            feedback.rectTransform.anchorMax = new Vector2(0.95f, 0.50f);
            feedback.rectTransform.offsetMin = Vector2.zero;
            feedback.rectTransform.offsetMax = Vector2.zero;

            // Button row container
            var buttonRow = CreateUIElement("ButtonRow", panelGO.transform);
            var btnRowRect = buttonRow.GetComponent<RectTransform>();
            btnRowRect.anchorMin = new Vector2(0.05f, 0.15f);
            btnRowRect.anchorMax = new Vector2(0.95f, 0.35f);
            btnRowRect.offsetMin = Vector2.zero;
            btnRowRect.offsetMax = Vector2.zero;

            // Buttons: DOT, DASH, GAP, CLEAR, SEND
            var dotBtn = CreateButton("DotButton", buttonRow.transform, "DOT (.)", new Vector2(0f, 0f), new Vector2(0.18f, 1f));
            var dashBtn = CreateButton("DashButton", buttonRow.transform, "DASH (-)", new Vector2(0.20f, 0f), new Vector2(0.38f, 1f));
            var gapBtn = CreateButton("GapButton", buttonRow.transform, "SPACE", new Vector2(0.40f, 0f), new Vector2(0.58f, 1f));
            var clearBtn = CreateButton("ClearButton", buttonRow.transform, "CLEAR", new Vector2(0.60f, 0f), new Vector2(0.78f, 1f));
            var sendBtn = CreateButton("SendButton", buttonRow.transform, "SEND", new Vector2(0.80f, 0f), new Vector2(1f, 1f));
            sendBtn.GetComponent<Image>().color = new Color(0.1f, 0.5f, 0.3f);

            // Close hint label
            var hint = CreateText("Hint", panelGO.transform, "[ESC] Close   |   [ENTER] Transmit   |   [BACKSPACE] Erase", 16, TextAnchor.MiddleCenter, Color.gray);
            hint.rectTransform.anchorMin = new Vector2(0f, 0.02f);
            hint.rectTransform.anchorMax = new Vector2(1f, 0.10f);
            hint.rectTransform.offsetMin = Vector2.zero;
            hint.rectTransform.offsetMax = Vector2.zero;

            // Wire SerializedObject fields on MorseRadioUI
            var so = new SerializedObject(radioUI);
            so.FindProperty("panelRoot").objectReferenceValue = panelGO;
            so.FindProperty("symbolsLabel").objectReferenceValue = symbols;
            so.FindProperty("previewLabel").objectReferenceValue = preview;
            so.FindProperty("feedbackLabel").objectReferenceValue = feedback;
            so.FindProperty("dotButton").objectReferenceValue = dotBtn;
            so.FindProperty("dashButton").objectReferenceValue = dashBtn;
            so.FindProperty("gapButton").objectReferenceValue = gapBtn;
            so.FindProperty("clearButton").objectReferenceValue = clearBtn;
            so.FindProperty("sendButton").objectReferenceValue = sendBtn;

            // Assign InteractTuning asset if one exists
            string[] tuningGuids = AssetDatabase.FindAssets("t:InteractTuning");
            if (tuningGuids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(tuningGuids[0]);
                var tuningAsset = AssetDatabase.LoadAssetAtPath<InteractTuning>(path);
                so.FindProperty("tuning").objectReferenceValue = tuningAsset;
            }

            so.ApplyModifiedProperties();

            // Save as Prefab
            PrefabUtility.SaveAsPrefabAsset(canvasGO, PrefabPath);
            Object.DestroyImmediate(canvasGO);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Selection.activeObject = savedPrefab;
            EditorGUIUtility.PingObject(savedPrefab);

            Debug.Log($"[MorseUIBuilder] Successfully created Morse Radio UI prefab at: {PrefabPath}");
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor alignment, Color color)
        {
            var go = CreateUIElement(name, parent);
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
            var btnGO = CreateUIElement(name, parent);
            var rect = btnGO.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = btnGO.AddComponent<Image>();
            image.color = new Color(0.15f, 0.22f, 0.30f);

            var button = btnGO.AddComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.25f, 0.38f, 0.50f);
            colors.pressedColor = new Color(0.08f, 0.12f, 0.18f);
            button.colors = colors;

            var labelTxt = CreateText("Label", btnGO.transform, label, 18, TextAnchor.MiddleCenter, Color.white);
            labelTxt.rectTransform.anchorMin = Vector2.zero;
            labelTxt.rectTransform.anchorMax = Vector2.one;
            labelTxt.rectTransform.offsetMin = Vector2.zero;
            labelTxt.rectTransform.offsetMax = Vector2.zero;

            return button;
        }
    }
}
#endif

