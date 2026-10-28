using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public sealed class EchoGame : MonoBehaviour
{
    private enum GameState
    {
        Title,
        Playing,
        Puzzle,
        SignalPuzzle,
        Choice,
        Paused,
        Dead,
        Ending
    }

    private static readonly Vector3 SpawnPoint = new Vector3(-25f, 0.1f, 0f);
    private static readonly Vector3 StationEditorOffset = new Vector3(-24f, 0f, 0f);
    private const float FlashlightCapacity = 60f;
    private static readonly Vector3[] RouteNodes =
    {
        new Vector3(-25f, 0.1f, 0f), new Vector3(-16f, 0.1f, 0f),
        new Vector3(-8f, 0.1f, 0f), new Vector3(0f, 0.1f, 0f),
        new Vector3(8f, 0.1f, 0f), new Vector3(16f, 0.1f, 0f),
        new Vector3(24f, 0.1f, 0f), new Vector3(-8f, 0.1f, 8f),
        new Vector3(0f, 0.1f, 11f), new Vector3(9f, 0.1f, 11f),
        new Vector3(-8f, 0.1f, -8f), new Vector3(0f, 0.1f, -11f),
        new Vector3(9f, 0.1f, -11f), new Vector3(20f, 0.1f, 0f),
        new Vector3(32f, 0.1f, 0f), new Vector3(40f, 0.1f, 0f),
        new Vector3(50f, 0.1f, 0f), new Vector3(58f, 0.1f, 0f),
        new Vector3(67f, 0.1f, 0f), new Vector3(75f, 0.1f, 0f),
        new Vector3(44f, 0.1f, 11f), new Vector3(52f, 0.1f, 12f),
        new Vector3(44f, 0.1f, -11f), new Vector3(52f, 0.1f, -12f),
        new Vector3(70f, 0.1f, 12f), new Vector3(70f, 0.1f, -12f)
    };
    private static readonly int[,] RouteEdges =
    {
        { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 4 }, { 4, 5 }, { 5, 13 }, { 13, 6 },
        { 2, 7 }, { 7, 8 }, { 8, 9 },
        { 2, 10 }, { 10, 11 }, { 11, 12 },
        { 6, 14 }, { 14, 15 }, { 15, 16 }, { 16, 17 }, { 17, 18 }, { 18, 19 },
        { 15, 20 }, { 20, 21 }, { 21, 16 },
        { 15, 22 }, { 22, 23 }, { 23, 16 },
        { 17, 24 }, { 17, 25 }, { 24, 18 }, { 25, 18 }
    };

    private static EchoGame instance;
    private readonly int[] puzzleCode = { 2, 3, 1 };
    private readonly int[] signalCode = { 3, 1, 2 };
    private readonly List<int> puzzleInput = new List<int>(3);
    private readonly List<int> signalInput = new List<int>(3);
    private readonly bool[] collectedCores = new bool[3];
    private readonly List<EchoInteractable> interactables = new List<EchoInteractable>();
    private readonly List<Light> corridorLights = new List<Light>();
    private readonly List<EchoPulseReveal> pulseRevealTargets = new List<EchoPulseReveal>();
    private readonly bool[] activatedRelays = new bool[3];
    private readonly bool[] collectedLogs = new bool[3];
    private readonly bool[] collectedSupplies = new bool[3];
    private readonly GameObject[] relayIndicators = new GameObject[3];
    private readonly GameObject[] reactorCellIndicators = new GameObject[3];
    private readonly GameObject[] reactorSocketIndicators = new GameObject[3];
    private readonly GameObject[] reactorWireConnectors = new GameObject[3];
    private readonly Dictionary<Material, Material> convertedCellMaterials = new Dictionary<Material, Material>();
    private readonly bool[] reactorWiresConnected = new bool[3];
    private readonly LineRenderer[] reactorWires = new LineRenderer[3];

    private GameState state = GameState.Title;
    private Transform worldRoot;
    private GameObject player;
    private CharacterController characterController;
    private EchoPlayerMotor playerMotor;
    private Camera playerCamera;
    private EchoCreature creature;
    private AudioSource ambienceSource;
    private AudioSource voiceSource;
    private AudioClip doorClip;
    private AudioClip[] voiceAlerts = new AudioClip[3];
    private GameObject airlockBulkhead;
    private GameObject annexBulkhead;
    private GameObject escapeShip;
    private GameObject blackHole;
    private Vector3 escapeShipStartPosition;
    private Quaternion escapeShipStartRotation;
    private Vector3 escapeShipStartScale;
    private Vector3 blackHolePosition = new Vector3(158f, 9f, 0f);
    private EchoInteractable activeInteractable;
    private GameObject[] kitCrates = new GameObject[4];
    private Material floorMaterial;
    private Material wallMaterial;
    private Material trimMaterial;
    private Material brightMaterial;
    private Material coreMaterial;
    private Material creatureMaterial;
    private Material deadCoreMaterial;
    private Material windowMaterial;
    private Material starMaterial;
    private Material planetMaterial;
    private Material reactorWireMaterial;
    private Material materialTemplate;
    private GameObject floorTilePrefab;
    private GameObject wallModulePrefab;
    private GameObject roofTilePrefab;
    private GameObject doorFramePrefab;
    private GameObject doorPanelPrefab;
    private GameObject portholePrefab;
    private GameObject pipeWallPrefab;
    private GameObject consolePrefab;
    private GameObject gearPrefab;
    private GameObject intercomPrefab;
    private GameObject pipesPrefab;
    private GameObject airGridPrefab;
    private GameObject overheadGridPrefab;
    private GameObject corridorLightPrefab;
    private GameObject cargoCratePrefab;
    private GameObject monsterPrefab;
    private GameObject escapeShipPrefab;
    private GameObject[] powerCellPrefabs;
    private string statusMessage = "The station is listening.";
    private string endingTitle = "";
    private string endingBody = "";
    private float statusUntil;
    private float battery = FlashlightCapacity;
    private float titleAnim;
    private float escapeFlightTime;
    private float escapeShipSpeed;
    private float blackHoleEntryTime;
    private int coresFound;
    private int relayCount;
    private int logsFound;
    private bool powerRestored;
    private bool signalCalibrated;
    private bool flashlightOn = true;
    private bool isCrouching;
    private int lastVoiceAlertTier;
    private float nextVoiceAlertTime;
    private GUIStyle titleStyle;
    private GUIStyle subtitleStyle;
    private GUIStyle hudStyle;
    private GUIStyle smallStyle;
    private GUIStyle panelStyle;
    private GUIStyle buttonStyle;
    private GUIStyle headingStyle;
    private bool stylesReady;

    public static EchoGame Instance { get { return instance; } }
    public bool CanEmitPulse { get { return state == GameState.Playing; } }
    public Transform PlayerTransform { get { return player != null ? player.transform : null; } }
    public Vector3 PlayerPosition { get { return player != null ? player.transform.localPosition : Vector3.zero; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindAnyObjectByType<EchoGame>() != null)
            return;

        GameObject host = new GameObject("ECHO - THE LAST SIGNAL");
        DontDestroyOnLoad(host);
        host.AddComponent<EchoGame>();
    }
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        BuildGame();
        SetCursor(false);
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void BuildGame()
    {
        Camera oldCamera = Camera.main;
        if (oldCamera != null)
        {
            oldCamera.enabled = false;
            AudioListener oldListener = oldCamera.GetComponent<AudioListener>();
            if (oldListener != null)
                oldListener.enabled = false;
        }

        foreach (Light oldLight in FindObjectsByType<Light>())
            oldLight.enabled = false;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.055f, 0.075f, 0.09f);
        RenderSettings.ambientIntensity = 0.22f;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.008f, 0.018f, 0.025f);
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.018f;
        BuildNebulaSkybox();

        CreateMaterials();
        LoadStationKit();
        LoadStationAudio();
        worldRoot = new GameObject("Station - Deck 03").transform;
        worldRoot.position = StationEditorOffset;
        BuildStation();
        BuildModularAnnex();
        BuildSpaceBackdrop();
        BuildBlackHole();
        BuildEscapeShip();
        BuildPlayer();
        BuildCreature();
        BuildAudio();
        AddInteractable("CORE", 0, "Take power core", new Vector3(8f, 0.25f, 12f), true);
        AddInteractable("CORE", 1, "Take power core", new Vector3(8f, 0.25f, -12f), true);
        AddInteractable("CORE", 2, "Take power core", new Vector3(18f, 0.25f, 6.5f), true);
        AddInteractable("PUZZLE", 0, "Access breaker panel", new Vector3(19f, 0.1f, -1.5f), false);
        AddInteractable("NOTE", 0, "Read maintenance note", new Vector3(-5.3f, 0.1f, 11f), false);
        AddInteractable("EXIT", 0, "Open the final airlock", new Vector3(76f, 0.1f, 0f), false);
        AddInteractable("RELAY", 0, "Align long-range relay", new Vector3(52f, 0.1f, 14f), false);
        AddInteractable("RELAY", 1, "Align backup relay", new Vector3(52f, 0.1f, -14f), false);
        AddInteractable("RELAY", 2, "Align cargo-bay relay", new Vector3(70f, 0.1f, 14f), false);
        AddInteractable("LOG", 0, "Play crew recording", new Vector3(47f, 0.1f, 16f), false);
        AddInteractable("LOG", 1, "Play crew recording", new Vector3(47f, 0.1f, -16f), false);
        AddInteractable("LOG", 2, "Play crew recording", new Vector3(70f, 0.1f, -14f), false);
        AddInteractable("NOTE", 1, "Read relay calibration note", new Vector3(46f, 0.1f, 17f), false);
        AddInteractable("SUPPLY", 0, "Open emergency supply case", new Vector3(57f, 0.1f, 17f), false);
        AddInteractable("SUPPLY", 1, "Open emergency supply case", new Vector3(57f, 0.1f, -17f), false);
        AddInteractable("SUPPLY", 2, "Open emergency supply case", new Vector3(72f, 0.1f, -11f), false);
        AddInteractable("COMMS", 0, "Calibrate the long-range array", new Vector3(72f, 0.1f, 0f), false);
        BuildDecor();
        for (int i = 0; i < reactorWires.Length; i++)
            AddInteractable("WIRE", i, "Connect reactor lead " + (i + 1), new Vector3(18.2f + i * 0.8f, 0.95f, -1.9f), false);
        CreateSign("SPAWN  /  00", new Vector3(-25f, 2.65f, 3.72f), 0f);
        CreateSign("ENGINEERING  /  01", new Vector3(-8f, 2.65f, 3.72f), 0f);
        CreateSign("CREW QUARTERS  /  02", new Vector3(2f, 2.65f, 17.68f), 180f);
        CreateSign("CREW QUARTERS  /  03", new Vector3(2f, 2.65f, -17.68f), 0f);
        CreateSign("REACTOR  /  04", new Vector3(13.28f, 2.65f, 0f), 90f);
        CreateSign("AIRLOCK  /  05", new Vector3(25.9f, 2.65f, 0f), 90f);
        CreateSign("HYDROPONICS  /  06", new Vector3(51f, 2.8f, 19.5f), 180f);
        CreateSign("MAINTENANCE  /  07", new Vector3(51f, 2.8f, -19.5f), 0f);
        CreateSign("CARGO BAY  /  08", new Vector3(70f, 2.8f, 19.5f), 180f);
    }

    private void LoadStationKit()
    {
        floorTilePrefab = LoadKitModule("Floor_Squared_01_6x6");
        wallModulePrefab = LoadKitModule("Wall_Cube_01_Long");
        roofTilePrefab = LoadKitModule("Roof_Squared_01_6x6");
        doorFramePrefab = LoadKitModule("DoorWay_01_Large");
        doorPanelPrefab = LoadKitModule("Door_Vert_01");
        portholePrefab = LoadKitModule("Porthole_01_Long");
        pipeWallPrefab = LoadKitModule("Wall_Pipes_01_Large");
        consolePrefab = LoadKitModule("Wall_Console_01_Large");
        gearPrefab = LoadKitModule("Wall_Gear_01_Half");
        intercomPrefab = LoadKitModule("Intercom_01");
        pipesPrefab = LoadKitModule("Pipes_01");
        airGridPrefab = LoadKitModule("Air_Grid_01");
        overheadGridPrefab = LoadKitModule("Grid_01");
        corridorLightPrefab = LoadKitModule("Light_Corridor_01");
        cargoCratePrefab = LoadKitModule("Crate_01");
        monsterPrefab = LoadKitModule("MonsterMutant7_Skin1");
        escapeShipPrefab = LoadKitModule("StarSparrow1");
        powerCellPrefabs = new[]
        {
            LoadKitModule("EnergyCell_Type1"),
            LoadKitModule("EnergyCell_Type2")
        };
    }

    private void LoadStationAudio()
    {
        doorClip = LoadStationClip("Pneumatic-door");
        voiceAlerts[0] = LoadStationClip("EchoWarningDistant");
        voiceAlerts[1] = LoadStationClip("EchoWarningClose");
        voiceAlerts[2] = LoadStationClip("EchoWarningCritical");
    }

    private static AudioClip LoadStationClip(string name)
    {
        AudioClip clip = Resources.Load<AudioClip>("StationAudio/" + name);
        if (clip == null)
            throw new System.InvalidOperationException("The station audio clip '" + name + "' is missing from Resources/StationAudio.");
        return clip;
    }

    private static GameObject LoadKitModule(string name)
    {
        GameObject prefab = Resources.Load<GameObject>("StationKit/" + name);
        if (prefab == null)
            throw new System.InvalidOperationException("The sci-fi kit module '" + name + "' is missing. In the Unity editor, run ECHO > Import Modular Station Kit.");
        return prefab;
    }

    private void CreateMaterials()
    {
        materialTemplate = Resources.Load<Material>("EchoLit");
        if (materialTemplate == null || materialTemplate.shader == null)
            throw new System.InvalidOperationException("The EchoLit material and its URP/Lit shader are required.");

        floorMaterial = MakeMaterial(new Color(0.045f, 0.065f, 0.08f), new Color(0.005f, 0.018f, 0.024f));
        wallMaterial = MakeMaterial(new Color(0.06f, 0.09f, 0.11f), new Color(0.012f, 0.04f, 0.05f));
        trimMaterial = MakeMaterial(new Color(0.08f, 0.19f, 0.22f), new Color(0.025f, 0.19f, 0.22f));
        brightMaterial = MakeMaterial(new Color(0.06f, 0.48f, 0.55f), new Color(0.01f, 0.5f, 0.62f));
        coreMaterial = MakeMaterial(new Color(0.13f, 0.8f, 0.9f), new Color(0.12f, 1.1f, 1.3f));
        creatureMaterial = MakeMaterial(new Color(0.09f, 0.025f, 0.04f), new Color(0.16f, 0.008f, 0.025f));
        deadCoreMaterial = MakeMaterial(new Color(0.045f, 0.06f, 0.07f), Color.black);
        reactorWireMaterial = MakeMaterial(new Color(0.035f, 0.055f, 0.07f), Color.black);
        windowMaterial = MakeMaterial(new Color(0.12f, 0.42f, 0.55f, 0.22f), new Color(0.025f, 0.11f, 0.16f));
        windowMaterial.SetFloat("_Surface", 1f);
        windowMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        windowMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        windowMaterial.SetFloat("_ZWrite", 0f);
        windowMaterial.SetOverrideTag("RenderType", "Transparent");
        windowMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        windowMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        starMaterial = MakeMaterial(new Color(0.35f, 0.75f, 1f), new Color(1.1f, 1.8f, 2.4f));
        planetMaterial = MakeMaterial(new Color(0.06f, 0.19f, 0.38f), new Color(0.025f, 0.15f, 0.4f));
    }

    private static Material MakeMaterial(Color color, Color emission)
    {
        Material material = new Material(Instance.materialTemplate);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        return material;
    }

    private void BuildStation()
    {
        BuildFloorGrid(-48f, 96f, -30f, 30f, "Station");
        BuildRoofGrid(-30f, 30f, -18f, 18f, "Deck");

        AddWall(-27f, 20f, 10f, true);
        AddWall(-14f, 20f, 8f, true);
        AddWall(0f, 20f, 12f, true);
        AddWall(14f, 20f, 8f, true);
        AddWall(27f, 20f, 10f, true);
        AddWall(-27f, -20f, 10f, true);
        AddWall(-14f, -20f, 8f, true);
        AddWall(0f, -20f, 12f, true);
        AddWall(14f, -20f, 8f, true);
        AddWall(27f, -20f, 10f, true);
        AddWall(-32f, 0f, 40f, false);
        AddWall(32f, 12f, 16f, false);
        AddWall(32f, -12f, 16f, false);

        AddWall(-22f, 4f, 20f, true);
        AddWall(0f, 4f, 10f, true);
        AddWall(18.5f, 4f, 9f, true);
        AddWall(-22f, -4f, 20f, true);
        AddWall(0f, -4f, 10f, true);
        AddWall(18.5f, -4f, 9f, true);

        AddWall(-13f, 12f, 16f, false);
        AddWall(-13f, -12f, 16f, false);
        AddWall(-5f, 5.5f, 3f, false);
        AddWall(-5f, -5.5f, 3f, false);
        AddWall(-11.5f, 8f, 3f, true);
        AddWall(-4.5f, 8f, 3f, true);
        AddWall(-11.5f, -8f, 3f, true);
        AddWall(-4.5f, -8f, 3f, true);
        AddWall(-2f, 8.5f, 1f, false);
        AddWall(-2f, 16f, 4f, false);
        AddWall(-2f, -8.5f, 1f, false);
        AddWall(-2f, -16f, 4f, false);
        AddWall(13f, 13f, 10f, false);
        AddWall(13f, -13f, 10f, false);
        AddWall(5.5f, 18f, 15f, true);
        AddWall(5.5f, -18f, 15f, true);

        AddWall(13f, 7f, 6f, true);
        AddWall(13f, -7f, 6f, true);
        AddWall(26f, 7f, 6f, true);
        AddWall(26f, -7f, 6f, true);
        AddWall(26f, 5f, 4f, false);
        AddWall(26f, -5f, 4f, false);
        AddWall(19.5f, 10f, 13f, true);
        AddWall(19.5f, -10f, 13f, true);

        AddWall(30f, 3.8f, 4f, true);
        AddWall(30f, -3.8f, 4f, true);

        float[] windowPositions = { -20f, -8f, 8f, 20f };
        for (int i = 0; i < windowPositions.Length; i++)
        {
            BuildSpaceWindow(windowPositions[i], 20f, 1f, "Crew deck observation window");
            BuildSpaceWindow(windowPositions[i], -20f, -1f, "Crew deck observation window");
        }

        AddLight(new Vector3(-23f, 3.2f, 0f), 5f, new Color(0.08f, 0.62f, 0.75f));
        AddLight(new Vector3(-8f, 3.2f, 0f), 5f, new Color(0.07f, 0.55f, 0.68f));
        AddLight(new Vector3(-8f, 3.2f, 11f), 4f, new Color(0.05f, 0.47f, 0.64f));
        AddLight(new Vector3(-8f, 3.2f, -11f), 4f, new Color(0.05f, 0.47f, 0.64f));
        AddLight(new Vector3(7f, 3.2f, 12f), 5f, new Color(0.06f, 0.48f, 0.64f));
        AddLight(new Vector3(7f, 3.2f, -12f), 5f, new Color(0.06f, 0.48f, 0.64f));
        AddLight(new Vector3(20f, 3.2f, 0f), 5f, new Color(0.62f, 0.1f, 0.08f));
        AddLight(new Vector3(29f, 3.2f, 0f), 4f, new Color(0.08f, 0.55f, 0.72f));
        AddLight(new Vector3(50f, 3.1f, 0f), 7f, new Color(0.06f, 0.44f, 0.66f));
        AddLight(new Vector3(70f, 3.1f, 0f), 8f, new Color(0.12f, 0.3f, 0.55f));
    }

    private void BuildSpaceWindow(float x, float wallZ, float outwardSign, string label)
    {
        float insideZ = wallZ - outwardSign * 0.34f;
        MakeBlock(label + " / reinforced viewport", new Vector3(x, 2f, insideZ),
            new Vector3(3.82f, 3.78f, 0.12f), windowMaterial, true);
        MakeBlock(label + " / upper frame", new Vector3(x, 3.91f, insideZ),
            new Vector3(4.15f, 0.14f, 0.24f), trimMaterial, false);
        MakeBlock(label + " / lower frame", new Vector3(x, 0.09f, insideZ),
            new Vector3(4.15f, 0.18f, 0.24f), trimMaterial, false);
        MakeBlock(label + " / left frame", new Vector3(x - 2.02f, 2f, insideZ),
            new Vector3(0.15f, 3.95f, 0.24f), brightMaterial, false);
        MakeBlock(label + " / right frame", new Vector3(x + 2.02f, 2f, insideZ),
            new Vector3(0.15f, 3.95f, 0.24f), brightMaterial, false);

        float outsideZ = wallZ + outwardSign * 42f;
        float[] starOffsets = { -1.5f, -0.75f, 0.05f, 0.9f, 1.55f };
        float[] starHeights = { 0.8f, 2.7f, 1.6f, 3.2f, 1.1f };
        for (int i = 0; i < starOffsets.Length; i++)
        {
            GameObject star = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            star.name = "Distant star";
            star.transform.SetParent(worldRoot, false);
            star.transform.localPosition = new Vector3(x + starOffsets[i] * 4f, starHeights[i], outsideZ);
            float size = i % 2 == 0 ? 0.28f : 0.18f;
            star.transform.localScale = Vector3.one * size;
            star.GetComponent<Renderer>().sharedMaterial = starMaterial;
            Destroy(star.GetComponent<Collider>());
        }
    }

    private void BuildSpaceBackdrop()
    {
        CreateSpaceBody(new Vector3(55f, 7f, 62f), new Vector3(13f, 13f, 13f), "Blue planet beyond the viewport");
        CreateSpaceBody(new Vector3(56f, 6f, -62f), new Vector3(10f, 10f, 10f), "Distant moon beyond the viewport");
    }

    private void BuildNebulaSkybox()
    {
        const int width = 512;
        const int height = 256;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
        texture.name = "ECHO / aurora nebula sky";
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Bilinear;
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            float v = (float)y / (height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = (float)x / width;
                float cloudNoise = Mathf.Sin(u * 35f + Mathf.Sin(v * 18f) * 2.5f) * 0.36f +
                    Mathf.Sin(u * 79f - v * 31f) * 0.22f +
                    Mathf.Cos(u * 17f + v * 43f) * 0.2f;
                float nebulaCenter = 0.52f + Mathf.Sin(u * Mathf.PI * 4f) * 0.12f;
                float cloudBand = Mathf.Exp(-Mathf.Pow((v - nebulaCenter) * 4.4f, 2f));
                float cloud = Mathf.Clamp01((cloudNoise + 0.28f) * 0.95f) * cloudBand;
                float auroraCenter = 0.54f + Mathf.Sin(u * Mathf.PI * 2.8f + 0.8f) * 0.09f;
                float auroraCurtains = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(u * 53f + Mathf.Sin(u * 13f) * 3f)), 5f);
                float aurora = Mathf.Exp(-Mathf.Pow((v - auroraCenter) * 15f, 2f)) * (0.25f + auroraCurtains);

                Color color = Color.Lerp(new Color(0.002f, 0.004f, 0.018f), new Color(0.008f, 0.018f, 0.055f), v);
                color += new Color(0.22f, 0.035f, 0.36f) * cloud * 0.52f;
                color += new Color(0.02f, 0.42f, 0.32f) * aurora * 0.62f;
                color += new Color(0.035f, 0.12f, 0.42f) * cloud * 0.25f;

                float starHash = Mathf.Sin((x + 1) * 127.1f + (y + 1) * 311.7f) * 43758.5453f;
                starHash -= Mathf.Floor(starHash);
                if (starHash > 0.9988f)
                    color += Color.white * (starHash > 0.9997f ? 1f : 0.45f);

                pixels[y * width + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        Shader skyShader = Shader.Find("Skybox/Panoramic");
        if (skyShader == null)
            throw new System.InvalidOperationException("The Unity panoramic skybox shader is required for the nebula sky.");

        Material sky = new Material(skyShader);
        sky.name = "ECHO / aurora nebula";
        sky.SetTexture("_MainTex", texture);
        if (sky.HasProperty("_Tint"))
            sky.SetColor("_Tint", Color.white);
        if (sky.HasProperty("_Exposure"))
            sky.SetFloat("_Exposure", 1.35f);
        if (sky.HasProperty("_Mapping"))
            sky.SetFloat("_Mapping", 1f);
        RenderSettings.skybox = sky;
    }

    private void BuildBlackHole()
    {
        blackHole = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        blackHole.name = "Event horizon / escape destination";
        blackHole.transform.SetParent(worldRoot, false);
        blackHole.transform.localPosition = blackHolePosition;
        blackHole.transform.localScale = Vector3.one * 10f;
        Material horizon = MakeMaterial(new Color(0.001f, 0.001f, 0.006f), Color.black);
        blackHole.GetComponent<Renderer>().sharedMaterial = horizon;
        Destroy(blackHole.GetComponent<Collider>());

        Material violetDisk = MakeMaterial(new Color(0.38f, 0.12f, 0.7f), new Color(1.1f, 0.22f, 1.8f));
        Material cyanDisk = MakeMaterial(new Color(0.08f, 0.6f, 0.66f), new Color(0.15f, 1.6f, 1.8f));
        CreateAccretionRing("Black hole / violet accretion", blackHolePosition, 5.2f, 6.1f, violetDisk);
        CreateAccretionRing("Black hole / aurora accretion", blackHolePosition + Vector3.left * 0.18f, 6.2f, 7.1f, cyanDisk);
    }

    private void CreateAccretionRing(string label, Vector3 position, float innerRadius, float outerRadius, Material material)
    {
        const int segments = 96;
        Mesh mesh = new Mesh();
        mesh.name = label;
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            float y = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            vertices[i * 2] = new Vector3(0f, y * innerRadius, z * innerRadius);
            vertices[i * 2 + 1] = new Vector3(0f, y * outerRadius, z * outerRadius);
            if (i < segments)
            {
                int index = i * 6;
                int vertex = i * 2;
                triangles[index] = vertex;
                triangles[index + 1] = vertex + 1;
                triangles[index + 2] = vertex + 2;
                triangles[index + 3] = vertex + 1;
                triangles[index + 4] = vertex + 3;
                triangles[index + 5] = vertex + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        GameObject ring = new GameObject(label);
        ring.transform.SetParent(worldRoot, false);
        ring.transform.localPosition = position;
        MeshFilter filter = ring.AddComponent<MeshFilter>();
        filter.sharedMesh = mesh;
        MeshRenderer renderer = ring.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
    }

    private void CreateSpaceBody(Vector3 position, Vector3 scale, string label)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = label;
        body.transform.SetParent(worldRoot, false);
        body.transform.localPosition = position;
        body.transform.localScale = scale;
        body.GetComponent<Renderer>().sharedMaterial = planetMaterial;
        Destroy(body.GetComponent<Collider>());
    }

    private void BuildFloorGrid(float minX, float maxX, float minZ, float maxZ, string section)
    {
        for (float x = minX; x <= maxX; x += 6f)
        {
            for (float z = minZ; z <= maxZ; z += 6f)
            {
                GameObject tile = PlaceKitModule(floorTilePrefab, new Vector3(x, 0.012f, z), 0f,
                    section + " modular floor " + x + " / " + z, false, true);
                AddMeshColliders(tile);
            }
        }
    }

    private void BuildRoofGrid(float minX, float maxX, float minZ, float maxZ, string section)
    {
        for (float x = minX; x <= maxX; x += 6f)
        {
            for (float z = minZ; z <= maxZ; z += 6f)
            {
                GameObject roof = PlaceKitModule(roofTilePrefab, new Vector3(x, 4.1f, z), 0f,
                    section + " modular ceiling " + x + " / " + z, false, false);
                AddMeshColliders(roof);
            }
        }
    }

    private void BuildModularAnnex()
    {
        BuildRoofGrid(35f, 83f, -18f, 18f, "Annex");

        AddWall(30f, 20f, 4f, true);
        AddWall(40f, 20f, 8f, true);
        AddWall(52f, 20f, 8f, true);
        AddWall(64f, 20f, 8f, true);
        AddWall(76f, 20f, 8f, true);
        AddWall(30f, -20f, 4f, true);
        AddWall(40f, -20f, 8f, true);
        AddWall(52f, -20f, 8f, true);
        AddWall(64f, -20f, 8f, true);
        AddWall(76f, -20f, 8f, true);
        AddWall(80f, 23f, 6f, false);
        AddWall(80f, -23f, 6f, false);
        AddWall(80f, 12f, 16f, false);
        AddWall(80f, -12f, 16f, false);

        AddWall(41f, 6f, 14f, true);
        AddWall(59f, 6f, 10f, true);
        AddWall(74f, 6f, 22f, true);
        AddWall(41f, -6f, 14f, true);
        AddWall(59f, -6f, 10f, true);
        AddWall(74f, -6f, 22f, true);

        AddWall(42f, 13f, 14f, false);
        AddWall(60f, 7.25f, 1.5f, false);
        AddWall(60f, 19f, 2f, false);
        AddWall(51f, 20f, 18f, true);
        AddWall(45f, 6f, 6f, true);
        AddWall(57f, 6f, 6f, true);

        AddWall(42f, -13f, 14f, false);
        AddWall(60f, -7.25f, 1.5f, false);
        AddWall(60f, -19f, 2f, false);
        AddWall(51f, -20f, 18f, true);
        AddWall(45f, -6f, 6f, true);
        AddWall(57f, -6f, 6f, true);

        AddWall(62f, 12f, 8f, false);
        AddWall(62f, -12f, 8f, false);

        annexBulkhead = CreateBlastDoor("Power-controlled annex blast door", new Vector3(32f, 1.8f, 0f), 5.2f, 4f);
        airlockBulkhead = CreateBlastDoor("Power-controlled airlock blast door", new Vector3(79.4f, 1.8f, 0f), 5.2f, 6f);

        PlaceKitModule(doorFramePrefab, new Vector3(32f, 0f, 0f), 90f, "Modular engineering airlock", true, false);
        PlaceKitModule(doorFramePrefab, new Vector3(51f, 0f, 6f), 0f, "Modular hydroponics doorway", true, false);
        PlaceKitModule(doorFramePrefab, new Vector3(51f, 0f, -6f), 180f, "Modular maintenance doorway", true, false);
        PlaceKitModule(doorFramePrefab, new Vector3(62f, 0f, 0f), 90f, "Modular cargo doorway", true, false);
        PlaceKitModule(doorFramePrefab, new Vector3(79.4f, 0f, 0f), 90f, "Modular escape airlock", true, false);

        BuildSpaceWindow(34f, 20f, 1f, "Hydroponics observation window");
        BuildSpaceWindow(46f, 20f, 1f, "Hydroponics observation window");
        BuildSpaceWindow(58f, 20f, 1f, "Cargo observation window");
        BuildSpaceWindow(70f, 20f, 1f, "Cargo observation window");
        BuildSpaceWindow(34f, -20f, -1f, "Maintenance observation window");
        BuildSpaceWindow(46f, -20f, -1f, "Maintenance observation window");
        BuildSpaceWindow(58f, -20f, -1f, "Cargo observation window");
        BuildSpaceWindow(70f, -20f, -1f, "Cargo observation window");
        PlaceKitModule(pipeWallPrefab, new Vector3(40.5f, 0f, 12f), 90f, "Hydroponics life-support column", true, false);
        PlaceKitModule(pipeWallPrefab, new Vector3(40.5f, 0f, -12f), 90f, "Maintenance coolant column", true, false);
        PlaceKitModule(consolePrefab, new Vector3(57.5f, 0.25f, 17.25f), 180f, "Hydroponics environmental controls", true, false);
        PlaceKitModule(consolePrefab, new Vector3(57.5f, 0.25f, -17.25f), 0f, "Maintenance environmental controls", true, false);
        PlaceKitModule(gearPrefab, new Vector3(62.5f, 0f, 8f), 90f, "Cargo bay drive assembly", true, false);
        PlaceKitModule(gearPrefab, new Vector3(62.5f, 0f, -8f), 90f, "Cargo bay drive assembly", true, false);
        PlaceKitModule(pipesPrefab, new Vector3(52f, 3.55f, 0f), 0f, "Main annex pipework", true, false);
        PlaceKitModule(airGridPrefab, new Vector3(46f, 0f, 13f), 90f, "Hydroponics ventilation", true, false);
        PlaceKitModule(airGridPrefab, new Vector3(46f, 0f, -13f), 90f, "Maintenance ventilation", true, false);
        PlaceKitModule(overheadGridPrefab, new Vector3(64f, 3.55f, 11f), 0f, "Cargo bay overhead gantry", true, false);
        PlaceKitModule(overheadGridPrefab, new Vector3(64f, 3.55f, -11f), 180f, "Cargo bay overhead gantry", true, false);

        for (int x = 40; x <= 76; x += 12)
        {
            PlaceKitModule(intercomPrefab, new Vector3(x, 1.25f, 3.55f), 0f, "Emergency intercom", true, false);
        }

        kitCrates[0] = PlaceKitModule(cargoCratePrefab, new Vector3(68f, 0.08f, 10f), 0f, "Cargo container - Echo crates", true, false);
        kitCrates[1] = PlaceKitModule(cargoCratePrefab, new Vector3(72f, 0.08f, 10f), 90f, "Cargo container - Echo crates", true, false);
        kitCrates[2] = PlaceKitModule(cargoCratePrefab, new Vector3(68f, 0.08f, -10f), 180f, "Cargo container - Echo crates", true, false);
        kitCrates[3] = PlaceKitModule(cargoCratePrefab, new Vector3(72f, 0.08f, -10f), 270f, "Cargo container - Echo crates", true, false);
        for (int i = 0; i < kitCrates.Length; i++)
        {
            BoxCollider crateCollider = kitCrates[i].AddComponent<BoxCollider>();
            crateCollider.center = new Vector3(0f, 0.65f, 0f);
            crateCollider.size = new Vector3(1.7f, 1.3f, 1.7f);
        }

        CreateSign("RELAY  /  A", new Vector3(52f, 3.15f, 17.9f), 180f);
        CreateSign("RELAY  /  B", new Vector3(52f, 3.15f, -17.9f), 0f);
        CreateSign("COMMS  /  C", new Vector3(72f, 3.15f, 17.9f), 180f);
        CreateSign("SIGNAL ROUTE  //  3-1-2", new Vector3(46f, 2.8f, 17.9f), 180f);
    }

    private GameObject PlaceKitModule(GameObject prefab, Vector3 position, float rotation, string label, bool pulseReveal, bool solid)
    {
        GameObject module = Instantiate(prefab, worldRoot);
        module.transform.localPosition = position;
        module.transform.localRotation = Quaternion.Euler(0f, rotation, 0f);
        module.name = label;
        if (pulseReveal)
        {
            EchoPulseReveal reveal = module.AddComponent<EchoPulseReveal>();
            reveal.Prepare();
            pulseRevealTargets.Add(reveal);
        }

        if (!solid)
        {
            Collider[] colliders = module.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
                Destroy(colliders[i]);
        }

        return module;
    }

    private static void AddMeshColliders(GameObject module)
    {
        MeshFilter[] meshFilters = module.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshFilters.Length; i++)
        {
            MeshFilter meshFilter = meshFilters[i];
            if (meshFilter.sharedMesh == null)
                continue;

            MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
            if (meshCollider == null)
                meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }
    }

    private void BuildDecor()
    {
        for (int x = -28; x <= 30; x += 4)
        {
            MakeBlock("Floor seam", new Vector3(x, 0.008f, 0f), new Vector3(0.035f, 0.012f, 7.7f), trimMaterial, false);
        }

        for (int z = -18; z <= 18; z += 4)
        {
            MakeBlock("Floor seam", new Vector3(-9f, 0.01f, z), new Vector3(7.7f, 0.012f, 0.035f), trimMaterial, false);
            MakeBlock("Floor seam", new Vector3(7f, 0.01f, z), new Vector3(11.5f, 0.012f, 0.035f), trimMaterial, false);
        }

        BuildReactorConsole();
        MakeBlock("Escape console", new Vector3(77f, 1.1f, -2.6f), new Vector3(0.6f, 1.5f, 1.2f), wallMaterial, true);

        for (int i = 0; i < reactorCellIndicators.Length; i++)
        {
            GameObject cell = Instantiate(powerCellPrefabs[i % powerCellPrefabs.Length], worldRoot, false);
            cell.name = "Reactor cell socket " + (i + 1);
            cell.transform.localPosition = new Vector3(18.2f + i * 0.8f, 1.13f, -1.9f);
            cell.transform.localRotation = Quaternion.Euler(0f, i * 35f, 0f);
            cell.transform.localScale = Vector3.one * 0.62f;
            ApplyEnergyCellMaterials(cell);
            Renderer[] renderers = cell.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new System.InvalidOperationException("The imported reactor cell prefab has no renderers.");

            Bounds cellBounds = renderers[0].bounds;
            for (int rendererIndex = 1; rendererIndex < renderers.Length; rendererIndex++)
                cellBounds.Encapsulate(renderers[rendererIndex].bounds);
            cell.transform.position += Vector3.up * (0.94f - cellBounds.min.y);
            reactorSocketIndicators[i] = CreateReactorSocket(i);
            cell.SetActive(collectedCores[i]);
            reactorCellIndicators[i] = cell;
        }
    }

    private void BuildReactorConsole()
    {
        MakeBlock("Reactor console backplate", new Vector3(19f, 1.62f, -2.35f),
            new Vector3(3.2f, 2.05f, 0.28f), wallMaterial, true);
        MakeBlock("Reactor console lower shelf", new Vector3(19f, 0.48f, -1.9f),
            new Vector3(3.2f, 0.28f, 0.9f), trimMaterial, true);
        MakeBlock("Reactor console status strip", new Vector3(19f, 2.45f, -2.16f),
            new Vector3(2.6f, 0.08f, 0.06f), brightMaterial, false);
        CreateSign("REACTOR  /  CELL ARRAY", new Vector3(19f, 2.78f, -2.14f), 0f);
    }

    private GameObject CreateReactorSocket(int index)
    {
        float x = 18.2f + index * 0.8f;
        GameObject socket = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        socket.name = "Reactor cell receptacle " + (index + 1);
        socket.transform.SetParent(worldRoot, false);
        socket.transform.localPosition = new Vector3(x, 0.79f, -1.93f);
        socket.transform.localScale = new Vector3(0.68f, 0.09f, 0.68f);
        socket.GetComponent<Renderer>().sharedMaterial = wallMaterial;

        GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        indicator.name = "Reactor receptacle status " + (index + 1);
        indicator.transform.SetParent(worldRoot, false);
        indicator.transform.localPosition = new Vector3(x, 0.89f, -1.93f);
        indicator.transform.localScale = new Vector3(0.48f, 0.025f, 0.48f);
        indicator.GetComponent<Renderer>().sharedMaterial = deadCoreMaterial;
        Destroy(socket.GetComponent<Collider>());
        Destroy(indicator.GetComponent<Collider>());
        return indicator;
    }

    private void SetReactorSocketPowered(int index, bool powered)
    {
        GameObject indicator = reactorSocketIndicators[index];
        if (indicator != null)
            indicator.GetComponent<Renderer>().sharedMaterial = powered ? coreMaterial : deadCoreMaterial;
    }

    private void SetReactorWireConnected(int index, bool connected)
    {
        if (reactorWires[index] != null)
        {
            reactorWires[index].sharedMaterial = connected ? coreMaterial : reactorWireMaterial;
            Color wireColor = connected ? new Color(0.18f, 0.9f, 1f) : new Color(0.13f, 0.18f, 0.22f);
            reactorWires[index].startColor = wireColor;
            reactorWires[index].endColor = wireColor;
        }

        if (reactorWireConnectors[index] != null)
            reactorWireConnectors[index].GetComponent<Renderer>().sharedMaterial =
                connected ? coreMaterial : deadCoreMaterial;
    }

    private bool AllReactorWiresConnected()
    {
        for (int i = 0; i < reactorWiresConnected.Length; i++)
            if (!reactorWiresConnected[i])
                return false;
        return true;
    }

    private int CountConnectedReactorWires()
    {
        int count = 0;
        for (int i = 0; i < reactorWiresConnected.Length; i++)
            if (reactorWiresConnected[i])
                count++;
        return count;
    }

    private void ApplyEnergyCellMaterials(GameObject cell)
    {
        Renderer[] renderers = cell.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Material[] sourceMaterials = renderers[rendererIndex].sharedMaterials;
            Material[] convertedMaterials = new Material[sourceMaterials.Length];
            for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
            {
                Material source = sourceMaterials[materialIndex];
                if (source == null)
                    throw new System.InvalidOperationException("The imported energy-cell prefab contains a missing material.");

                Material converted;
                if (!convertedCellMaterials.TryGetValue(source, out converted))
                {
                    converted = CreateEnergyCellMaterial(source);
                    convertedCellMaterials.Add(source, converted);
                }

                convertedMaterials[materialIndex] = converted;
            }

            renderers[rendererIndex].sharedMaterials = convertedMaterials;
        }
    }

    private Material CreateEnergyCellMaterial(Material source)
    {
        Material converted = new Material(materialTemplate);
        CopyCellTexture(source, converted, "_MainTex", "_BaseMap");
        CopyCellTexture(source, converted, "_BumpMap", "_BumpMap");
        CopyCellTexture(source, converted, "_MetallicGlossMap", "_MetallicGlossMap");
        CopyCellTexture(source, converted, "_OcclusionMap", "_OcclusionMap");
        CopyCellTexture(source, converted, "_EmissionMap", "_EmissionMap");

        Color tint = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
        bool glass = source.name.IndexOf("glass", System.StringComparison.OrdinalIgnoreCase) >= 0;
        if (glass)
            tint.a = Mathf.Min(tint.a, 0.48f);
        if (converted.HasProperty("_BaseColor"))
            converted.SetColor("_BaseColor", tint);

        if (source.HasProperty("_Metallic") && converted.HasProperty("_Metallic"))
            converted.SetFloat("_Metallic", source.GetFloat("_Metallic"));
        if (source.HasProperty("_Glossiness") && converted.HasProperty("_Smoothness"))
            converted.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
        if (source.HasProperty("_OcclusionStrength") && converted.HasProperty("_OcclusionStrength"))
            converted.SetFloat("_OcclusionStrength", source.GetFloat("_OcclusionStrength"));
        if (source.HasProperty("_EmissionColor") && converted.HasProperty("_EmissionColor"))
        {
            converted.EnableKeyword("_EMISSION");
            converted.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
        }
        if (converted.GetTexture("_BumpMap") != null)
            converted.EnableKeyword("_NORMALMAP");
        if (converted.GetTexture("_MetallicGlossMap") != null)
            converted.EnableKeyword("_METALLICSPECGLOSSMAP");

        if (glass)
        {
            converted.SetFloat("_Surface", 1f);
            converted.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            converted.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            converted.SetFloat("_ZWrite", 0f);
            converted.SetOverrideTag("RenderType", "Transparent");
            converted.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            converted.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        }

        return converted;
    }

    private static void CopyCellTexture(Material source, Material destination, string sourceProperty, string destinationProperty)
    {
        if (!source.HasProperty(sourceProperty) || !destination.HasProperty(destinationProperty))
            return;

        Texture texture = source.GetTexture(sourceProperty);
        if (texture == null)
            return;

        destination.SetTexture(destinationProperty, texture);
        destination.SetTextureScale(destinationProperty, source.GetTextureScale(sourceProperty));
        destination.SetTextureOffset(destinationProperty, source.GetTextureOffset(sourceProperty));
    }

    private void AddWall(float x, float z, float length, bool alongX)
    {
        GameObject wall = PlaceFittedModule(wallModulePrefab, new Vector3(x, 2f, z), alongX,
            length, 4f, "Creepy Cat modular bulkhead");
        AddMeshColliders(wall);
    }

    private GameObject PlaceFittedModule(GameObject prefab, Vector3 center, bool alongX, float length, float height, string label)
    {
        GameObject module = Instantiate(prefab, worldRoot);
        module.transform.localPosition = Vector3.zero;
        module.transform.localRotation = Quaternion.identity;
        module.transform.localScale = Vector3.one;
        module.name = label;

        Bounds bounds = GetRendererBounds(module);
        Vector3 sourceCenter = worldRoot.InverseTransformPoint(bounds.center);
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
        return module;
    }

    private Bounds GetRendererBounds(GameObject module)
    {
        Renderer[] renderers = module.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new System.InvalidOperationException("The modular asset '" + module.name + "' has no renderers.");

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private void AddLight(Vector3 position, float range, Color color)
    {
        GameObject fixture = PlaceKitModule(corridorLightPrefab,
            new Vector3(position.x, 2.6f, position.z), 0f, "Creepy Cat corridor light", false, false);
        Light[] lights = fixture.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].enabled = true;
            lights[i].color = color;
            lights[i].range = Mathf.Max(lights[i].range, range);
            lights[i].intensity = 0.72f;
            lights[i].shadows = LightShadows.None;
            corridorLights.Add(lights[i]);
        }

        if (lights.Length == 0)
        {
            Light light = fixture.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = 0.72f;
            light.shadows = LightShadows.None;
            corridorLights.Add(light);
        }
    }

    private GameObject CreateBlastDoor(string name, Vector3 position, float width, float corridorHalfWidth)
    {
        GameObject door = PlaceFittedModule(doorPanelPrefab, position, false, width, 3.6f, name);
        BoxCollider doorCollider = door.GetComponent<BoxCollider>();
        if (doorCollider == null)
            doorCollider = door.AddComponent<BoxCollider>();
        doorCollider.center = Vector3.zero;
        Vector3 moduleScale = door.transform.localScale;
        bool widthRunsAlongLocalX =
            Mathf.Abs((door.transform.localRotation * Vector3.right).z) >
            Mathf.Abs((door.transform.localRotation * Vector3.forward).z);
        float localWidth = (width + 0.1f) /
            Mathf.Abs(widthRunsAlongLocalX ? moduleScale.x : moduleScale.z);
        float localThickness = 0.7f /
            Mathf.Abs(widthRunsAlongLocalX ? moduleScale.z : moduleScale.x);
        doorCollider.size = widthRunsAlongLocalX
            ? new Vector3(localWidth, 3.6f / Mathf.Abs(moduleScale.y), localThickness)
            : new Vector3(localThickness, 3.6f / Mathf.Abs(moduleScale.y), localWidth);
        door.AddComponent<EchoBlastDoor>().Initialize();

        float sideGap = corridorHalfWidth - width * 0.5f;
        if (sideGap > 0.1f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                MakeBlock(name + " / sealed side gap",
                    new Vector3(position.x, 1.8f, side * (width * 0.5f + sideGap * 0.5f)),
                    new Vector3(0.7f, 3.6f, sideGap + 0.08f), wallMaterial, true);
            }
        }

        return door;
    }

    private GameObject MakeBlock(string name, Vector3 position, Vector3 scale, Material material, bool solid)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(worldRoot, false);
        block.transform.localPosition = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid)
            Destroy(block.GetComponent<Collider>());
        return block;
    }

    private void BuildPlayer()
    {
        player = new GameObject("You - Echo operator");
        player.transform.SetParent(worldRoot, false);
        player.transform.localPosition = SpawnPoint;
        characterController = player.AddComponent<CharacterController>();
        characterController.height = 1.75f;
        characterController.radius = 0.34f;
        characterController.center = new Vector3(0f, 0.9f, 0f);
        characterController.slopeLimit = 45f;
        characterController.stepOffset = 0.3f;
        playerMotor = player.AddComponent<EchoPlayerMotor>();
        playerMotor.Initialize(this, characterController);

        GameObject cameraObject = new GameObject("First-person camera");
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        cameraObject.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        playerCamera = cameraObject.AddComponent<Camera>();
        playerCamera.tag = "MainCamera";
        playerCamera.fieldOfView = 78f;
        playerCamera.nearClipPlane = 0.08f;
        playerCamera.farClipPlane = 90f;
        playerCamera.clearFlags = CameraClearFlags.Skybox;
        playerCamera.backgroundColor = new Color(0.005f, 0.012f, 0.02f);
        cameraObject.AddComponent<AudioListener>();
        playerMotor.SetCamera(playerCamera);

        GameObject flashlight = new GameObject("Dying flashlight");
        flashlight.transform.SetParent(cameraObject.transform, false);
        flashlight.transform.localPosition = new Vector3(0.08f, -0.08f, 0.18f);
        Light torch = flashlight.AddComponent<Light>();
        torch.type = LightType.Spot;
        torch.color = new Color(0.64f, 0.88f, 1f);
        torch.range = 13f;
        torch.spotAngle = 58f;
        torch.intensity = 2.2f;
        torch.shadows = LightShadows.Soft;
        playerMotor.SetFlashlight(torch);

        EchoPulse pulse = player.AddComponent<EchoPulse>();
        pulse.Bind(playerCamera);
    }

    private void BuildCreature()
    {
        GameObject creatureObject = new GameObject("The Listener");
        creatureObject.transform.SetParent(worldRoot, false);
        creatureObject.transform.localPosition = new Vector3(21f, 0f, 0f);
        creature = creatureObject.AddComponent<EchoCreature>();
        creature.Initialize(this, monsterPrefab, materialTemplate);
    }

    private void BuildEscapeShip()
    {
        escapeShip = Instantiate(escapeShipPrefab, worldRoot, false);
        escapeShip.name = "StarSparrow - escape craft";
        escapeShip.transform.localPosition = new Vector3(87.5f, 0.15f, 0f);
        escapeShip.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        escapeShipStartPosition = escapeShip.transform.localPosition;
        escapeShipStartRotation = escapeShip.transform.localRotation;
        escapeShipStartScale = escapeShip.transform.localScale;

        Collider[] colliders = escapeShip.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;
    }

    private void AddInteractable(string kind, int index, string label, Vector3 position, bool isCore)
    {
        GameObject target = new GameObject(kind == "CORE" ? "Resonance power core" : label);
        target.transform.SetParent(worldRoot, false);
        target.transform.localPosition = position;
        EchoInteractable interactable = target.AddComponent<EchoInteractable>();
        interactable.Initialize(kind, index, label);
        interactables.Add(interactable);

        if (isCore)
        {
            GameObject cell = Instantiate(powerCellPrefabs[index % powerCellPrefabs.Length], target.transform, false);
            cell.name = index == 2 ? "Power cell / recovered from the floor" : "Power cell / dropped on the deck";
            cell.transform.localPosition = Vector3.zero;
            cell.transform.localRotation = Quaternion.Euler(index == 0 ? 88f : index == 1 ? 72f : 76f, index * 71f, 18f);
            cell.transform.localScale = Vector3.one * 0.8f;
            ApplyEnergyCellMaterials(cell);
            Renderer[] cellRenderers = cell.GetComponentsInChildren<Renderer>(true);
            if (cellRenderers.Length == 0)
                throw new System.InvalidOperationException("The imported power-cell prefab has no renderers.");

            Bounds cellBounds = cellRenderers[0].bounds;
            for (int i = 1; i < cellRenderers.Length; i++)
                cellBounds.Encapsulate(cellRenderers[i].bounds);
            cell.transform.position += Vector3.up * (0.035f - cellBounds.min.y);

            Collider[] cellColliders = cell.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cellColliders.Length; i++)
                Destroy(cellColliders[i]);

            Light glow = target.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = new Color(0.08f, 0.78f, 0.95f);
            glow.range = 2.8f;
            glow.intensity = 0.85f;
            glow.shadows = LightShadows.None;
        }
        else
        {
            SphereCollider trigger = target.AddComponent<SphereCollider>();
            trigger.radius = 2.2f;
            trigger.isTrigger = true;

            if (kind == "RELAY")
            {
                GameObject emitter = GameObject.CreatePrimitive(PrimitiveType.Cube);
                emitter.name = "Relay carrier emitter";
                emitter.transform.SetParent(target.transform, false);
                emitter.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                emitter.transform.localScale = new Vector3(0.52f, 0.62f, 0.52f);
                emitter.GetComponent<Renderer>().sharedMaterial = creatureMaterial;
                Destroy(emitter.GetComponent<Collider>());
                Light beacon = emitter.AddComponent<Light>();
                beacon.type = LightType.Point;
                beacon.range = 4.5f;
                beacon.color = new Color(1f, 0.07f, 0.025f);
                beacon.intensity = 0.7f;
                beacon.shadows = LightShadows.None;
                relayIndicators[index] = emitter;

                GameObject basePlate = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                basePlate.name = "Relay stand";
                basePlate.transform.SetParent(target.transform, false);
                basePlate.transform.localPosition = new Vector3(0f, 0.22f, 0f);
                basePlate.transform.localScale = new Vector3(1.05f, 0.22f, 1.05f);
                basePlate.GetComponent<Renderer>().sharedMaterial = wallMaterial;
                Destroy(basePlate.GetComponent<Collider>());

                PlaceKitModule(intercomPrefab, position + Vector3.up * 1.1f, 180f, "Kit relay control terminal", true, false);
            }
            else if (kind == "LOG" || kind == "NOTE" || kind == "PUZZLE" || kind == "COMMS" || kind == "EXIT")
            {
                GameObject console = kind == "LOG" || kind == "NOTE" ? intercomPrefab : consolePrefab;
                PlaceKitModule(console, position + Vector3.up, 180f, "Kit " + label, true, false);
            }
            else if (kind == "SUPPLY")
            {
                GameObject supplyCase = PlaceKitModule(cargoCratePrefab, position + Vector3.up * 0.12f, 0f, "Emergency supply case", true, false);
                supplyCase.transform.SetParent(target.transform, true);
                BoxCollider caseCollider = supplyCase.AddComponent<BoxCollider>();
                caseCollider.center = new Vector3(0f, 0.55f, 0f);
                caseCollider.size = new Vector3(1.4f, 1.1f, 1.4f);
            }
            else if (kind == "WIRE")
            {
                LineRenderer wire = target.AddComponent<LineRenderer>();
                wire.useWorldSpace = true;
                wire.positionCount = 4;
                wire.startWidth = 0.055f;
                wire.endWidth = 0.055f;
                wire.sharedMaterial = reactorWireMaterial;
                wire.startColor = new Color(0.13f, 0.18f, 0.22f);
                wire.endColor = wire.startColor;
                wire.numCapVertices = 4;
                wire.numCornerVertices = 4;
                wire.SetPosition(0, worldRoot.TransformPoint(new Vector3(position.x, 1.03f, -1.88f)));
                wire.SetPosition(1, worldRoot.TransformPoint(new Vector3(position.x, 1.27f, -2.13f)));
                wire.SetPosition(2, worldRoot.TransformPoint(new Vector3(19f, 1.27f, -2.13f)));
                wire.SetPosition(3, worldRoot.TransformPoint(new Vector3(19f, 1.85f, -2.1f)));
                reactorWires[index] = wire;

                GameObject connector = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                connector.name = "Reactor lead connector " + (index + 1);
                connector.transform.SetParent(target.transform, false);
                connector.transform.localPosition = new Vector3(0f, 0.12f, 0.16f);
                connector.transform.localScale = new Vector3(0.16f, 0.08f, 0.16f);
                connector.GetComponent<Renderer>().sharedMaterial = deadCoreMaterial;
                Destroy(connector.GetComponent<Collider>());
                reactorWireConnectors[index] = connector;
            }
        }
    }

    private void CreateSign(string text, Vector3 position, float rotation)
    {
        GameObject sign = new GameObject("Deck stencil");
        sign.transform.SetParent(worldRoot, false);
        sign.transform.localPosition = position;
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
        renderer.sharedMaterial = trimMaterial;
    }

    private void Update()
    {
        if (state == GameState.Title)
        {
            titleAnim += Time.unscaledDeltaTime;
            if (Pressed(Key.Enter) || Pressed(Key.Space))
                StartGame();
            return;
        }

        if (state == GameState.Ending)
        {
            escapeFlightTime += Time.deltaTime;
            if (blackHoleEntryTime >= 0f)
            {
                blackHoleEntryTime += Time.deltaTime;
                escapeShip.transform.localPosition = Vector3.Lerp(escapeShip.transform.localPosition,
                    blackHolePosition, Time.deltaTime * 2.4f);
                escapeShip.transform.localScale = Vector3.Lerp(escapeShip.transform.localScale,
                    Vector3.zero, Time.deltaTime * 1.15f);
            }
            else if (escapeShip != null)
            {
                UpdateEscapeFlight();
            }

            if (blackHoleEntryTime >= 2.6f && (Pressed(Key.Enter) || Pressed(Key.Space)))
                RestartGame();
            return;
        }

        if (state == GameState.Dead)
        {
            if (Pressed(Key.Enter) || Pressed(Key.Space))
                RestartGame();
            return;
        }

        if (Pressed(Key.Escape))
        {
            if (state == GameState.Playing)
            {
                PauseGame();
                return;
            }
            else if (state == GameState.Paused)
            {
                ResumeGame();
                return;
            }
            else if (state == GameState.Puzzle || state == GameState.SignalPuzzle || state == GameState.Choice)
            {
                state = GameState.Playing;
                playerMotor.SetControlsEnabled(true);
                SetCursor(true);
                return;
            }
        }

        if (state == GameState.Paused)
            return;

        if (state == GameState.Puzzle || state == GameState.SignalPuzzle)
        {
            if (state == GameState.Puzzle)
                UpdatePuzzleInput();
            else
                UpdateSignalPuzzleInput();
            return;
        }

        if (state == GameState.Choice)
        {
            if (Pressed(Key.Digit1) || Pressed(Key.Numpad1))
                ChooseEnding(true);
            else if (Pressed(Key.Digit2) || Pressed(Key.Numpad2))
                ChooseEnding(false);
            return;
        }

        if (state != GameState.Playing)
            return;

        activeInteractable = FindNearbyInteractable();
        if (activeInteractable != null && Pressed(Key.E))
            Interact(activeInteractable);

        if (Pressed(Key.F))
            ToggleFlashlight();

        isCrouching = Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
        if (flashlightOn)
        {
            battery = Mathf.Max(0f, battery - Time.deltaTime * (IsInCoolantLeak() ? 0.34f : 0.13f));
            if (battery <= 0f)
            {
                flashlightOn = false;
                playerMotor.SetFlashlightActive(false);
                ShowMessage("Flashlight battery depleted.");
            }
        }

        if (IsInCoolantLeak() && Time.time >= statusUntil)
            ShowMessage("COOLANT LEAK  //  flashlight charge is draining fast.");

        float distance = Vector3.Distance(player.transform.localPosition, creature.transform.localPosition);
        UpdateThreatEffects(distance);
        if (distance < 1.45f)
            Die();
    }

    private static bool Pressed(Key key)
    {
        return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
    }

    private void StartGame()
    {
        state = GameState.Playing;
        SetCursor(true);
        ShowMessage("Spawn hatch is open. Recover the three power cells and return them to the reactor.");
        if (playerMotor != null)
            playerMotor.SetControlsEnabled(true);
    }

    private void PauseGame()
    {
        state = GameState.Paused;
        Time.timeScale = 0f;
        SetCursor(false);
    }

    private void ResumeGame()
    {
        state = GameState.Playing;
        Time.timeScale = 1f;
        SetCursor(true);
    }

    private void RestartGame()
    {
        Time.timeScale = 1f;
        if (player.transform.parent != worldRoot)
            player.transform.SetParent(worldRoot, false);
        if (escapeShip != null)
        {
            escapeShip.transform.SetParent(worldRoot, false);
            escapeShip.transform.localPosition = escapeShipStartPosition;
            escapeShip.transform.localRotation = escapeShipStartRotation;
            escapeShip.transform.localScale = escapeShipStartScale;
        }
        escapeFlightTime = 0f;
        escapeShipSpeed = 8f;
        blackHoleEntryTime = -1f;
        coresFound = 0;
        battery = FlashlightCapacity;
        powerRestored = false;
        signalCalibrated = false;
        relayCount = 0;
        logsFound = 0;
        flashlightOn = true;
        puzzleInput.Clear();
        signalInput.Clear();
        for (int i = 0; i < collectedCores.Length; i++)
        {
            collectedCores[i] = false;
            interactables[i].gameObject.SetActive(true);
            if (reactorCellIndicators[i] != null)
                reactorCellIndicators[i].SetActive(false);
            SetReactorSocketPowered(i, false);
        }
        for (int i = 0; i < reactorWiresConnected.Length; i++)
        {
            reactorWiresConnected[i] = false;
            SetReactorWireConnected(i, false);
        }

        for (int i = 0; i < activatedRelays.Length; i++)
        {
            activatedRelays[i] = false;
            collectedLogs[i] = false;
            collectedSupplies[i] = false;
        }

        for (int i = 3; i < interactables.Count; i++)
        {
            EchoInteractable item = interactables[i];
            if (item.Kind == "RELAY" || item.Kind == "LOG" || item.Kind == "SUPPLY")
                item.gameObject.SetActive(true);
        }

        SetRelayIndicators(false);

        foreach (Light light in corridorLights)
            light.intensity = 0.72f;

        activeInteractable = null;
        player.transform.localPosition = SpawnPoint;
        player.transform.rotation = Quaternion.identity;
        playerMotor.ResetMotor();
        playerMotor.SetFlashlightActive(true);
        creature.ResetCreature();
        SetBlastDoorOpen(annexBulkhead, false);
        SetBlastDoorOpen(airlockBulkhead, false);
        for (int i = 0; i < kitCrates.Length; i++)
            kitCrates[i].SetActive(true);
        state = GameState.Playing;
        SetCursor(true);
        ShowMessage("Find three power cells. Their lit slots mark your progress at the reactor.");
    }

    private void Die()
    {
        if (state != GameState.Playing)
            return;

        creature.PrepareRespawnAfterDeath(player.transform.localPosition);
        state = GameState.Dead;
        playerMotor.SetControlsEnabled(false);
        SetCursor(false);
    }

    private EchoInteractable FindNearbyInteractable()
    {
        EchoInteractable closest = null;
        float bestDistance = 2.6f;
        for (int i = 0; i < interactables.Count; i++)
        {
            EchoInteractable item = interactables[i];
            if (item == null || !item.gameObject.activeInHierarchy)
                continue;

            float distance = Vector3.Distance(player.transform.localPosition, item.transform.localPosition);
            if (distance < bestDistance)
            {
                closest = item;
                bestDistance = distance;
            }
        }

        return closest;
    }

    private void Interact(EchoInteractable item)
    {
        if (item.Kind == "CORE")
        {
            if (collectedCores[item.Index])
                return;

            collectedCores[item.Index] = true;
            item.gameObject.SetActive(false);
            if (reactorCellIndicators[item.Index] != null)
                reactorCellIndicators[item.Index].SetActive(true);
            SetReactorSocketPowered(item.Index, true);
            coresFound++;
            battery = Mathf.Min(FlashlightCapacity, battery + 18f);
            ShowMessage("POWER CELL SECURED  //  Reactor slot " + coresFound + " / 3 energized. Flashlight charge restored.");
            return;
        }

        if (item.Kind == "WIRE")
        {
            if (!collectedCores[item.Index])
            {
                ShowMessage("Reactor lead " + (item.Index + 1) + " is waiting for its power cell.");
                return;
            }

            if (reactorWiresConnected[item.Index])
            {
                ShowMessage("Reactor lead " + (item.Index + 1) + " is already connected.");
                return;
            }

            reactorWiresConnected[item.Index] = true;
            SetReactorWireConnected(item.Index, true);
            int connectedCount = 0;
            for (int i = 0; i < reactorWiresConnected.Length; i++)
                if (reactorWiresConnected[i])
                    connectedCount++;

            ShowMessage("REACTOR LEAD " + (item.Index + 1) + " CONNECTED  //  " + connectedCount + " / 3 circuits live.");
            if (connectedCount == reactorWiresConnected.Length)
                ShowMessage("ALL REACTOR LEADS ARE CONNECTED. Enter the breaker code: 2, 3, 1.");
            return;
        }

        if (item.Kind == "NOTE")
        {
            ShowMessage(item.Index == 0
                ? "Maintenance note: reactor breaker route is 2  >  3  >  1."
                : "Crew note: long-range relay calibration is 3  >  1  >  2. Don't broadcast until it's aligned.");
            return;
        }

        if (item.Kind == "RELAY")
        {
            if (activatedRelays[item.Index])
            {
                ShowMessage("Relay " + (char)('A' + item.Index) + " is holding its carrier signal.");
                return;
            }

            activatedRelays[item.Index] = true;
            relayCount++;
            UpdateRelayIndicator(item.Index, true);
            creature.HearPulse(PlayerPosition);
            ShowMessage("RELAY " + (char)('A' + item.Index) + " LOCKED  //  Its carrier tone echoes through the deck.");
            if (relayCount == 3)
                ShowMessage("ALL THREE RELAYS ARE ONLINE. Calibrate the array in cargo control.");
            return;
        }

        if (item.Kind == "LOG")
        {
            if (collectedLogs[item.Index])
                ShowMessage("Recording already archived. Crew log " + (logsFound) + " / 3.");
            else
            {
                collectedLogs[item.Index] = true;
                logsFound++;
                ShowMessage(GetCrewRecording(item.Index));
            }

            return;
        }

        if (item.Kind == "SUPPLY")
        {
            if (collectedSupplies[item.Index])
                return;

            collectedSupplies[item.Index] = true;
            item.gameObject.SetActive(false);
            battery = Mathf.Min(FlashlightCapacity, battery + 24f);
            ShowMessage("Emergency battery pack collected. Charge replenished.");
            return;
        }

        if (item.Kind == "COMMS")
        {
            if (!powerRestored)
                ShowMessage("Cargo control is dark. Restore the reactor first.");
            else if (relayCount < 3)
                ShowMessage("The array needs all three carriers: " + relayCount + " / 3 relays.");
            else if (signalCalibrated)
                ShowMessage("The rescue carrier is calibrated and transmitting.");
            else
            {
                signalInput.Clear();
                state = GameState.SignalPuzzle;
                playerMotor.SetControlsEnabled(false);
                SetCursor(false);
            }

            return;
        }

        if (item.Kind == "PUZZLE")
        {
            if (powerRestored)
                ShowMessage("Reactor online. The annex blast door is open.");
            else if (coresFound < 3)
                ShowMessage("Breaker panel is dead. Cores required: " + coresFound + " / 3.");
            else if (!AllReactorWiresConnected())
                ShowMessage("The reactor has power cells, but its leads are still disconnected: " +
                    CountConnectedReactorWires() + " / 3.");
            else
            {
                puzzleInput.Clear();
                state = GameState.Puzzle;
                playerMotor.SetControlsEnabled(false);
                SetCursor(false);
            }

            return;
        }

        if (item.Kind == "EXIT")
        {
            if (!powerRestored)
                ShowMessage("Airlock sealed. Restore the reactor first.");
            else
            {
                state = GameState.Choice;
                playerMotor.SetControlsEnabled(false);
                SetCursor(false);
            }
        }
    }

    private bool IsInCoolantLeak()
    {
        Vector3 position = player.transform.localPosition;
        return powerRestored && position.x > 40f && position.x < 59f && position.z < -8f && position.z > -17f;
    }

    private static string GetCrewRecording(int index)
    {
        switch (index)
        {
            case 0:
                return "CREW LOG 01  //  The Listener follows the return tone. Never ping twice from the same place.";
            case 1:
                return "CREW LOG 02  //  The coolant purge killed the lights. It wasn't in the maintenance schedule.";
            default:
                return "CREW LOG 03  //  We heard a reply on the long-range array. Someone else is out there.";
        }
    }

    private void UpdateRelayIndicator(int index, bool enabled)
    {
        GameObject indicator = relayIndicators[index];
        if (indicator == null)
            return;

        Renderer indicatorRenderer = indicator.GetComponent<Renderer>();
        indicatorRenderer.sharedMaterial = enabled ? coreMaterial : creatureMaterial;
        Light indicatorLight = indicator.GetComponent<Light>();
        indicatorLight.color = enabled ? new Color(0.08f, 0.8f, 1f) : new Color(1f, 0.07f, 0.025f);
        indicatorLight.intensity = enabled ? 1.8f : 0.7f;
    }

    private void SetRelayIndicators(bool enabled)
    {
        for (int i = 0; i < relayIndicators.Length; i++)
            UpdateRelayIndicator(i, enabled);
    }

    private void UpdatePuzzleInput()
    {
        for (int i = 0; i < 3; i++)
        {
            Key digit = (Key)((int)Key.Digit1 + i);
            Key numpad = (Key)((int)Key.Numpad1 + i);
            if (!Pressed(digit) && !Pressed(numpad))
                continue;

            int entered = i + 1;
            if (entered != puzzleCode[puzzleInput.Count])
            {
                puzzleInput.Clear();
                creature.HearPulse(PlayerPosition);
                ShowMessage("Incorrect sequence. The breaker snaps back.");
                return;
            }

            puzzleInput.Add(entered);
            if (puzzleInput.Count == puzzleCode.Length)
                RestorePower();
            return;
        }

        if (Pressed(Key.Backspace))
            puzzleInput.Clear();
    }

    private void UpdateSignalPuzzleInput()
    {
        for (int i = 0; i < 3; i++)
        {
            Key digit = (Key)((int)Key.Digit1 + i);
            Key numpad = (Key)((int)Key.Numpad1 + i);
            if (!Pressed(digit) && !Pressed(numpad))
                continue;

            int entered = i + 1;
            if (entered != signalCode[signalInput.Count])
            {
                signalInput.Clear();
                creature.HearPulse(PlayerPosition);
                ShowMessage("Carrier slipped. Cargo control broadcasts a decoy tone.");
                return;
            }

            signalInput.Add(entered);
            if (signalInput.Count == signalCode.Length)
            {
                signalCalibrated = true;
                state = GameState.Playing;
                playerMotor.SetControlsEnabled(true);
                SetCursor(true);
                ShowMessage("RESCUE CARRIER LOCKED  //  Someone beyond the dark can hear you.");
            }

            return;
        }

        if (Pressed(Key.Backspace))
            signalInput.Clear();
    }

    private void RestorePower()
    {
        powerRestored = true;
        state = GameState.Playing;
        playerMotor.SetControlsEnabled(true);
        SetCursor(true);
        ShowMessage("Reactor restored. The annex door is opening.");
        SetBlastDoorOpen(annexBulkhead, true);
        foreach (Light light in corridorLights)
            light.intensity = 1.15f;
    }

    private void ChooseEnding(bool sendSignal)
    {
        state = GameState.Ending;
        playerMotor.SetControlsEnabled(false);
        escapeFlightTime = 0f;
        escapeShipSpeed = 8f;
        blackHoleEntryTime = -1f;
        if (sendSignal)
        {
            if (signalCalibrated)
            {
                endingTitle = "THE SIGNAL LIVES";
                endingBody = "Your calibrated beacon reaches a human voice. You board the StarSparrow and carry the crew's final signal into deep space.";
            }
            else
            {
                endingTitle = "A SIGNAL INTO THE DARK";
                endingBody = "Your dying carrier broadcasts the station coordinates. You board the StarSparrow and leave the unanswered beacon behind.";
            }
        }
        else
        {
            endingTitle = "SILENCE, AT LAST";
            endingBody = "You seal the airlock, board the StarSparrow, and launch alone. Behind you, three red lights vanish into the cold.";
        }
        SetBlastDoorOpen(airlockBulkhead, true);
        player.transform.SetParent(escapeShip.transform, false);
        player.transform.localPosition = new Vector3(0f, 0f, 2.2f);
        player.transform.localRotation = Quaternion.identity;
        playerCamera.transform.localRotation = Quaternion.identity;
    }

    private void UpdateEscapeFlight()
    {
        Keyboard keyboard = Keyboard.current;
        float throttle = keyboard != null && keyboard.wKey.isPressed ? 1f : 0f;
        float brake = keyboard != null && keyboard.sKey.isPressed ? 1f : 0f;
        float targetSpeed = Mathf.Lerp(10f, 25f, throttle);
        if (brake > 0f)
            targetSpeed = 3f;
        if (keyboard != null && keyboard.leftShiftKey.isPressed && throttle > 0f)
            targetSpeed = 32f;
        escapeShipSpeed = Mathf.MoveTowards(escapeShipSpeed, targetSpeed, Time.deltaTime * 8f);

        float steer = keyboard == null ? 0f :
            (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
        float pitch = keyboard == null ? 0f :
            (keyboard.upArrowKey.isPressed ? 1f : 0f) - (keyboard.downArrowKey.isPressed ? 1f : 0f);
        Vector3 toBlackHole = blackHolePosition - escapeShip.transform.localPosition;
        Quaternion targetRotation = Quaternion.LookRotation(toBlackHole.normalized, Vector3.up);
        targetRotation *= Quaternion.Euler(pitch * 12f, steer * 22f, -steer * 12f);
        escapeShip.transform.localRotation = Quaternion.Slerp(escapeShip.transform.localRotation,
            targetRotation, Time.deltaTime * 1.8f);
        escapeShip.transform.localPosition +=
            escapeShip.transform.localRotation * Vector3.forward * (escapeShipSpeed * Time.deltaTime);

        if (Vector3.Distance(escapeShip.transform.localPosition, blackHolePosition) < 11f)
            blackHoleEntryTime = 0f;
    }

    private void SetBlastDoorOpen(GameObject doorObject, bool open)
    {
        if (doorObject == null)
            return;

        EchoBlastDoor door = doorObject.GetComponent<EchoBlastDoor>();
        if (door == null)
            throw new System.InvalidOperationException("The blast-door model is missing EchoBlastDoor.");

        bool playSound = open && !door.IsOpen;
        door.SetOpen(open);
        if (playSound && doorClip != null)
            AudioSource.PlayClipAtPoint(doorClip, doorObject.transform.position, 0.8f);
    }

    private void BuildAudio()
    {
        ambienceSource = worldRoot.gameObject.AddComponent<AudioSource>();
        ambienceSource.clip = LoadStationClip("Ambiant_Loop");
        ambienceSource.loop = true;
        ambienceSource.volume = 0.2f;
        ambienceSource.spatialBlend = 0f;
        ambienceSource.playOnAwake = false;
        ambienceSource.Play();

        voiceSource = playerCamera.gameObject.AddComponent<AudioSource>();
        voiceSource.volume = 0.85f;
        voiceSource.spatialBlend = 0f;
        voiceSource.playOnAwake = false;
    }

    private void UpdateThreatEffects(float distance)
    {
        bool chasing = creature.IsInvestigating;
        int alertTier = !chasing ? 0 : distance < 4f ? 3 : distance < 8f ? 2 : distance < 15f ? 1 : 0;
        if (alertTier > lastVoiceAlertTier && Time.time >= nextVoiceAlertTime)
        {
            voiceSource.PlayOneShot(voiceAlerts[alertTier - 1]);
            ShowMessage(alertTier == 3
                ? "PROXIMITY ALERT  //  MOVE NOW."
                : alertTier == 2
                    ? "SECURITY WARNING  //  The Listener is closing in."
                    : "MOTION DETECTED  //  The Listener is searching nearby.");
            nextVoiceAlertTime = Time.time + 8f;
            lastVoiceAlertTier = alertTier;
        }

        if (distance > 18f)
            lastVoiceAlertTier = 0;

        float baseIntensity = powerRestored ? 1.15f : 0.72f;
        for (int i = 0; i < corridorLights.Count; i++)
        {
            Light light = corridorLights[i];
            if (light == null)
                continue;

            float monsterDistance = Vector3.Distance(
                worldRoot.InverseTransformPoint(light.transform.position), creature.transform.localPosition);
            float proximity = chasing ? Mathf.InverseLerp(18f, 4f, monsterDistance) : 0f;
            if (proximity <= 0.05f)
            {
                light.intensity = baseIntensity;
                continue;
            }

            float flicker = Mathf.PerlinNoise(Time.time * 14f, i * 0.73f);
            light.intensity = baseIntensity * (flicker > 0.61f ? 0.04f : flicker > 0.48f ? 0.3f : 1f);
        }
    }

    public void OnPulse()
    {
        if (state != GameState.Playing)
            return;

        creature.HearPulse(PlayerPosition);
        ShowMessage("SONAR PING  //  It heard you.");
    }

    public void ShowMessage(string message)
    {
        statusMessage = message;
        statusUntil = Time.time + 4f;
    }

    public bool IsCrouching { get { return isCrouching; } }

    public void RevealStation(Vector3 center, float radius, float duration)
    {
        for (int i = 0; i < pulseRevealTargets.Count; i++)
        {
            EchoPulseReveal target = pulseRevealTargets[i];
            if (target != null && Vector3.Distance(center, target.transform.position) <= radius)
                target.Reveal(duration);
        }
    }

    private void ToggleFlashlight()
    {
        if (battery <= 0f)
        {
            ShowMessage("The flashlight is dead.");
            return;
        }

        flashlightOn = !flashlightOn;
        playerMotor.SetFlashlightActive(flashlightOn);
    }

    private void SetCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void OnGUI()
    {
        EnsureStyles();
        switch (state)
        {
            case GameState.Title:
                DrawTitle();
                break;
            case GameState.Playing:
                DrawHud();
                break;
            case GameState.Puzzle:
                DrawPuzzle();
                break;
            case GameState.SignalPuzzle:
                DrawSignalPuzzle();
                break;
            case GameState.Choice:
                DrawEndingChoice();
                break;
            case GameState.Paused:
                DrawPause();
                break;
            case GameState.Dead:
                DrawEndCard("THE DARK FOUND YOU", "The station goes quiet. Every echo led it closer.", "ENTER  /  TRY AGAIN");
                break;
            case GameState.Ending:
                if (blackHoleEntryTime < 2.6f)
                    DrawEscapeFlight();
                else
                    DrawEndCard(endingTitle, endingBody, "ENTER  /  PLAY AGAIN");
                break;
        }
    }

    private bool GetNextObjective(out string label, out Vector3 position)
    {
        position = Vector3.zero;
        label = "";
        if (coresFound < collectedCores.Length)
        {
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < interactables.Count; i++)
            {
                EchoInteractable item = interactables[i];
                if (item.Kind != "CORE" || collectedCores[item.Index])
                    continue;

                float distance = (item.transform.localPosition - player.transform.localPosition).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    position = item.transform.localPosition;
                }
            }

            label = "Recover power cores  " + coresFound + " / 3";
            return true;
        }

        if (!powerRestored)
        {
            if (!AllReactorWiresConnected())
            {
                float closestWireDistance = float.PositiveInfinity;
                for (int i = 0; i < interactables.Count; i++)
                {
                    EchoInteractable wire = interactables[i];
                    if (wire.Kind != "WIRE" || reactorWiresConnected[wire.Index] || !collectedCores[wire.Index])
                        continue;

                    float distance = (wire.transform.localPosition - player.transform.localPosition).sqrMagnitude;
                    if (distance < closestWireDistance)
                    {
                        closestWireDistance = distance;
                        position = wire.transform.localPosition;
                    }
                }

                label = "Connect reactor leads  " + CountConnectedReactorWires() + " / 3";
                return true;
            }

            label = "Restore the reactor";
            position = new Vector3(19f, 0.1f, -1.5f);
            return true;
        }

        if (relayCount < activatedRelays.Length)
        {
            float closestDistance = float.PositiveInfinity;
            for (int i = 0; i < interactables.Count; i++)
            {
                EchoInteractable item = interactables[i];
                if (item.Kind != "RELAY" || activatedRelays[item.Index])
                    continue;

                float distance = (item.transform.localPosition - player.transform.localPosition).sqrMagnitude;
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    position = item.transform.localPosition;
                }
            }

            label = "Align comms relays  " + relayCount + " / 3";
            return true;
        }

        if (!signalCalibrated)
        {
            label = "Calibrate the rescue array";
            position = new Vector3(72f, 0.1f, 0f);
            return true;
        }

        label = "Open the final airlock";
        position = new Vector3(76f, 0.1f, 0f);
        return true;
    }

    private void EnsureStyles()
    {
        if (stylesReady || GUI.skin == null)
            return;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = 50;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = new Color(0.82f, 0.97f, 1f);

        subtitleStyle = new GUIStyle(GUI.skin.label);
        subtitleStyle.fontSize = 16;
        subtitleStyle.alignment = TextAnchor.MiddleCenter;
        subtitleStyle.wordWrap = true;
        subtitleStyle.normal.textColor = new Color(0.61f, 0.77f, 0.82f);

        hudStyle = new GUIStyle(GUI.skin.label);
        hudStyle.fontSize = 15;
        hudStyle.fontStyle = FontStyle.Bold;
        hudStyle.normal.textColor = new Color(0.69f, 0.92f, 0.96f);

        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.fontSize = 12;
        smallStyle.normal.textColor = new Color(0.57f, 0.72f, 0.77f);

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = MakeGuiTexture(new Color(0.012f, 0.025f, 0.035f, 0.2f));

        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = 16;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.normal.textColor = Color.white;
        buttonStyle.hover.textColor = new Color(0.5f, 0.95f, 1f);

        headingStyle = new GUIStyle(GUI.skin.label);
        headingStyle.fontSize = 25;
        headingStyle.fontStyle = FontStyle.Bold;
        headingStyle.alignment = TextAnchor.MiddleCenter;
        headingStyle.normal.textColor = new Color(0.62f, 0.92f, 0.97f);

        Font minimalFont = GUI.skin.font;
        if (minimalFont == null)
            minimalFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (minimalFont != null)
        {
            titleStyle.font = minimalFont;
            subtitleStyle.font = minimalFont;
            hudStyle.font = minimalFont;
            smallStyle.font = minimalFont;
            buttonStyle.font = minimalFont;
            headingStyle.font = minimalFont;
        }

        stylesReady = true;
    }

    private static Texture2D MakeGuiTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private void DrawTitle()
    {
        DrawOverlay(new Color(0.005f, 0.012f, 0.02f, 0.84f));
        float center = Screen.width * 0.5f;
        GUI.Label(new Rect(center - 310, 100, 620, 66), "ECHO", titleStyle);
        GUI.Label(new Rect(center - 310, 161, 620, 36), "THE LAST SIGNAL", headingStyle);
        GUI.Label(new Rect(center - 330, 220, 660, 48), "ONE STATION. THREE CORES. ONE THING THAT HEARS YOU.", subtitleStyle);

        GUILayout.BeginArea(new Rect(center - 300, 302, 600, 205), panelStyle);
        GUILayout.Space(15);
        GUILayout.Label("You wake alone on Deck 03. The crew is gone. The lights are dying.", subtitleStyle);
        GUILayout.Space(12);
        GUILayout.Label("ENTER begin    WASD move    MOUSE look    SHIFT sprint    SPACE jump", subtitleStyle);
        GUILayout.Label("CTRL crouch    Q sonar    F flashlight    E interact    ESC pause", subtitleStyle);
        GUILayout.Space(14);
        GUILayout.Label("Sound reveals the station — and tells the Listener where you are.", subtitleStyle);
        GUILayout.EndArea();

        if (GUI.Button(new Rect(center - 130, 535, 260, 52), "ENTER THE STATION", buttonStyle))
            StartGame();
        GUI.Label(new Rect(center - 230, 600, 460, 28), "A short first-person stealth horror experience", smallStyle);
    }

    private void DrawHud()
    {
        Color old = GUI.color;
        float margin = Mathf.Max(14f, Screen.width * 0.022f);
        float leftWidth = Mathf.Min(300f, Screen.width * 0.42f);
        float rightWidth = Mathf.Min(230f, Screen.width * 0.36f);
        float panelTop = 18f;
        GUI.Box(new Rect(margin, panelTop, leftWidth, 142f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(margin + 14f, panelTop + 10f, leftWidth - 28f, 24f), "ECHO  /  DECK 03", hudStyle);
        GUI.Label(new Rect(margin + 14f, panelTop + 38f, leftWidth - 28f, 20f), "REACTOR CELLS", smallStyle);
        for (int i = 0; i < collectedCores.Length; i++)
        {
            Rect cellSlot = new Rect(margin + 14f + i * 36f, panelTop + 61f, 28f, 24f);
            GUI.color = collectedCores[i] ? new Color(0.08f, 0.72f, 0.82f) : new Color(0.12f, 0.2f, 0.24f);
            GUI.Box(cellSlot, GUIContent.none, panelStyle);
            GUI.color = collectedCores[i] ? Color.white : new Color(0.54f, 0.67f, 0.7f);
            GUI.Label(new Rect(cellSlot.x, cellSlot.y + 2f, cellSlot.width, cellSlot.height - 2f),
                (i + 1).ToString(), headingStyle);
        }
        GUI.color = old;
        GUI.Label(new Rect(margin + 14f, panelTop + 92f, leftWidth - 28f, 19f),
            powerRestored ? "REACTOR  /  ONLINE" : "REACTOR  /  " + coresFound + " OF 3 CELLS NEEDED", smallStyle);
        GUI.Label(new Rect(margin + 14f, panelTop + 114f, leftWidth - 28f, 19f),
            "RELAYS  " + relayCount + " / 3     LOGS  " + logsFound + " / 3", smallStyle);

        float rightX = Screen.width - margin - rightWidth;
        GUI.Box(new Rect(rightX, panelTop, rightWidth, 92f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(rightX + 14f, panelTop + 10f, rightWidth - 28f, 21f),
            flashlightOn ? "FLASHLIGHT  /  ON" : "FLASHLIGHT  /  OFF", smallStyle);
        GUI.Box(new Rect(rightX + 14f, panelTop + 39f, rightWidth - 28f, 10f), GUIContent.none, panelStyle);
        GUI.color = battery > 16f ? new Color(0.1f, 0.8f, 0.88f) : new Color(1f, 0.27f, 0.16f);
        GUI.Box(new Rect(rightX + 14f, panelTop + 39f, (rightWidth - 28f) * battery / FlashlightCapacity, 10f), GUIContent.none);
        GUI.color = old;
        GUI.Label(new Rect(rightX + 14f, panelTop + 59f, rightWidth - 28f, 19f),
            Mathf.CeilToInt(battery) + "%     Q  SONAR     F  LIGHT", smallStyle);

        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;
        Color reticleColor = activeInteractable != null
            ? new Color(0.95f, 0.78f, 0.3f, 0.95f)
            : new Color(0.55f, 0.9f, 0.95f, 0.82f);
        GUI.color = reticleColor;
        GUI.DrawTexture(new Rect(centerX - 1f, centerY - 8f, 2f, 16f), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(centerX - 8f, centerY - 1f, 16f, 2f), Texture2D.whiteTexture);
        GUI.color = old;

        if (activeInteractable != null)
        {
            float promptWidth = Mathf.Min(430f, Screen.width - 32f);
            GUI.Box(new Rect(centerX - promptWidth * 0.5f, Screen.height - 96f, promptWidth, 44f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(centerX - promptWidth * 0.5f + 12f, Screen.height - 89f, promptWidth - 24f, 30f),
                "[ E ]  " + activeInteractable.Label, hudStyle);
        }
        else if (Time.time < statusUntil)
        {
            float messageWidth = Mathf.Min(640f, Screen.width - 32f);
            GUI.Box(new Rect(centerX - messageWidth * 0.5f, Screen.height - 92f, messageWidth, 46f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(centerX - messageWidth * 0.5f + 14f, Screen.height - 84f, messageWidth - 28f, 30f),
                statusMessage, subtitleStyle);
        }

        float danger = Mathf.Clamp01(1f - Vector3.Distance(player.transform.localPosition, creature.transform.localPosition) / 10f);
        if (danger > 0.05f)
        {
            GUI.color = new Color(0.85f, 0.1f, 0.12f, danger * 0.24f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(rightX + 2f, panelTop + 112f, rightWidth, 24f),
                danger > 0.7f ? "THREAT  /  IMMEDIATE" : "THREAT  /  CLOSING", subtitleStyle);
        }

        DrawObjectivePointer();
        GUI.Label(new Rect(margin, Screen.height - 31f, Mathf.Min(520f, Screen.width - margin * 2f), 22f),
            "WASD MOVE    SHIFT SPRINT    SPACE JUMP    CTRL CROUCH    E INTERACT    Q SONAR    ESC PAUSE", smallStyle);
    }

    private void DrawObjectivePointer()
    {
        string objective;
        Vector3 position;
        if (!GetNextObjective(out objective, out position))
            return;

        float distance = Vector3.Distance(player.transform.localPosition, position);
        float margin = Mathf.Max(14f, Screen.width * 0.022f);
        float panelWidth = Mathf.Min(300f, Screen.width * 0.42f);
        float panelX = margin;
        GUI.Box(new Rect(panelX, 168f, panelWidth, 54f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(panelX + 12f, 174f, panelWidth - 24f, 22f), objective, hudStyle);
        GUI.Label(new Rect(panelX + 12f, 198f, panelWidth - 24f, 18f),
            "NEXT OBJECTIVE  /  " + Mathf.RoundToInt(distance) + " m", smallStyle);

        Vector3 projected = playerCamera.WorldToScreenPoint(worldRoot.TransformPoint(position + Vector3.up * 1.4f));
        Vector2 marker = new Vector2(projected.x, Screen.height - projected.y);
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (projected.z < 0f)
            marker = center * 2f - marker;

        Vector2 direction = marker - center;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.up;
        direction.Normalize();

        float maxX = Mathf.Max(30f, Screen.width * 0.5f - 38f);
        float maxY = Mathf.Max(80f, Screen.height * 0.5f - 38f);
        float scaleX = Mathf.Abs(direction.x) > 0.001f ? maxX / Mathf.Abs(direction.x) : float.PositiveInfinity;
        float scaleY = Mathf.Abs(direction.y) > 0.001f ? maxY / Mathf.Abs(direction.y) : float.PositiveInfinity;
        Vector2 edge = center + direction * Mathf.Min(scaleX, scaleY);
        bool onScreen = projected.z > 0f && marker.x > 36f && marker.x < Screen.width - 36f &&
            marker.y > 76f && marker.y < Screen.height - 48f;
        Vector2 pointerPosition = onScreen ? marker : edge;
        string arrow = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
            ? direction.x < 0f ? "<" : ">"
            : direction.y < 0f ? "^" : "v";
        GUI.Box(new Rect(pointerPosition.x - 20f, pointerPosition.y - 20f, 40f, 40f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(pointerPosition.x - 18f, pointerPosition.y - 20f, 36f, 38f), arrow, headingStyle);
    }

    private void DrawPuzzle()
    {
        DrawOverlay(new Color(0f, 0.008f, 0.014f, 0.75f));
        float center = Screen.width * 0.5f;
        GUILayout.BeginArea(new Rect(center - 260, Screen.height * 0.5f - 190, 520, 380), panelStyle);
        GUILayout.Space(24);
        GUILayout.Label("REACTOR BREAKER ROUTE", headingStyle);
        GUILayout.Space(14);
        GUILayout.Label("Maintenance note: 2  >  3  >  1", subtitleStyle);
        GUILayout.Space(26);
        string entered = puzzleInput.Count == 0 ? "_   _   _" : string.Join("   ", puzzleInput.ConvertAll(value => value.ToString()).ToArray()) + (puzzleInput.Count < 3 ? "   _" : "");
        GUILayout.Label(entered, titleStyle);
        GUILayout.Space(28);
        GUILayout.Label("Press the numbered breakers in order.", subtitleStyle);
        GUILayout.Label("1 / 2 / 3  select     BACKSPACE  reset     ESC  cancel", smallStyle);
        GUILayout.Space(20);
        if (GUILayout.Button("CLOSE PANEL", buttonStyle, GUILayout.Height(40)))
        {
            state = GameState.Playing;
            playerMotor.SetControlsEnabled(true);
            SetCursor(true);
        }
        GUILayout.EndArea();
    }

    private void DrawSignalPuzzle()
    {
        DrawOverlay(new Color(0f, 0.008f, 0.014f, 0.78f));
        float center = Screen.width * 0.5f;
        GUILayout.BeginArea(new Rect(center - 270, Screen.height * 0.5f - 195, 540, 390), panelStyle);
        GUILayout.Space(23);
        GUILayout.Label("LONG-RANGE CARRIER ARRAY", headingStyle);
        GUILayout.Space(15);
        GUILayout.Label("The crew's calibration route is 3  >  1  >  2.", subtitleStyle);
        GUILayout.Space(24);
        string entered = signalInput.Count == 0 ? "_   _   _" : string.Join("   ", signalInput.ConvertAll(value => value.ToString()).ToArray()) + (signalInput.Count < 3 ? "   _" : "");
        GUILayout.Label(entered, titleStyle);
        GUILayout.Space(28);
        GUILayout.Label("A wrong frequency broadcasts your location.", subtitleStyle);
        GUILayout.Label("1 / 2 / 3  tune     BACKSPACE  reset     ESC  close", smallStyle);
        GUILayout.Space(20);
        if (GUILayout.Button("CLOSE ARRAY", buttonStyle, GUILayout.Height(40)))
        {
            state = GameState.Playing;
            playerMotor.SetControlsEnabled(true);
            SetCursor(true);
        }
        GUILayout.EndArea();
    }

    private void DrawEndingChoice()
    {
        DrawOverlay(new Color(0f, 0.008f, 0.014f, 0.78f));
        float center = Screen.width * 0.5f;
        GUILayout.BeginArea(new Rect(center - 280, Screen.height * 0.5f - 185, 560, 370), panelStyle);
        GUILayout.Space(24);
        GUILayout.Label("AIRLOCK READY", headingStyle);
        GUILayout.Space(16);
        GUILayout.Label("You can still hear movement in the reactor.", subtitleStyle);
        GUILayout.Space(25);
        if (GUILayout.Button(signalCalibrated
                ? "1   SEND THE CREW'S LAST SIGNAL  /  CARRIER READY"
                : "1   SEND AN UNCALIBRATED SIGNAL", buttonStyle, GUILayout.Height(58)))
            ChooseEnding(true);
        GUILayout.Space(13);
        if (GUILayout.Button("2   ESCAPE ALONE", buttonStyle, GUILayout.Height(58)))
            ChooseEnding(false);
        GUILayout.Space(23);
        GUILayout.Label("Relays " + relayCount + " / 3     Crew logs " + logsFound + " / 3", smallStyle);
        GUILayout.EndArea();
    }

    private void DrawPause()
    {
        DrawOverlay(new Color(0.005f, 0.012f, 0.02f, 0.8f));
        float center = Screen.width * 0.5f;
        GUILayout.BeginArea(new Rect(center - 210, Screen.height * 0.5f - 125, 420, 250), panelStyle);
        GUILayout.Space(22);
        GUILayout.Label("PAUSED", headingStyle);
        GUILayout.Space(26);
        if (GUILayout.Button("RESUME", buttonStyle, GUILayout.Height(46)))
            ResumeGame();
        GUILayout.Space(10);
        if (GUILayout.Button("RESTART", buttonStyle, GUILayout.Height(46)))
            RestartGame();
        GUILayout.EndArea();
    }

    private void DrawEndCard(string title, string body, string action)
    {
        DrawOverlay(new Color(0.005f, 0.012f, 0.02f, 0.88f));
        float center = Screen.width * 0.5f;
        GUI.Label(new Rect(center - 380, Screen.height * 0.5f - 115, 760, 60), title, headingStyle);
        GUI.Label(new Rect(center - 320, Screen.height * 0.5f - 35, 640, 90), body, subtitleStyle);
        GUI.Label(new Rect(center - 250, Screen.height * 0.5f + 90, 500, 35), action, hudStyle);
    }

    private void DrawEscapeFlight()
    {
        DrawOverlay(new Color(0.005f, 0.012f, 0.02f, 0.18f));
        float distance = escapeShip != null
            ? Vector3.Distance(escapeShip.transform.localPosition, blackHolePosition)
            : 0f;
        GUI.Label(new Rect(24f, 22f, 420f, 30f), "STARSPARROW  /  BLACK HOLE APPROACH", hudStyle);
        GUI.Label(new Rect(24f, 52f, 540f, 24f),
            "W  THROTTLE   S  BRAKE   A / D  STEER   ARROWS  PITCH   SHIFT  BOOST", smallStyle);
        GUI.Label(new Rect(24f, 78f, 360f, 24f),
            "VELOCITY  " + Mathf.RoundToInt(escapeShipSpeed) + " m/s    EVENT HORIZON  " +
            Mathf.RoundToInt(distance) + " m", smallStyle);
        if (blackHoleEntryTime >= 0f)
            GUI.Label(new Rect(Screen.width * 0.5f - 220f, Screen.height * 0.5f, 440f, 45f),
                "ENTERING THE EVENT HORIZON", headingStyle);
    }

    private void DrawOverlay(Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = old;
    }

    public Vector3 GetRoutePosition(int index)
    {
        return RouteNodes[index];
    }

    public int GetNextRouteNode(Vector3 from, Vector3 destination)
    {
        int start = NearestRouteNode(from);
        int goal = NearestRouteNode(destination);
        if (start == goal)
            return -1;

        float[] distances = new float[RouteNodes.Length];
        int[] previous = new int[RouteNodes.Length];
        bool[] visited = new bool[RouteNodes.Length];
        for (int i = 0; i < distances.Length; i++)
        {
            distances[i] = float.PositiveInfinity;
            previous[i] = -1;
        }

        distances[start] = 0f;
        for (int step = 0; step < RouteNodes.Length; step++)
        {
            int current = -1;
            float best = float.PositiveInfinity;
            for (int i = 0; i < distances.Length; i++)
            {
                if (!visited[i] && distances[i] < best)
                {
                    current = i;
                    best = distances[i];
                }
            }

            if (current < 0 || current == goal)
                break;
            visited[current] = true;
            for (int edge = 0; edge < RouteEdges.GetLength(0); edge++)
            {
                int a = RouteEdges[edge, 0];
                int b = RouteEdges[edge, 1];
                int neighbor = a == current ? b : b == current ? a : -1;
                if (neighbor < 0)
                    continue;

                float candidate = distances[current] + Vector3.Distance(RouteNodes[current], RouteNodes[neighbor]);
                if (candidate < distances[neighbor])
                {
                    distances[neighbor] = candidate;
                    previous[neighbor] = current;
                }
            }
        }

        int next = goal;
        while (previous[next] >= 0 && previous[next] != start)
            next = previous[next];
        return previous[next] < 0 ? -1 : next;
    }

    private int NearestRouteNode(Vector3 position)
    {
        int nearest = 0;
        float bestDistance = float.PositiveInfinity;
        for (int i = 0; i < RouteNodes.Length; i++)
        {
            float distance = (RouteNodes[i] - position).sqrMagnitude;
            if (distance < bestDistance)
            {
                nearest = i;
                bestDistance = distance;
            }
        }

        return nearest;
    }
}

public sealed class EchoPlayerMotor : MonoBehaviour
{
    private EchoGame game;
    private CharacterController controller;
    private Light flashlight;
    private Camera playerCamera;
    private float verticalSpeed;
    private float pitch;
    private float yaw = 90f;
    private float cameraBob;
    private bool controlsEnabled;
    private float originalHeight;

    public void Initialize(EchoGame owner, CharacterController character)
    {
        game = owner;
        controller = character;
        originalHeight = character.height;
        playerCamera = GetComponentInChildren<Camera>();
        controlsEnabled = false;
    }

    public void SetFlashlight(Light light)
    {
        flashlight = light;
    }

    public void SetFlashlightActive(bool active)
    {
        if (flashlight != null)
            flashlight.enabled = active;
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;
    }

    public void SetCamera(Camera camera)
    {
        playerCamera = camera;
    }

    public void ResetMotor()
    {
        verticalSpeed = 0f;
        pitch = 0f;
        yaw = 90f;
        controlsEnabled = true;
        controller.height = originalHeight;
        controller.center = new Vector3(0f, 0.9f, 0f);
    }

    private void Update()
    {
        if (!controlsEnabled || Keyboard.current == null || Mouse.current == null)
            return;

        Vector2 look = Mouse.current.delta.ReadValue() * 0.08f;
        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, -82f, 82f);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        Vector2 input = Vector2.zero;
        Keyboard keyboard = Keyboard.current;
        if (keyboard.wKey.isPressed) input.y += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        input = Vector2.ClampMagnitude(input, 1f);

        bool crouching = game.IsCrouching;
        controller.height = Mathf.Lerp(controller.height, crouching ? 1.05f : originalHeight, Time.deltaTime * 10f);
        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
        bool sprinting = keyboard.leftShiftKey.isPressed && !crouching && input.y > 0.1f;
        float speed = crouching ? 2.25f : sprinting ? 6.3f : 4.25f;
        Vector3 movement = transform.right * input.x + transform.forward * input.y;

        if (controller.isGrounded)
        {
            if (verticalSpeed < 0f)
                verticalSpeed = -1.5f;
            if (keyboard.spaceKey.wasPressedThisFrame)
                verticalSpeed = 4.8f;
        }

        verticalSpeed -= 18f * Time.deltaTime;
        movement *= speed;
        movement.y = verticalSpeed;
        controller.Move(movement * Time.deltaTime);

        float movementAmount = input.magnitude;
        cameraBob += Time.deltaTime * (sprinting ? 12f : 8f) * movementAmount;
        Vector3 cameraPosition = new Vector3(0f, controller.height * 0.5f + 0.65f, 0f);
        if (movementAmount > 0.05f && controller.isGrounded)
            cameraPosition.y += Mathf.Sin(cameraBob) * 0.025f;
        playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, cameraPosition, Time.deltaTime * 14f);
    }
}

public sealed class EchoInteractable : MonoBehaviour
{
    public string Kind { get; private set; }
    public string Label { get; private set; }
    public int Index { get; private set; }

    public void Initialize(string kind, int index, string label)
    {
        Kind = kind;
        Index = index;
        Label = label;
    }
}

public sealed class EchoCreature : MonoBehaviour
{
    private static readonly int[] RespawnRouteNodes = { 2, 3, 4, 5, 7, 8, 9, 10, 11, 12, 13 };
    private EchoGame game;
    private readonly List<Renderer> creatureRenderers = new List<Renderer>();
    private readonly List<Material> runtimeMaterials = new List<Material>();
    private Animator animator;
    private CharacterController movementController;
    private PlayableGraph animationGraph;
    private AnimationMixerPlayable animationMixer;
    private AnimationClipPlayable idlePlayable;
    private AnimationClipPlayable runPlayable;
    private AnimationClip idleClip;
    private AnimationClip runClip;
    private Vector3 investigationPoint;
    private Vector3 patrolPoint;
    private float investigationTime;
    private float patrolTimer;
    private float animationTime;
    private float chaseBlend;
    private int patrolIndex;
    private int lastRespawnNode = -1;
    private Vector3 respawnPosition = new Vector3(21f, 0f, 0f);
    private bool investigating;
    private bool revealed;
    private bool isVisible = true;
    private float revealUntil;
    public bool IsInvestigating { get { return investigating; } }

    public void Initialize(EchoGame owner, GameObject modelPrefab, Material materialTemplate)
    {
        game = owner;
        BuildBody(modelPrefab, materialTemplate);
        patrolPoint = game.GetRoutePosition(4);
    }

    private void BuildBody(GameObject modelPrefab, Material materialTemplate)
    {
        GameObject model = Instantiate(modelPrefab, transform, false);
        model.name = "Monster Mutant 7";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * 1.65f;
        animator = model.GetComponent<Animator>();
        if (animator == null)
            throw new System.InvalidOperationException("The Monster Mutant 7 prefab is missing its Animator component.");
        animator.runtimeAnimatorController = null;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new System.InvalidOperationException("The Monster Mutant 7 prefab has no renderers.");
        creatureRenderers.AddRange(renderers);
        ApplyPinkTexturedMaterials(renderers, materialTemplate);
        LoadAnimationClips();
        CreateAnimationGraph();

        movementController = gameObject.AddComponent<CharacterController>();
        movementController.height = 2.8f;
        movementController.radius = 0.48f;
        movementController.center = new Vector3(0f, 1.4f, 0f);
        movementController.slopeLimit = 45f;
        movementController.stepOffset = 0.3f;
        movementController.skinWidth = 0.06f;
        movementController.minMoveDistance = 0f;
        Light eyeGlow = gameObject.AddComponent<Light>();
        eyeGlow.type = LightType.Point;
        eyeGlow.color = new Color(1f, 0.04f, 0.015f);
        eyeGlow.range = 3.5f;
        eyeGlow.intensity = 1.4f;
        eyeGlow.shadows = LightShadows.None;
        SetVisible(false);
    }

    private void LoadAnimationClips()
    {
        AnimationClip[] clips = Resources.LoadAll<AnimationClip>("MonsterAnimations");
        for (int i = 0; i < clips.Length; i++)
        {
            if (idleClip == null && clips[i].name.IndexOf("idle1", System.StringComparison.OrdinalIgnoreCase) >= 0)
                idleClip = clips[i];
            if (runClip == null && clips[i].name.IndexOf("run1", System.StringComparison.OrdinalIgnoreCase) >= 0)
                runClip = clips[i];
        }

        if (idleClip == null || runClip == null || idleClip.length <= 0f || runClip.length <= 0f)
            throw new System.InvalidOperationException(
                "The Monster Mutant 7 idle1 and run1 animation clips must be imported under Resources/MonsterAnimations with valid lengths.");
    }

    private void CreateAnimationGraph()
    {
        animationGraph = PlayableGraph.Create("Monster Mutant 7 animation");
        animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        animationMixer = AnimationMixerPlayable.Create(animationGraph, 2);
        idlePlayable = AnimationClipPlayable.Create(animationGraph, idleClip);
        runPlayable = AnimationClipPlayable.Create(animationGraph, runClip);
        idlePlayable.SetSpeed(0d);
        runPlayable.SetSpeed(0d);
        animationGraph.Connect(idlePlayable, 0, animationMixer, 0);
        animationGraph.Connect(runPlayable, 0, animationMixer, 1);
        animationMixer.SetInputWeight(0, 1f);
        animationMixer.SetInputWeight(1, 0f);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(animationGraph, "Monster Mutant 7", animator);
        output.SetSourcePlayable(animationMixer);
        animationGraph.Play();
    }

    private void ApplyPinkTexturedMaterials(Renderer[] renderers, Material materialTemplate)
    {
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Material[] sourceMaterials = renderers[rendererIndex].sharedMaterials;
            Material[] convertedMaterials = new Material[sourceMaterials.Length];
            for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
            {
                Material source = sourceMaterials[materialIndex];
                if (source == null)
                    throw new System.InvalidOperationException("The Monster Mutant 7 skin contains a missing material.");

                Material converted = new Material(materialTemplate);
                CopyTextureProperty(source, converted, "_MainTex", "_BaseMap");
                CopyTextureProperty(source, converted, "_BumpMap", "_BumpMap");
                CopyTextureProperty(source, converted, "_MetallicGlossMap", "_MetallicGlossMap");
                CopyTextureProperty(source, converted, "_OcclusionMap", "_OcclusionMap");
                CopyTextureProperty(source, converted, "_EmissionMap", "_EmissionMap");

                Color sourceColor = source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white;
                Color pinkTint = new Color(1f, 0.48f, 0.68f);
                Color tintedColor = new Color(sourceColor.r * pinkTint.r, sourceColor.g * pinkTint.g,
                    sourceColor.b * pinkTint.b, sourceColor.a);
                if (converted.HasProperty("_BaseColor"))
                    converted.SetColor("_BaseColor", tintedColor);
                else
                    converted.color = tintedColor;

                if (source.HasProperty("_Metallic") && converted.HasProperty("_Metallic"))
                    converted.SetFloat("_Metallic", source.GetFloat("_Metallic"));
                if (source.HasProperty("_Glossiness") && converted.HasProperty("_Smoothness"))
                    converted.SetFloat("_Smoothness", source.GetFloat("_Glossiness"));
                if (source.HasProperty("_OcclusionStrength") && converted.HasProperty("_OcclusionStrength"))
                    converted.SetFloat("_OcclusionStrength", source.GetFloat("_OcclusionStrength"));
                if (source.HasProperty("_EmissionColor") && converted.HasProperty("_EmissionColor"))
                {
                    converted.EnableKeyword("_EMISSION");
                    converted.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));
                }

                if (converted.GetTexture("_BumpMap") != null)
                    converted.EnableKeyword("_NORMALMAP");
                if (converted.GetTexture("_MetallicGlossMap") != null)
                    converted.EnableKeyword("_METALLICSPECGLOSSMAP");
                runtimeMaterials.Add(converted);
                convertedMaterials[materialIndex] = converted;
            }

            renderers[rendererIndex].sharedMaterials = convertedMaterials;
        }
    }

    private static void CopyTextureProperty(Material source, Material destination, string sourceProperty, string destinationProperty)
    {
        if (!source.HasProperty(sourceProperty) || !destination.HasProperty(destinationProperty))
            return;

        Texture texture = source.GetTexture(sourceProperty);
        if (texture == null)
            return;

        destination.SetTexture(destinationProperty, texture);
        destination.SetTextureScale(destinationProperty, source.GetTextureScale(sourceProperty));
        destination.SetTextureOffset(destinationProperty, source.GetTextureOffset(sourceProperty));
    }

    private void Update()
    {
        if (game == null || !game.CanEmitPulse)
            return;

        float distance = Vector3.Distance(transform.localPosition, game.PlayerPosition);
        if (distance < (game.IsCrouching ? 7f : 14f))
        {
            investigationPoint = game.PlayerPosition;
            investigating = true;
            investigationTime = 12f;
        }

        if (investigating)
        {
            investigationTime -= Time.deltaTime;
            if (investigationTime <= 0f && distance > 7f)
            {
                investigating = false;
                patrolTimer = 0f;
            }
        }

        if (!investigating)
        {
            patrolTimer -= Time.deltaTime;
            if (patrolTimer <= 0f || Vector3.Distance(transform.localPosition, patrolPoint) < 1.5f)
            {
                patrolIndex = (patrolIndex + 1) % 4;
                patrolPoint = game.GetRoutePosition(patrolIndex == 0 ? 3 : patrolIndex == 1 ? 5 : patrolIndex == 2 ? 2 : 4);
                patrolTimer = 4f;
            }
        }

        Vector3 target = investigating ? investigationPoint : patrolPoint;
        int nextNode = game.GetNextRouteNode(transform.localPosition, target);
        if (nextNode >= 0 && Vector3.Distance(transform.localPosition, target) > 2.2f)
            target = game.GetRoutePosition(nextNode);
        target.y = transform.localPosition.y;

        float speed = investigating
            ? distance < 5f ? 5.8f : 5f
            : 1.65f;
        Vector3 oldPosition = transform.localPosition;
        Vector3 movement = target - oldPosition;
        movement.y = 0f;
        if (movement.sqrMagnitude > 0.001f)
        {
            movement = movement.normalized * speed * Time.deltaTime;
            movementController.Move(transform.parent.TransformDirection(movement));
        }

        Vector3 direction = transform.localPosition - oldPosition;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 6f);

        if (revealed && Time.time > revealUntil)
            revealed = false;

        SetVisible(revealed || investigating || distance < 9f);
        SetAnimation(investigating);
    }

    private void SetAnimation(bool chasing)
    {
        if (!animationGraph.IsValid())
            return;

        animationTime += Time.deltaTime;
        idlePlayable.SetTime(animationTime % idleClip.length);
        runPlayable.SetTime(animationTime * 1.15f % runClip.length);
        chaseBlend = Mathf.MoveTowards(chaseBlend, chasing ? 1f : 0f, Time.deltaTime * 3.5f);
        animationMixer.SetInputWeight(0, 1f - chaseBlend);
        animationMixer.SetInputWeight(1, chaseBlend);
    }

    public void HearPulse(Vector3 source)
    {
        investigationPoint = source;
        investigating = true;
        investigationTime = 8f;
    }

    public void Reveal(float duration)
    {
        revealUntil = Time.time + duration;
        revealed = true;
        SetVisible(true);
    }

    private void SetVisible(bool visible)
    {
        if (isVisible == visible)
            return;

        isVisible = visible;
        for (int i = 0; i < creatureRenderers.Count; i++)
            creatureRenderers[i].enabled = visible;
    }

    public void ResetCreature()
    {
        SetCreaturePosition(respawnPosition);
        transform.rotation = Quaternion.identity;
        investigationTime = 0f;
        investigating = false;
        patrolTimer = 0f;
        patrolIndex = 0;
        patrolPoint = game.GetRoutePosition(4);
        revealed = false;
        SetVisible(false);
        chaseBlend = 0f;
        SetAnimation(false);
    }

    public void PrepareRespawnAfterDeath(Vector3 deathPosition)
    {
        int selectedNode = -1;
        List<int> candidates = new List<int>(RespawnRouteNodes.Length);
        for (int i = 0; i < RespawnRouteNodes.Length; i++)
        {
            int node = RespawnRouteNodes[i];
            Vector3 candidate = game.GetRoutePosition(node);
            if (node != lastRespawnNode && Vector3.Distance(candidate, deathPosition) >= 14f)
                candidates.Add(node);
        }

        if (candidates.Count == 0)
        {
            for (int i = 0; i < RespawnRouteNodes.Length; i++)
            {
                int node = RespawnRouteNodes[i];
                if (node != lastRespawnNode)
                    candidates.Add(node);
            }
        }

        if (candidates.Count == 0)
            throw new System.InvalidOperationException("No alternate monster respawn points are available.");

        selectedNode = candidates[Random.Range(0, candidates.Count)];
        lastRespawnNode = selectedNode;
        respawnPosition = game.GetRoutePosition(selectedNode);
        SetCreaturePosition(respawnPosition);
        investigationTime = 0f;
        investigating = false;
        patrolTimer = 0f;
        patrolIndex = 0;
        patrolPoint = game.GetRoutePosition(4);
        revealed = false;
        SetVisible(false);
    }

    private void SetCreaturePosition(Vector3 position)
    {
        if (movementController != null)
            movementController.enabled = false;
        transform.localPosition = position;
        if (movementController != null)
            movementController.enabled = true;
    }

    private void OnDestroy()
    {
        if (animationGraph.IsValid())
            animationGraph.Destroy();

        for (int i = 0; i < runtimeMaterials.Count; i++)
            Destroy(runtimeMaterials[i]);
    }
}
