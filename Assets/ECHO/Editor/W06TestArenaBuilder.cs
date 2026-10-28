using System;
using System.Collections.Generic;
using System.IO;
using Echo.Interact;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class W06TestArenaBuilder
{
    private const string MonsterScenePath = "Assets/ECHO/Scenes/Test_Monster.unity";
    private const string HideScenePath = "Assets/ECHO/Scenes/Test_Hide.unity";

    [MenuItem("ECHO/W-06/Generate Test Arenas")]
    private static void GenerateTestArenas()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("Stop Play Mode before generating the W-06 test scenes.");
            return;
        }

        if ((File.Exists(MonsterScenePath) || File.Exists(HideScenePath)) &&
            !EditorUtility.DisplayDialog(
                "Regenerate W-06 test scenes?",
                "One or both test scenes already exist. Regeneration will replace those scene files.",
                "Regenerate",
                "Cancel"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string previousScenePath = SceneManager.GetActiveScene().path;
        Directory.CreateDirectory("Assets/ECHO/Scenes");
        CreateMonsterScene();
        CreateHideScene();
        AddScenesToBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(previousScenePath))
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
        else
            EditorSceneManager.OpenScene(MonsterScenePath, OpenSceneMode.Single);

        Debug.Log("Generated Test_Monster and Test_Hide, then added both to Build Settings.");
    }

    [MenuItem("ECHO/W-06/Open Test Monster")]
    private static void OpenMonsterScene()
    {
        OpenTestScene(MonsterScenePath);
    }

    [MenuItem("ECHO/W-06/Open Test Hide")]
    private static void OpenHideScene()
    {
        OpenTestScene(HideScenePath);
    }

    private static void OpenTestScene(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("Test scene is missing. Run ECHO > W-06 > Generate Test Arenas first.");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }

    private static void CreateMonsterScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject host = new GameObject("ECHO W-06 Test Monster");
        EchoGame game = host.AddComponent<EchoGame>();

        Transform[] waypoints =
        {
            CreateWaypoint("Patrol 01 - West", new Vector3(-15f, 0.1f, 0f)),
            CreateWaypoint("Patrol 02 - West passage", new Vector3(-10f, 0.1f, 0f)),
            CreateWaypoint("Patrol 03 - East passage", new Vector3(-4f, 0.1f, 0f)),
            CreateWaypoint("Patrol 04 - Doorway approach", new Vector3(1f, 0.1f, 0f)),
            CreateWaypoint("Patrol 05 - Doorway exit", new Vector3(8f, 0.1f, 0f)),
            CreateWaypoint("Patrol 06 - East corridor", new Vector3(15f, 0.1f, 0f)),
            CreateWaypoint("Patrol 07 - East end", new Vector3(22f, 0.1f, 0f))
        };

        ConfigureGame(game, true, true, new Vector3(22f, 0.1f, 0f),
            new Vector3(-15f, 0.1f, 0f), waypoints);
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MonsterScenePath);
    }

    private static void CreateHideScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject host = new GameObject("ECHO W-06 Test Hide");
        EchoGame game = host.AddComponent<EchoGame>();
        ConfigureGame(game, true, false, new Vector3(0f, 0.1f, 0f),
            Vector3.zero, new Transform[0]);
        CreateHideMinigameUI();
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), HideScenePath);
    }

    private static Transform CreateWaypoint(string name, Vector3 position)
    {
        GameObject waypoint = new GameObject(name);
        waypoint.transform.position = position;
        return waypoint.transform;
    }

    private static void ConfigureGame(
        EchoGame game,
        bool testMode,
        bool monsterEnabled,
        Vector3 playerSpawn,
        Vector3 monsterSpawn,
        Transform[] waypoints)
    {
        SerializedObject serializedGame = new SerializedObject(game);
        serializedGame.FindProperty("testArenaMode").boolValue = testMode;
        serializedGame.FindProperty("testMonsterEnabled").boolValue = monsterEnabled;
        serializedGame.FindProperty("testPlayerSpawn").vector3Value = playerSpawn;
        serializedGame.FindProperty("testMonsterSpawn").vector3Value = monsterSpawn;
        SerializedProperty route = serializedGame.FindProperty("testPatrolWaypoints");
        route.arraySize = waypoints.Length;
        for (int i = 0; i < waypoints.Length; i++)
            route.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i];
        serializedGame.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CreateHideMinigameUI()
    {
        GameObject hideController = new GameObject("Hide Minigame Controller");
        HideMinigame minigame = hideController.AddComponent<HideMinigame>();

        GameObject canvasObject = new GameObject(
            "Hide Minigame Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject panel = CreateUiRect("Minigame Panel", canvasObject.transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(460f, 430f);
        panel.GetComponent<Image>().color = new Color(0.01f, 0.04f, 0.06f, 0.88f);

        GameObject ring = CreateUiRect("Center Ring", panel.transform);
        RectTransform ringRect = ring.GetComponent<RectTransform>();
        ringRect.anchorMin = new Vector2(0.5f, 0.58f);
        ringRect.anchorMax = new Vector2(0.5f, 0.58f);
        ringRect.sizeDelta = new Vector2(240f, 240f);
        Image ringImage = ring.GetComponent<Image>();
        ringImage.color = new Color(0f, 0f, 0f, 0f);
        Outline outline = ring.AddComponent<Outline>();
        outline.effectColor = new Color(0.2f, 0.9f, 1f, 0.95f);
        outline.effectDistance = new Vector2(3f, 3f);

        GameObject reticle = CreateUiRect("Reticle", ring.transform);
        RectTransform reticleRect = reticle.GetComponent<RectTransform>();
        reticleRect.anchorMin = new Vector2(0.5f, 0.5f);
        reticleRect.anchorMax = new Vector2(0.5f, 0.5f);
        reticleRect.sizeDelta = new Vector2(16f, 16f);
        reticle.GetComponent<Image>().color = new Color(1f, 0.48f, 0.22f, 1f);

        GameObject meterBackground = CreateUiRect("Banging Meter Background", panel.transform);
        RectTransform backgroundRect = meterBackground.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.1f, 0.12f);
        backgroundRect.anchorMax = new Vector2(0.9f, 0.12f);
        backgroundRect.sizeDelta = new Vector2(0f, 24f);
        meterBackground.GetComponent<Image>().color = new Color(0.15f, 0.2f, 0.22f, 1f);

        GameObject meterFill = CreateUiRect("Banging Meter Fill", meterBackground.transform);
        RectTransform fillRect = meterFill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = meterFill.GetComponent<Image>();
        fillImage.color = new Color(1f, 0.25f, 0.12f, 1f);
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillAmount = 0f;

        GameObject labelObject = new GameObject("Instructions", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(panel.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.08f, 0.88f);
        labelRect.anchorMax = new Vector2(0.92f, 0.98f);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        Text label = labelObject.GetComponent<Text>();
        label.text = "KEEP THE RETICLE INSIDE THE RING";
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 20;

        SerializedObject serializedMinigame = new SerializedObject(minigame);
        serializedMinigame.FindProperty("minigameCanvas").objectReferenceValue = panel;
        serializedMinigame.FindProperty("centerRingTransform").objectReferenceValue = ringRect;
        serializedMinigame.FindProperty("reticleTransform").objectReferenceValue = reticleRect;
        serializedMinigame.FindProperty("meterFillBar").objectReferenceValue = fillImage;
        serializedMinigame.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateUiRect(string name, Transform parent)
    {
        GameObject element = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        element.transform.SetParent(parent, false);
        return element;
    }

    private static void AddScenesToBuildSettings()
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        AddBuildScene(scenes, MonsterScenePath);
        AddBuildScene(scenes, HideScenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void AddBuildScene(List<EditorBuildSettingsScene> scenes, string path)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (string.Equals(scenes[i].path, path, StringComparison.OrdinalIgnoreCase))
                return;
        }

        scenes.Add(new EditorBuildSettingsScene(path, true));
    }
}
