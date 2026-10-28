using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool that builds a static preview of the ECHO station geometry directly
/// into the active scene so that it is visible in the Unity Scene view during Edit mode.
/// The preview is placed at the world origin (0,0,0).
///
/// Usage:
///   ECHO > Build Scene Preview   — spawns the station geometry
///   ECHO > Clear Scene Preview   — removes it
///
/// The preview is automatically removed when entering Play mode to avoid duplicating
/// the runtime-generated station from EchoGame.
/// </summary>
public static class EchoSceneBuilder
{
    private const string RootName = "ECHO_SCENE_PREVIEW";

    // ------------------------------------------------------------------ menu items
    [MenuItem("ECHO/Build Scene Preview", false, 200)]
    public static void BuildPreview()
    {
        ClearPreview();

        // Load prefabs (same as EchoGame.LoadStationKit)
        GameObject floorTile = Load("Floor_Squared_01_6x6");
        GameObject wallModule = Load("Wall_Cube_01_Long");
        GameObject roofTile = Load("Roof_Squared_01_6x6");
        GameObject doorFrame = Load("DoorWay_01_Large");
        GameObject doorPanel = Load("Door_Vert_01");
        GameObject pipeWall = Load("Wall_Pipes_01_Large");
        GameObject console = Load("Wall_Console_01_Large");
        GameObject gear = Load("Wall_Gear_01_Half");
        GameObject intercom = Load("Intercom_01");
        GameObject pipes = Load("Pipes_01");
        GameObject airGrid = Load("Air_Grid_01");
        GameObject overheadGrid = Load("Grid_01");
        GameObject corridorLight = Load("Light_Corridor_01");
        GameObject crate = Load("Crate_01");
        GameObject ship = Load("StarSparrow1");
        GameObject monster = Load("MonsterMutant7_Skin1");
        GameObject[] cells = { Load("EnergyCell_Type1"), Load("EnergyCell_Type2") };

        // Create materials (mirrors EchoGame.CreateMaterials)
        Material template = Resources.Load<Material>("EchoLit");
        if (template == null) { Debug.LogError("EchoLit material not found in Resources."); return; }

        Material floorMat = MakeMat(template, c(0.045f, 0.065f, 0.08f), c(0.005f, 0.018f, 0.024f));
        Material wallMat = MakeMat(template, c(0.06f, 0.09f, 0.11f), c(0.012f, 0.04f, 0.05f));
        Material trimMat = MakeMat(template, c(0.08f, 0.19f, 0.22f), c(0.025f, 0.19f, 0.22f));
        Material brightMat = MakeMat(template, c(0.06f, 0.48f, 0.55f), c(0.01f, 0.5f, 0.62f));
        Material coreMat = MakeMat(template, c(0.13f, 0.8f, 0.9f), c(0.12f, 1.1f, 1.3f));
        Material deadCoreMat = MakeMat(template, c(0.045f, 0.06f, 0.07f), Color.black);
        Material windowMat = MakeMat(template, c(0.12f, 0.42f, 0.55f, 0.22f), c(0.025f, 0.11f, 0.16f));
        windowMat.SetFloat("_Surface", 1f);
        windowMat.SetOverrideTag("RenderType", "Transparent");
        windowMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        windowMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        Material starMat = MakeMat(template, c(0.35f, 0.75f, 1f), c(1.1f, 1.8f, 2.4f));
        Material planetMat = MakeMat(template, c(0.06f, 0.19f, 0.38f), c(0.025f, 0.15f, 0.4f));
        Material creatureMat = MakeMat(template, c(0.09f, 0.025f, 0.04f), c(0.16f, 0.008f, 0.025f));
        Material wireMat = MakeMat(template, c(0.035f, 0.055f, 0.07f), Color.black);

        // Root at origin
        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build ECHO Scene Preview");
        Transform R = root.transform;

        // ============ BUILD STATION (mirrors EchoGame.BuildStation) ============

        // Floors
        FloorGrid(floorTile, R, -48f, 96f, -30f, 30f, "Station");
        // Roofs
        RoofGrid(roofTile, R, -30f, 30f, -18f, 18f, "Deck");

        // Outer walls
        Wall(wallModule, R, -27f, 20f, 10f, true); Wall(wallModule, R, -14f, 20f, 8f, true);
        Wall(wallModule, R, 0f, 20f, 12f, true); Wall(wallModule, R, 14f, 20f, 8f, true);
        Wall(wallModule, R, 27f, 20f, 10f, true);
        Wall(wallModule, R, -27f, -20f, 10f, true); Wall(wallModule, R, -14f, -20f, 8f, true);
        Wall(wallModule, R, 0f, -20f, 12f, true); Wall(wallModule, R, 14f, -20f, 8f, true);
        Wall(wallModule, R, 27f, -20f, 10f, true);
        Wall(wallModule, R, -32f, 0f, 40f, false);
        Wall(wallModule, R, 32f, 12f, 16f, false); Wall(wallModule, R, 32f, -12f, 16f, false);

        // Inner corridor walls
        Wall(wallModule, R, -22f, 4f, 20f, true); Wall(wallModule, R, 0f, 4f, 10f, true);
        Wall(wallModule, R, 18.5f, 4f, 9f, true);
        Wall(wallModule, R, -22f, -4f, 20f, true); Wall(wallModule, R, 0f, -4f, 10f, true);
        Wall(wallModule, R, 18.5f, -4f, 9f, true);

        // Room partitions
        Wall(wallModule, R, -13f, 12f, 16f, false); Wall(wallModule, R, -13f, -12f, 16f, false);
        Wall(wallModule, R, -5f, 5.5f, 3f, false); Wall(wallModule, R, -5f, -5.5f, 3f, false);
        Wall(wallModule, R, -11.5f, 8f, 3f, true); Wall(wallModule, R, -4.5f, 8f, 3f, true);
        Wall(wallModule, R, -11.5f, -8f, 3f, true); Wall(wallModule, R, -4.5f, -8f, 3f, true);
        Wall(wallModule, R, -2f, 8.5f, 1f, false); Wall(wallModule, R, -2f, 16f, 4f, false);
        Wall(wallModule, R, -2f, -8.5f, 1f, false); Wall(wallModule, R, -2f, -16f, 4f, false);
        Wall(wallModule, R, 13f, 13f, 10f, false); Wall(wallModule, R, 13f, -13f, 10f, false);
        Wall(wallModule, R, 5.5f, 18f, 15f, true); Wall(wallModule, R, 5.5f, -18f, 15f, true);

        // Reactor area
        Wall(wallModule, R, 13f, 7f, 6f, true); Wall(wallModule, R, 13f, -7f, 6f, true);
        Wall(wallModule, R, 26f, 7f, 6f, true); Wall(wallModule, R, 26f, -7f, 6f, true);
        Wall(wallModule, R, 26f, 5f, 4f, false); Wall(wallModule, R, 26f, -5f, 4f, false);
        Wall(wallModule, R, 19.5f, 10f, 13f, true); Wall(wallModule, R, 19.5f, -10f, 13f, true);

        // Airlock walls
        Wall(wallModule, R, 30f, 3.8f, 4f, true); Wall(wallModule, R, 30f, -3.8f, 4f, true);

        // Windows
        float[] winPos = { -20f, -8f, 8f, 20f };
        for (int i = 0; i < winPos.Length; i++)
        {
            SpaceWindow(R, winPos[i], 20f, 1f, windowMat, trimMat, brightMat, starMat);
            SpaceWindow(R, winPos[i], -20f, -1f, windowMat, trimMat, brightMat, starMat);
        }

        // Lights
        AddLight(corridorLight, R, v(-23f, 3.2f, 0f), 5f, c(0.08f, 0.62f, 0.75f));
        AddLight(corridorLight, R, v(-8f, 3.2f, 0f), 5f, c(0.07f, 0.55f, 0.68f));
        AddLight(corridorLight, R, v(-8f, 3.2f, 11f), 4f, c(0.05f, 0.47f, 0.64f));
        AddLight(corridorLight, R, v(-8f, 3.2f, -11f), 4f, c(0.05f, 0.47f, 0.64f));
        AddLight(corridorLight, R, v(7f, 3.2f, 12f), 5f, c(0.06f, 0.48f, 0.64f));
        AddLight(corridorLight, R, v(7f, 3.2f, -12f), 5f, c(0.06f, 0.48f, 0.64f));
        AddLight(corridorLight, R, v(20f, 3.2f, 0f), 5f, c(0.62f, 0.1f, 0.08f));
        AddLight(corridorLight, R, v(29f, 3.2f, 0f), 4f, c(0.08f, 0.55f, 0.72f));
        AddLight(corridorLight, R, v(50f, 3.1f, 0f), 7f, c(0.06f, 0.44f, 0.66f));
        AddLight(corridorLight, R, v(70f, 3.1f, 0f), 8f, c(0.12f, 0.3f, 0.55f));

        // ============ BUILD MODULAR ANNEX (mirrors EchoGame.BuildModularAnnex) ============

        RoofGrid(roofTile, R, 35f, 83f, -18f, 18f, "Annex");

        // Annex outer walls
        Wall(wallModule, R, 30f, 20f, 4f, true); Wall(wallModule, R, 40f, 20f, 8f, true);
        Wall(wallModule, R, 52f, 20f, 8f, true); Wall(wallModule, R, 64f, 20f, 8f, true);
        Wall(wallModule, R, 76f, 20f, 8f, true);
        Wall(wallModule, R, 30f, -20f, 4f, true); Wall(wallModule, R, 40f, -20f, 8f, true);
        Wall(wallModule, R, 52f, -20f, 8f, true); Wall(wallModule, R, 64f, -20f, 8f, true);
        Wall(wallModule, R, 76f, -20f, 8f, true);
        Wall(wallModule, R, 80f, 23f, 6f, false); Wall(wallModule, R, 80f, -23f, 6f, false);
        Wall(wallModule, R, 80f, 12f, 16f, false); Wall(wallModule, R, 80f, -12f, 16f, false);

        // Annex inner walls
        Wall(wallModule, R, 41f, 6f, 14f, true); Wall(wallModule, R, 59f, 6f, 10f, true);
        Wall(wallModule, R, 74f, 6f, 22f, true);
        Wall(wallModule, R, 41f, -6f, 14f, true); Wall(wallModule, R, 59f, -6f, 10f, true);
        Wall(wallModule, R, 74f, -6f, 22f, true);

        Wall(wallModule, R, 42f, 13f, 14f, false);
        Wall(wallModule, R, 60f, 7.25f, 1.5f, false); Wall(wallModule, R, 60f, 19f, 2f, false);
        Wall(wallModule, R, 51f, 20f, 18f, true); Wall(wallModule, R, 45f, 6f, 6f, true);
        Wall(wallModule, R, 57f, 6f, 6f, true);
        Wall(wallModule, R, 42f, -13f, 14f, false);
        Wall(wallModule, R, 60f, -7.25f, 1.5f, false); Wall(wallModule, R, 60f, -19f, 2f, false);
        Wall(wallModule, R, 51f, -20f, 18f, true); Wall(wallModule, R, 45f, -6f, 6f, true);
        Wall(wallModule, R, 57f, -6f, 6f, true);
        Wall(wallModule, R, 62f, 12f, 8f, false); Wall(wallModule, R, 62f, -12f, 8f, false);

        // Blast doors (visual only — no runtime behaviour)
        FittedModule(doorPanel, R, v(32f, 1.8f, 0f), false, 5.2f, 3.6f, "Annex blast door");
        FittedModule(doorPanel, R, v(79.4f, 1.8f, 0f), false, 5.2f, 3.6f, "Airlock blast door");

        // Doorframes
        PlaceModule(doorFrame, R, v(32f, 0f, 0f), 90f, "Engineering airlock");
        PlaceModule(doorFrame, R, v(51f, 0f, 6f), 0f, "Hydroponics doorway");
        PlaceModule(doorFrame, R, v(51f, 0f, -6f), 180f, "Maintenance doorway");
        PlaceModule(doorFrame, R, v(62f, 0f, 0f), 90f, "Cargo doorway");
        PlaceModule(doorFrame, R, v(79.4f, 0f, 0f), 90f, "Escape airlock");

        // Annex windows
        SpaceWindow(R, 34f, 20f, 1f, windowMat, trimMat, brightMat, starMat);
        SpaceWindow(R, 46f, 20f, 1f, windowMat, trimMat, brightMat, starMat);
        SpaceWindow(R, 58f, 20f, 1f, windowMat, trimMat, brightMat, starMat);
        SpaceWindow(R, 70f, 20f, 1f, windowMat, trimMat, brightMat, starMat);
        SpaceWindow(R, 34f, -20f, -1f, windowMat, trimMat, brightMat, starMat);
        SpaceWindow(R, 46f, -20f, -1f, windowMat, trimMat, brightMat, starMat);
        SpaceWindow(R, 58f, -20f, -1f, windowMat, trimMat, brightMat, starMat);
        SpaceWindow(R, 70f, -20f, -1f, windowMat, trimMat, brightMat, starMat);

        // Annex modules
        PlaceModule(pipeWall, R, v(40.5f, 0f, 12f), 90f, "Hydroponics column");
        PlaceModule(pipeWall, R, v(40.5f, 0f, -12f), 90f, "Maintenance column");
        PlaceModule(console, R, v(57.5f, 0.25f, 17.25f), 180f, "Hydroponics controls");
        PlaceModule(console, R, v(57.5f, 0.25f, -17.25f), 0f, "Maintenance controls");
        PlaceModule(gear, R, v(62.5f, 0f, 8f), 90f, "Cargo drive assembly");
        PlaceModule(gear, R, v(62.5f, 0f, -8f), 90f, "Cargo drive assembly");
        PlaceModule(pipes, R, v(52f, 3.55f, 0f), 0f, "Main annex pipework");
        PlaceModule(airGrid, R, v(46f, 0f, 13f), 90f, "Hydroponics ventilation");
        PlaceModule(airGrid, R, v(46f, 0f, -13f), 90f, "Maintenance ventilation");
        PlaceModule(overheadGrid, R, v(64f, 3.55f, 11f), 0f, "Cargo overhead gantry");
        PlaceModule(overheadGrid, R, v(64f, 3.55f, -11f), 180f, "Cargo overhead gantry");

        // Intercoms
        for (int x = 40; x <= 76; x += 12)
            PlaceModule(intercom, R, v(x, 1.25f, 3.55f), 0f, "Emergency intercom");

        // Cargo crates
        PlaceModule(crate, R, v(68f, 0.08f, 10f), 0f, "Cargo crate");
        PlaceModule(crate, R, v(72f, 0.08f, 10f), 90f, "Cargo crate");
        PlaceModule(crate, R, v(68f, 0.08f, -10f), 180f, "Cargo crate");
        PlaceModule(crate, R, v(72f, 0.08f, -10f), 270f, "Cargo crate");

        // ============ SIGNS ============
        Sign(R, trimMat, "SPAWN  /  00", v(-25f, 2.65f, 3.72f), 0f);
        Sign(R, trimMat, "ENGINEERING  /  01", v(-8f, 2.65f, 3.72f), 0f);
        Sign(R, trimMat, "CREW QUARTERS  /  02", v(2f, 2.65f, 17.68f), 180f);
        Sign(R, trimMat, "CREW QUARTERS  /  03", v(2f, 2.65f, -17.68f), 0f);
        Sign(R, trimMat, "REACTOR  /  04", v(13.28f, 2.65f, 0f), 90f);
        Sign(R, trimMat, "AIRLOCK  /  05", v(25.9f, 2.65f, 0f), 90f);
        Sign(R, trimMat, "HYDROPONICS  /  06", v(51f, 2.8f, 19.5f), 180f);
        Sign(R, trimMat, "MAINTENANCE  /  07", v(51f, 2.8f, -19.5f), 0f);
        Sign(R, trimMat, "CARGO BAY  /  08", v(70f, 2.8f, 19.5f), 180f);
        Sign(R, trimMat, "RELAY  /  A", v(52f, 3.15f, 17.9f), 180f);
        Sign(R, trimMat, "RELAY  /  B", v(52f, 3.15f, -17.9f), 0f);
        Sign(R, trimMat, "COMMS  /  C", v(72f, 3.15f, 17.9f), 180f);
        Sign(R, trimMat, "SIGNAL ROUTE  //  3-1-2", v(46f, 2.8f, 17.9f), 180f);
        Sign(R, trimMat, "REACTOR  /  CELL ARRAY", v(19f, 2.78f, -2.14f), 0f);

        // ============ DECOR ============
        // Floor seams
        for (int x = -28; x <= 30; x += 4)
            Block(R, "Floor seam", v(x, 0.008f, 0f), v(0.035f, 0.012f, 7.7f), trimMat);
        for (int z = -18; z <= 18; z += 4)
        {
            Block(R, "Floor seam", v(-9f, 0.01f, z), v(7.7f, 0.012f, 0.035f), trimMat);
            Block(R, "Floor seam", v(7f, 0.01f, z), v(11.5f, 0.012f, 0.035f), trimMat);
        }

        // Reactor console
        Block(R, "Reactor backplate", v(19f, 1.62f, -2.35f), v(3.2f, 2.05f, 0.28f), wallMat);
        Block(R, "Reactor shelf", v(19f, 0.48f, -1.9f), v(3.2f, 0.28f, 0.9f), trimMat);
        Block(R, "Reactor strip", v(19f, 2.45f, -2.16f), v(2.6f, 0.08f, 0.06f), brightMat);
        Block(R, "Escape console", v(77f, 1.1f, -2.6f), v(0.6f, 1.5f, 1.2f), wallMat);

        // Reactor cell sockets
        for (int i = 0; i < 3; i++)
        {
            float sx = 18.2f + i * 0.8f;
            Cylinder(R, "Cell receptacle " + (i + 1), v(sx, 0.79f, -1.93f), v(0.68f, 0.09f, 0.68f), wallMat);
            Cylinder(R, "Cell status " + (i + 1), v(sx, 0.89f, -1.93f), v(0.48f, 0.025f, 0.48f), deadCoreMat);
        }

        // ============ SPACE BACKDROP ============
        Sphere(R, "Blue planet", v(55f, 7f, 62f), v(13f, 13f, 13f), planetMat);
        Sphere(R, "Distant moon", v(56f, 6f, -62f), v(10f, 10f, 10f), planetMat);

        // ============ BLACK HOLE ============
        Material horizon = MakeMat(template, c(0.001f, 0.001f, 0.006f), Color.black);
        Sphere(R, "Event horizon", v(158f, 9f, 0f), Vector3.one * 10f, horizon);

        // ============ ESCAPE SHIP ============
        GameObject shipInst = PrefabUtility.InstantiatePrefab(ship, R) as GameObject;
        if (shipInst == null) shipInst = Object.Instantiate(ship, R);
        shipInst.name = "StarSparrow - escape craft (preview)";
        shipInst.transform.localPosition = v(87.5f, 0.15f, 0f);
        shipInst.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);

        // ============ MONSTER (static preview) ============
        GameObject monsterInst = PrefabUtility.InstantiatePrefab(monster, R) as GameObject;
        if (monsterInst == null) monsterInst = Object.Instantiate(monster, R);
        monsterInst.name = "The Listener (preview)";
        monsterInst.transform.localPosition = v(21f, 0f, 0f);
        monsterInst.transform.localScale = new Vector3(2.15f, 2.75f, 2.15f);

        Transform[] monsterTransforms = monsterInst.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < monsterTransforms.Length; i++)
        {
            string tName = monsterTransforms[i].name;
            if (tName == "Character1_Spine" || tName == "Character1_Spine1")
                monsterTransforms[i].localScale = new Vector3(1.1f, 1.55f, 1.1f);
            else if (tName == "Character1_Spine2")
                monsterTransforms[i].localScale = new Vector3(1.08f, 1.45f, 1.08f);
            else if (tName == "Character1_Neck")
                monsterTransforms[i].localRotation = monsterTransforms[i].localRotation * Quaternion.Euler(28f, 48f, -38f);
            else if (tName == "Character1_Head")
                monsterTransforms[i].localRotation = monsterTransforms[i].localRotation * Quaternion.Euler(-12f, 22f, 16f);
        }

        // ============ POWER CORES (static preview) ============
        Vector3[] corePositions = { v(8f, 0.25f, 12f), v(8f, 0.25f, -12f), v(18f, 0.25f, 6.5f) };
        for (int i = 0; i < corePositions.Length; i++)
        {
            GameObject cell = Object.Instantiate(cells[i % cells.Length], R);
            cell.name = "Power core " + (i + 1) + " (preview)";
            cell.transform.localPosition = corePositions[i];
            cell.transform.localScale = Vector3.one * 0.8f;
        }

        // ============ INTERACTABLE MARKERS ============
        CreateMarker(R, "RELAY A", v(52f, 1.45f, 14f), creatureMat);
        CreateMarker(R, "RELAY B", v(52f, 1.45f, -14f), creatureMat);
        CreateMarker(R, "RELAY C", v(70f, 1.45f, 14f), creatureMat);

        // Position the Scene view camera
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
        {
            sceneView.pivot = new Vector3(24f, 2f, 0f);
            sceneView.rotation = Quaternion.Euler(65f, 0f, 0f);
            sceneView.size = 60f;
            sceneView.Repaint();
        }

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("ECHO: Scene preview built at origin. Use ECHO > Clear Scene Preview to remove.");
    }

    [MenuItem("ECHO/Clear Scene Preview", false, 201)]
    public static void ClearPreview()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log("ECHO: Scene preview cleared.");
        }
    }

    // Remove preview automatically when entering Play mode
    [InitializeOnLoadMethod]
    private static void RegisterPlayModeHook()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            GameObject existing = GameObject.Find(RootName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
                Debug.Log("ECHO: Auto-removed scene preview before entering Play mode.");
            }
        }
    }

    // ------------------------------------------------------------------ helpers

    private static GameObject Load(string name)
    {
        GameObject prefab = Resources.Load<GameObject>("StationKit/" + name);
        if (prefab == null)
            Debug.LogError("ECHO preview: missing prefab StationKit/" + name + ". Run ECHO > Import Modular Station Kit first.");
        return prefab;
    }

    private static Color c(float r, float g, float b, float a = 1f)
    {
        return new Color(r, g, b, a);
    }

    private static Vector3 v(float x, float y, float z)
    {
        return new Vector3(x, y, z);
    }

    private static Material MakeMat(Material template, Color color, Color emission)
    {
        Material m = new Material(template);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        else m.color = color;
        if (m.HasProperty("_EmissionColor"))
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", emission);
        }
        return m;
    }

    // ---- Geometry primitives ----

    private static GameObject PlaceModule(GameObject prefab, Transform parent, Vector3 pos, float yaw, string label)
    {
        if (prefab == null) return null;
        GameObject go = PrefabUtility.InstantiatePrefab(prefab, parent) as GameObject;
        if (go == null) go = Object.Instantiate(prefab, parent);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        go.name = label;
        return go;
    }

    private static void FloorGrid(GameObject tile, Transform parent, float minX, float maxX, float minZ, float maxZ, string section)
    {
        if (tile == null) return;
        for (float x = minX; x <= maxX; x += 6f)
            for (float z = minZ; z <= maxZ; z += 6f)
                PlaceModule(tile, parent, v(x, 0.012f, z), 0f, section + " floor " + x + "/" + z);
    }

    private static void RoofGrid(GameObject tile, Transform parent, float minX, float maxX, float minZ, float maxZ, string section)
    {
        if (tile == null) return;
        for (float x = minX; x <= maxX; x += 6f)
            for (float z = minZ; z <= maxZ; z += 6f)
                PlaceModule(tile, parent, v(x, 4.1f, z), 0f, section + " ceiling " + x + "/" + z);
    }

    private static void Wall(GameObject wallPrefab, Transform parent, float x, float z, float length, bool alongX)
    {
        FittedModule(wallPrefab, parent, v(x, 2f, z), alongX, length, 4f, "Bulkhead");
    }

    private static void FittedModule(GameObject prefab, Transform parent, Vector3 center, bool alongX, float length, float height, string label)
    {
        if (prefab == null) return;
        GameObject module = Object.Instantiate(prefab, parent);
        module.transform.localPosition = Vector3.zero;
        module.transform.localRotation = Quaternion.identity;
        module.transform.localScale = Vector3.one;
        module.name = label;

        Renderer[] renderers = module.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) { Object.DestroyImmediate(module); return; }
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);

        Vector3 sourceCenter = parent.InverseTransformPoint(bounds.center);
        Vector3 sourceSize = bounds.size;
        bool longAxisIsZ = sourceSize.z >= sourceSize.x;
        int longAxis = longAxisIsZ ? 2 : 0;
        Vector3 scale = Vector3.one;
        scale.y = height / sourceSize.y;
        scale[longAxis] = length / sourceSize[longAxis];

        float yaw = longAxisIsZ
            ? alongX ? 90f : 0f
            : alongX ? 0f : 90f;
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        module.transform.localRotation = rotation;
        module.transform.localScale = scale;
        module.transform.localPosition = center - rotation * Vector3.Scale(sourceCenter, scale);
    }

    private static void SpaceWindow(Transform parent, float x, float wallZ, float outwardSign,
        Material windowMat, Material trimMat, Material brightMat, Material starMat)
    {
        float insideZ = wallZ - outwardSign * 0.34f;
        Block(parent, "Window viewport", v(x, 2f, insideZ), v(3.82f, 3.78f, 0.12f), windowMat);
        Block(parent, "Window upper frame", v(x, 3.91f, insideZ), v(4.15f, 0.14f, 0.24f), trimMat);
        Block(parent, "Window lower frame", v(x, 0.09f, insideZ), v(4.15f, 0.18f, 0.24f), trimMat);
        Block(parent, "Window left frame", v(x - 2.02f, 2f, insideZ), v(0.15f, 3.95f, 0.24f), brightMat);
        Block(parent, "Window right frame", v(x + 2.02f, 2f, insideZ), v(0.15f, 3.95f, 0.24f), brightMat);

        float outsideZ = wallZ + outwardSign * 42f;
        float[] starOffsets = { -1.5f, -0.75f, 0.05f, 0.9f, 1.55f };
        float[] starHeights = { 0.8f, 2.7f, 1.6f, 3.2f, 1.1f };
        for (int i = 0; i < starOffsets.Length; i++)
        {
            float size = i % 2 == 0 ? 0.28f : 0.18f;
            Sphere(parent, "Distant star", v(x + starOffsets[i] * 4f, starHeights[i], outsideZ), Vector3.one * size, starMat);
        }
    }

    private static void AddLight(GameObject lightPrefab, Transform parent, Vector3 pos, float range, Color color)
    {
        GameObject fixture = PlaceModule(lightPrefab, parent, v(pos.x, 2.6f, pos.z), 0f, "Corridor light");
        if (fixture == null) return;
        Light[] lights = fixture.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].enabled = true;
            lights[i].color = color;
            lights[i].range = Mathf.Max(lights[i].range, range);
            lights[i].intensity = 0.22f;
            lights[i].shadows = LightShadows.None;
        }
        if (lights.Length == 0)
        {
            Light l = fixture.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.range = range;
            l.intensity = 0.22f;
            l.shadows = LightShadows.None;
        }
    }

    private static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = pos;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(block.GetComponent<Collider>());
        return block;
    }

    private static GameObject Sphere(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = name;
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = pos;
        sphere.transform.localScale = scale;
        sphere.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(sphere.GetComponent<Collider>());
        return sphere;
    }

    private static GameObject Cylinder(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cyl.name = name;
        cyl.transform.SetParent(parent, false);
        cyl.transform.localPosition = pos;
        cyl.transform.localScale = scale;
        cyl.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(cyl.GetComponent<Collider>());
        return cyl;
    }

    private static void Sign(Transform parent, Material mat, string text, Vector3 pos, float rotation)
    {
        GameObject sign = new GameObject("Deck stencil");
        sign.transform.SetParent(parent, false);
        sign.transform.localPosition = pos;
        sign.transform.localRotation = Quaternion.Euler(0f, rotation, 0f);
        TextMesh label = sign.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = 42;
        label.characterSize = 0.075f;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = new Color(0.18f, 0.72f, 0.82f);
        label.richText = false;
        MeshRenderer renderer = sign.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = mat;
    }

    private static void CreateMarker(Transform parent, string name, Vector3 pos, Material mat)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = name + " (preview marker)";
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = pos;
        marker.transform.localScale = v(0.52f, 0.62f, 0.52f);
        marker.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(marker.GetComponent<Collider>());
    }
}

