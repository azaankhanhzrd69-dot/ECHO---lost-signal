using System.Collections.Generic;
using UnityEngine;

public sealed partial class EchoGame : MonoBehaviour
{
    private static readonly Vector3[] LockerCornerPositions =
    {
        new Vector3(-31.5f, 1f, 19.5f),
        new Vector3(-31.5f, 1f, -19.5f),
        new Vector3(-12.5f, 1f, 19.5f),
        new Vector3(-12.5f, 1f, -19.5f),
        new Vector3(12.5f, 1f, 17.5f),
        new Vector3(12.5f, 1f, -17.5f),
        new Vector3(25.5f, 1f, 9.5f),
        new Vector3(25.5f, 1f, -9.5f),
        new Vector3(41.5f, 1f, 19.5f),
        new Vector3(41.5f, 1f, -19.5f),
        new Vector3(42.5f, 1f, 19.5f),
        new Vector3(42.5f, 1f, -19.5f),
        new Vector3(79.5f, 1f, 19.5f),
        new Vector3(79.5f, 1f, -19.5f)
    };

    // C-03c: "Built map" flag. When true, loads the pre-baked station and lights from scene instead of runtime build.
    [Tooltip("When true, loads pre-baked station geometry and lighting from the scene instead of procedurally building it at runtime. Set in Inspector on the EchoGame host GameObject.")]
    [SerializeField] private bool useBakedStation = false;

    private void BuildGame()
    {
        if (testArenaMode)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.075f, 0.09f);
            RenderSettings.ambientIntensity = 0.8f;
            RenderSettings.fog = false;
            CreateMaterials();
            LoadStationKit();
            worldRoot = new GameObject("W-06 Test Arena").transform;
            BuildTestArena();
        }
        else if (!useBakedStation)
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
            RenderSettings.ambientLight = new Color(0.0003f, 0.0005f, 0.0008f);
            RenderSettings.ambientIntensity = 0.002f;
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.0002f, 0.0003f, 0.0005f);
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.065f;
            BuildNebulaSkybox();

            CreateMaterials();
            LoadStationKit();
            worldRoot = new GameObject("Station - Deck 03").transform;
            worldRoot.position = StationEditorOffset;
            BuildStation();
            BuildModularAnnex();
            BuildSpaceBackdrop();
            BuildBlackHole();
            BuildEscapeShip();
        }
        else
        {
            CollectBakedStationReferences();
        }

        LoadStationAudio();
        BuildPlayer();
        BuildCreature();
        BuildAudio();

        if (testArenaMode)
        {
            state = GameState.Playing;
            playerMotor.SetControlsEnabled(true);
            if (!testMonsterEnabled)
                AddInteractable("HIDE", 0, "Enter locker", new Vector3(2.15f, 0.1f, 0f), false);
        }
        else if (!useBakedStation)
        {
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

        if (!testArenaMode)
            BuildRoomLockersAndProps();
    }

    private void BuildRoomLockersAndProps()
    {
        StationLockerAndPropsConfig config =
            Resources.Load<StationLockerAndPropsConfig>("StationLockerAndProps");
        if (config == null)
        {
            Debug.LogWarning(
                "[EchoGame] Station locker and prop configuration is missing. Run ECHO > W-07 > Configure Station Lockers and Props.",
                this);
            return;
        }

        if (config.LockerPrefab == null)
            throw new System.InvalidOperationException("The station locker prefab is not assigned in StationLockerAndProps.");
        if (config.Lockers == null || config.Lockers.Length != LockerCornerPositions.Length)
            throw new System.InvalidOperationException(
                "The station locker configuration must contain one placement for every room.");

        for (int i = 0; i < config.Lockers.Length; i++)
        {
            StationLockerAndPropsConfig.LockerPlacement placement = config.Lockers[i];
            GameObject locker = Instantiate(config.LockerPrefab, worldRoot, false);
            locker.name = "Hide locker " + (i + 1).ToString("D2");
            locker.transform.localPosition = LockerCornerPositions[i];
            locker.transform.localRotation = Quaternion.Euler(0f, placement.yaw, 0f);
            ApplyImportedMaterials(locker);

            EchoInteractable interactable = locker.GetComponent<EchoInteractable>();
            if (interactable == null)
                interactable = locker.AddComponent<EchoInteractable>();
            interactable.Initialize("HIDE", i, "Hide in locker");
            interactables.Add(interactable);
        }

        GameObject[] propPrefabs = config.PropPrefabs;
        for (int i = 0; i < config.Props.Length; i++)
        {
            StationLockerAndPropsConfig.PropPlacement placement = config.Props[i];
            if (placement.prefabIndex < 0 || placement.prefabIndex >= propPrefabs.Length)
                throw new System.InvalidOperationException(
                    "Station prop placement " + i + " references an invalid prefab index.");
            if (propPrefabs[placement.prefabIndex] == null)
                throw new System.InvalidOperationException(
                    "Station prop prefab " + placement.prefabIndex + " is not assigned.");

            GameObject prop = Instantiate(propPrefabs[placement.prefabIndex], worldRoot, false);
            prop.name = propPrefabs[placement.prefabIndex].name + " / station corner prop " + (i + 1);
            prop.transform.localPosition = placement.position;
            prop.transform.localRotation = Quaternion.Euler(0f, placement.yaw, 0f);
            prop.transform.localScale = placement.scale;
        }
    }

    private void CollectBakedStationReferences()
    {
        // 1. World root
        GameObject rootGo = GameObject.Find("Station - Deck 03");
        if (rootGo != null)
        {
            worldRoot = rootGo.transform;
        }
        else
        {
            Debug.LogWarning("[EchoGame] Baked station root 'Station - Deck 03' not found in scene.");
        }

        // 2. Blast doors (doors count)
        annexBulkhead = GameObject.Find("Power-controlled annex blast door");
        GameObject extraAirlockDoor = GameObject.Find("Power-controlled airlock blast door");
        if (extraAirlockDoor != null)
            extraAirlockDoor.SetActive(false);

        GameObject escapeAirlockFrame = GameObject.Find("Modular escape airlock");
        if (escapeAirlockFrame != null)
            escapeAirlockFrame.SetActive(false);

        int doorsFound = 0;
        if (annexBulkhead != null) doorsFound++;

        // 3. Escape ship and Black hole
        escapeShip = GameObject.Find("StarSparrow - escape craft");
        if (escapeShip != null)
        {
            escapeShipStartPosition = escapeShip.transform.localPosition;
            escapeShipStartRotation = escapeShip.transform.localRotation;
            escapeShipStartScale = escapeShip.transform.localScale;
        }

        blackHole = GameObject.Find("Event horizon / escape destination");

        // 4. Lights
        corridorLights.Clear();
        Light[] allLights = FindObjectsByType<Light>(FindObjectsInactive.Include);
        for (int i = 0; i < allLights.Length; i++)
        {
            Light l = allLights[i];
            if (worldRoot != null && l.transform.IsChildOf(worldRoot))
            {
                corridorLights.Add(l);
            }
        }
        int lightsFound = corridorLights.Count;

        // 5. Interactables (pickups)
        interactables.Clear();
        EchoInteractable[] foundInteractables = FindObjectsByType<EchoInteractable>(FindObjectsInactive.Include);

        List<EchoInteractable> cores = new List<EchoInteractable>();
        List<EchoInteractable> others = new List<EchoInteractable>();

        for (int i = 0; i < foundInteractables.Length; i++)
        {
            EchoInteractable item = foundInteractables[i];
            string goName = item.gameObject.name;
            Vector3 pos = item.transform.localPosition;

            if (goName == "Resonance power core" || goName.StartsWith("Power cell"))
            {
                int index = (pos.z < 0f) ? 1 : (pos.x > 15f) ? 2 : 0;
                item.Initialize("CORE", index, "Take power core");
                cores.Add(item);
            }
            else if (goName == "Access breaker panel")
            {
                item.Initialize("PUZZLE", 0, goName);
                others.Add(item);
            }
            else if (goName == "Read maintenance note")
            {
                item.Initialize("NOTE", 0, goName);
                others.Add(item);
            }
            else if (goName == "Read relay calibration note")
            {
                item.Initialize("NOTE", 1, goName);
                others.Add(item);
            }
            else if (goName == "Open the final airlock")
            {
                item.Initialize("EXIT", 0, goName);
                others.Add(item);
            }
            else if (goName.StartsWith("Align"))
            {
                int index = goName.Contains("long-range") ? 0 : goName.Contains("backup") ? 1 : 2;
                item.Initialize("RELAY", index, goName);
                others.Add(item);

                Transform emitter = item.transform.Find("Relay carrier emitter");
                if (emitter != null && index >= 0 && index < relayIndicators.Length)
                {
                    relayIndicators[index] = emitter.gameObject;
                }
            }
            else if (goName == "Play crew recording")
            {
                int index = (pos.x > 60f) ? 2 : (pos.z < 0f) ? 1 : 0;
                item.Initialize("LOG", index, goName);
                others.Add(item);
            }
            else if (goName == "Open emergency supply case")
            {
                int index = (pos.x > 65f) ? 2 : (pos.z < 0f) ? 1 : 0;
                item.Initialize("SUPPLY", index, goName);
                others.Add(item);
            }
            else if (goName == "Calibrate the long-range array")
            {
                item.Initialize("COMMS", 0, goName);
                others.Add(item);
            }
            else if (goName.StartsWith("Connect reactor lead"))
            {
                int index = 0;
                if (goName.EndsWith("2")) index = 1;
                else if (goName.EndsWith("3")) index = 2;
                item.Initialize("WIRE", index, goName);
                others.Add(item);

                LineRenderer lr = item.GetComponent<LineRenderer>();
                if (lr != null && index >= 0 && index < reactorWires.Length)
                {
                    reactorWires[index] = lr;
                }
                Transform conn = item.transform.Find("Reactor lead connector " + (index + 1));
                if (conn != null && index >= 0 && index < reactorWireConnectors.Length)
                {
                    reactorWireConnectors[index] = conn.gameObject;
                }
            }
            else
            {
                others.Add(item);
            }

            item.transform.localPosition = GetAccessibleInteractionPosition(item.Kind, pos);
            if (item.Kind == "WIRE")
            {
                Transform connector = item.transform.Find("Reactor lead connector " + (item.Index + 1));
                if (connector != null)
                {
                    connector.localPosition = new Vector3(
                        0f,
                        1.07f - item.transform.localPosition.y,
                        -1.74f - item.transform.localPosition.z);
                }
            }
        }

        cores.Sort((a, b) => a.Index.CompareTo(b.Index));
        interactables.AddRange(cores);
        interactables.AddRange(others);
        int pickupsFound = interactables.Count;

        // 6. Reactor indicators
        for (int i = 0; i < 3; i++)
        {
            if (reactorSocketIndicators[i] == null)
                reactorSocketIndicators[i] = GameObject.Find("Reactor cell receptacle " + (i + 1));
            if (reactorCellIndicators[i] == null)
                reactorCellIndicators[i] = GameObject.Find("Reactor cell socket " + (i + 1));
        }

        // 7. Check for camera/audio listener clash in baked scene (Point 5)
        Camera sceneCam = Camera.main;
        if (sceneCam != null)
        {
            AudioListener listener = sceneCam.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = false;
        }

        // 8. Required single Log line
        string logMsg = $"[EchoGame] Using baked station. Found {doorsFound} doors, {pickupsFound} pickups, and {lightsFound} lights.";
        if (doorsFound == 0 || pickupsFound == 0 || lightsFound == 0)
        {
            Debug.LogWarning(logMsg);
        }
        else
        {
            Debug.Log(logMsg);
        }
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

        monsterFootstepLoopClip = TryLoadStationClip("freesound_community-footsteps-75638")
            ?? TryLoadStationClip("Monster_Footsteps");

        monsterProximityRoarClip = TryLoadStationClip("dragon-studio-dragon-roar-364478")
            ?? TryLoadStationClip("Dragon_Roar")
            ?? TryLoadStationClip("freesound_community-monster-scream-death-101123")
            ?? CreateFallbackScreamClip();

        monsterDeathRoarClip = TryLoadStationClip("dragon-studio-epic-dragon-roar-364481")
            ?? TryLoadStationClip("Epic_Dragon_Roar")
            ?? monsterProximityRoarClip;

        for (int i = 0; i < 10; i++)
        {
            string num = (i + 1).ToString("D2");
            footstepClips[i] = TryLoadStationClip("Player_Footstep_" + num);
        }
    }

    private static AudioClip TryLoadStationClip(string name)
    {
        try
        {
            return Resources.Load<AudioClip>("StationAudio/" + name);
        }
        catch
        {
            return null;
        }
    }

    private static AudioClip CreateFallbackScreamClip()
    {
        const int sampleRate = 44100;
        const float duration = 2.4f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        AudioClip clip = AudioClip.Create("MonsterRoarFallback", sampleCount, 1, sampleRate, false);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float progress = t / duration;
            float env = t < 0.08f ? (t / 0.08f) : (t > 1.8f ? Mathf.Clamp01((duration - t) / 0.6f) : 1f);
            float gutFreq = Mathf.Lerp(84f, 46f, progress);
            float gut = Mathf.Sin(2f * Mathf.PI * gutFreq * t + Mathf.Sin(2f * Mathf.PI * 18f * t) * 2.4f);
            float screechFreq = Mathf.Lerp(1180f, 440f, Mathf.Pow(progress, 0.65f));
            float screech = Mathf.Sin(2f * Mathf.PI * screechFreq * t + Mathf.Sin(2f * Mathf.PI * 40f * t) * 3.8f);
            float shriek = Mathf.Sin(2f * Mathf.PI * (screechFreq * 1.5f) * t);
            float rasp = (Mathf.PerlinNoise(t * 3000f, 0.3f) * 2f - 1f) * 0.42f;

            float mix = gut * 0.45f + screech * 0.45f + shriek * 0.2f + rasp;
            samples[i] = Mathf.Clamp(mix * 1.6f, -0.92f, 0.92f) * env * 0.88f;
        }

        clip.SetData(samples, 0);
        return clip;
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
            sky.SetFloat("_Exposure", 0.12f);
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

        AddWall(40f, 20f, 8f, true);
        AddWall(64f, 20f, 8f, true);
        AddWall(76f, 20f, 8f, true);
        AddWall(40f, -20f, 8f, true);
        AddWall(64f, -20f, 8f, true);
        AddWall(76f, -20f, 8f, true);
        AddWall(80f, 23f, 6f, false);
        AddWall(80f, -23f, 6f, false);
        AddWall(80f, 12f, 16f, false);
        AddWall(80f, -12f, 16f, false);

        AddWall(41f, 6f, 14f, true);
        AddWall(58.5f, 6f, 9f, true);
        AddWall(74f, 6f, 22f, true);
        AddWall(41f, -6f, 14f, true);
        AddWall(58.5f, -6f, 9f, true);
        AddWall(74f, -6f, 22f, true);

        AddWall(42f, 13f, 14f, false);
        AddWall(60f, 7.25f, 1.5f, false);
        AddWall(60f, 19f, 2f, false);
        AddWall(52f, 20f, 16f, true);

        AddWall(42f, -13f, 14f, false);
        AddWall(60f, -7.25f, 1.5f, false);
        AddWall(60f, -19f, 2f, false);
        AddWall(52f, -20f, 16f, true);

        AddWall(62f, 12f, 8f, false);
        AddWall(62f, -12f, 8f, false);

        annexBulkhead = CreateBlastDoor("Power-controlled annex blast door", new Vector3(32f, 1.8f, 0f), 5.2f, 4f);

        PlaceKitModule(doorFramePrefab, new Vector3(32f, 0f, 0f), 90f, "Modular engineering airlock", true, false);
        PlaceKitModule(doorFramePrefab, new Vector3(51f, 0f, 6f), 0f, "Modular hydroponics doorway", true, false);
        PlaceKitModule(doorFramePrefab, new Vector3(51f, 0f, -6f), 180f, "Modular maintenance doorway", true, false);
        PlaceKitModule(doorFramePrefab, new Vector3(62f, 0f, 0f), 90f, "Modular cargo doorway", true, false);

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
            ApplyImportedMaterials(cell);
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

    private void ApplyImportedMaterials(GameObject importedObject)
    {
        Renderer[] renderers = importedObject.GetComponentsInChildren<Renderer>(true);
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Material[] sourceMaterials = renderers[rendererIndex].sharedMaterials;
            Material[] convertedMaterials = new Material[sourceMaterials.Length];
            for (int materialIndex = 0; materialIndex < sourceMaterials.Length; materialIndex++)
            {
                Material source = sourceMaterials[materialIndex];
                if (source == null)
                    throw new System.InvalidOperationException("The imported prefab contains a missing material.");

                Material converted;
                if (!convertedCellMaterials.TryGetValue(source, out converted))
                {
                    converted = CreateUrpMaterial(source);
                    convertedCellMaterials.Add(source, converted);
                }

                convertedMaterials[materialIndex] = converted;
            }

            renderers[rendererIndex].sharedMaterials = convertedMaterials;
        }
    }

    private Material CreateUrpMaterial(Material source)
    {
        if (materialTemplate == null)
            materialTemplate = Resources.Load<Material>("EchoLit");
        if (materialTemplate == null || materialTemplate.shader == null)
            throw new System.InvalidOperationException("The EchoLit material and its URP/Lit shader are required.");

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
            lights[i].intensity = 0.04f;
            lights[i].shadows = LightShadows.None;
            corridorLights.Add(lights[i]);
        }

        if (lights.Length == 0)
        {
            Light light = fixture.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = 0.04f;
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

    private static Vector3 GetAccessibleInteractionPosition(string kind, Vector3 originalPosition)
    {
        if (kind == "PUZZLE")
            return new Vector3(originalPosition.x + 1.25f, 0.1f, 0.1f);
        if (kind == "WIRE")
            return new Vector3(originalPosition.x, 0.1f, -1.1f);

        return originalPosition;
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
        player.transform.localPosition = testArenaMode ? testPlayerSpawn : SpawnPoint;
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
        torch.color = new Color(0.72f, 0.88f, 1f);
        torch.range = 22f;
        torch.spotAngle = 78f;
        torch.intensity = 3.2f;
        torch.shadows = LightShadows.Soft;
        playerMotor.SetFlashlight(torch);
        playerMotor.SetFootstepAudio(footstepClips);

        EchoPulse pulse = player.AddComponent<EchoPulse>();
        pulse.Bind(playerCamera);
    }

    private void BuildCreature()
    {
        if (testArenaMode && !testMonsterEnabled)
            return;
        if (testArenaMode && (testPatrolWaypoints == null || testPatrolWaypoints.Length == 0))
            throw new System.InvalidOperationException("The W-06 monster arena requires at least one assigned patrol waypoint.");

        GameObject creatureObject = new GameObject("The Listener");
        creatureObject.transform.SetParent(worldRoot, false);
        creatureObject.transform.localPosition = testArenaMode
            ? testMonsterSpawn
            : new Vector3(21f, 0f, 0f);
        creature = creatureObject.AddComponent<EchoCreature>();
        creature.Initialize(this, monsterPrefab, materialTemplate, monsterFootstepLoopClip, monsterProximityRoarClip, monsterDeathRoarClip, footstepClips, voiceAlerts[2]);
        if (testArenaMode)
        {
            creature.ConfigurePatrolWaypoints(testPatrolWaypoints);
            creature.SetVisible(true);
        }
    }

    private void BuildTestArena()
    {
        if (testMonsterEnabled)
        {
            MakeBlock("Test floor", new Vector3(0f, -0.25f, 0f), new Vector3(52f, 0.5f, 6f), floorMaterial, true);
            MakeBlock("North bulkhead", new Vector3(0f, 2f, 3f), new Vector3(52f, 4f, 0.5f), wallMaterial, true);
            MakeBlock("South bulkhead", new Vector3(0f, 2f, -3f), new Vector3(52f, 4f, 0.5f), wallMaterial, true);
            MakeBlock("West bulkhead", new Vector3(-26f, 2f, 0f), new Vector3(0.5f, 4f, 6f), wallMaterial, true);
            MakeBlock("East bulkhead", new Vector3(26f, 2f, 0f), new Vector3(0.5f, 4f, 6f), wallMaterial, true);
            MakeBlock("Passage north wall", new Vector3(6f, 2f, 2.25f), new Vector3(0.6f, 4f, 1.5f), wallMaterial, true);
            MakeBlock("Passage south wall", new Vector3(6f, 2f, -2.25f), new Vector3(0.6f, 4f, 1.5f), wallMaterial, true);
            MakeBlock("Passage lintel", new Vector3(6f, 3.65f, 0f), new Vector3(0.6f, 0.7f, 3f), trimMaterial, true);
            AddTestLight(new Vector3(-12f, 3.2f, 0f), new Color(0.2f, 0.72f, 0.92f));
            AddTestLight(new Vector3(15f, 3.2f, 0f), new Color(0.92f, 0.36f, 0.16f));
        }
        else
        {
            MakeBlock("Hide test floor", new Vector3(0f, -0.25f, 0f), new Vector3(18f, 0.5f, 16f), floorMaterial, true);
            MakeBlock("North wall", new Vector3(0f, 2f, 8f), new Vector3(18f, 4f, 0.5f), wallMaterial, true);
            MakeBlock("South wall", new Vector3(0f, 2f, -8f), new Vector3(18f, 4f, 0.5f), wallMaterial, true);
            MakeBlock("West wall", new Vector3(-9f, 2f, 0f), new Vector3(0.5f, 4f, 16f), wallMaterial, true);
            MakeBlock("East wall", new Vector3(9f, 2f, 0f), new Vector3(0.5f, 4f, 16f), wallMaterial, true);
            MakeBlock("Locker shell", new Vector3(2.8f, 1.25f, 0f), new Vector3(1.1f, 2.5f, 1.2f), trimMaterial, true);
            MakeBlock("Locker door", new Vector3(2.22f, 1.25f, 0f), new Vector3(0.08f, 2.25f, 0.94f), brightMaterial, false);
            AddTestLight(new Vector3(0f, 3.2f, 1f), new Color(0.2f, 0.72f, 0.92f));
        }
    }

    private void AddTestLight(Vector3 position, Color color)
    {
        GameObject lightObject = new GameObject("Test arena light");
        lightObject.transform.SetParent(worldRoot, false);
        lightObject.transform.localPosition = position;
        Light testLight = lightObject.AddComponent<Light>();
        testLight.type = LightType.Point;
        testLight.color = color;
        testLight.range = 18f;
        testLight.intensity = 4f;
        testLight.shadows = LightShadows.Soft;
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
        target.transform.localPosition = GetAccessibleInteractionPosition(kind, position);
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
            ApplyImportedMaterials(cell);
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
                connector.transform.localPosition = new Vector3(
                    0f,
                    1.07f - target.transform.localPosition.y,
                    -1.74f - target.transform.localPosition.z);
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
}
