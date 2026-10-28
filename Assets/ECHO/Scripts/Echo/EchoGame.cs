using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public sealed partial class EchoGame : MonoBehaviour
{
    private enum GameState
    {
        Title,
        Playing,
        Puzzle,
        SignalPuzzle,
        Choice,
        Paused,
        Dying,
        Dead,
        Ending
    }

    private static readonly Vector3 SpawnPoint = new Vector3(-25f, 0.1f, 0f);
    private static readonly Vector3 StationEditorOffset = Vector3.zero;
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
    private AudioClip monsterFootstepLoopClip;
    private AudioClip monsterProximityRoarClip;
    private AudioClip monsterDeathRoarClip;
    private AudioClip[] footstepClips = new AudioClip[10];
    private float deathCutsceneTimer;
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

        if (state == GameState.Dying)
        {
            UpdateDeathCutscene();
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
            TriggerDeathCutscene();
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
            light.intensity = 0.04f;

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

    private void TriggerDeathCutscene()
    {
        if (state != GameState.Playing)
            return;

        state = GameState.Dying;
        deathCutsceneTimer = 2.4f;
        playerMotor.SetControlsEnabled(false);
        SetCursor(false);

        Vector3 camPos = playerCamera.transform.position;
        Vector3 camFwd = playerCamera.transform.forward;
        camFwd.y = 0f;
        if (camFwd.sqrMagnitude < 0.001f)
            camFwd = player.transform.forward;
        camFwd.Normalize();

        Vector3 monsterWorldPos = camPos + camFwd * 0.82f;
        monsterWorldPos.y = player.transform.position.y;
        creature.SetCreaturePosition(worldRoot.InverseTransformPoint(monsterWorldPos));
        creature.transform.rotation = Quaternion.LookRotation(-camFwd);
        creature.SetVisible(true);
        creature.TriggerJumpscareScream();
    }

    private void UpdateDeathCutscene()
    {
        deathCutsceneTimer -= Time.deltaTime;

        Vector3 headPos = creature != null ? creature.HeadPosition : (playerCamera.transform.position + playerCamera.transform.forward);
        Vector3 lookDir = (headPos - playerCamera.transform.position).normalized;
        Quaternion targetLook = Quaternion.LookRotation(lookDir);

        float shakeStrength = Mathf.Clamp01(deathCutsceneTimer / 1.6f) * 7.5f;
        float shakeX = (Mathf.PerlinNoise(Time.time * 42f, 0.2f) - 0.5f) * shakeStrength;
        float shakeY = (Mathf.PerlinNoise(0.5f, Time.time * 42f) - 0.5f) * shakeStrength;
        float shakeZ = (Mathf.PerlinNoise(Time.time * 28f, Time.time * 28f) - 0.5f) * shakeStrength * 0.7f;

        playerCamera.transform.rotation = targetLook * Quaternion.Euler(shakeX, shakeY, shakeZ);

        if (playerMotor != null)
        {
            bool flicker = Mathf.Sin(Time.time * 45f) > -0.2f && Random.value > 0.15f;
            playerMotor.SetFlashlightActive(flicker);
        }

        if (deathCutsceneTimer <= 0f)
            Die();
    }

    private void Die()
    {
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

        float baseIntensity = powerRestored ? 0.28f : 0.04f;
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
    public bool FlashlightOn { get { return flashlightOn; } }

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
            case GameState.Dying:
                DrawDyingJumpscare();
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

    private void DrawDyingJumpscare()
    {
        float pulse = Mathf.Sin(Time.time * 26f) * 0.25f + 0.5f;
        Color old = GUI.color;
        GUI.color = new Color(0.92f, 0.02f, 0.02f, pulse);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);

        float marginY = Screen.height * 0.16f;
        float marginX = Screen.width * 0.12f;
        GUI.color = new Color(0.02f, 0.01f, 0.01f, 0.88f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, marginY), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, Screen.height - marginY, Screen.width, marginY), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, marginY, marginX, Screen.height - marginY * 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(Screen.width - marginX, marginY, marginX, Screen.height - marginY * 2), Texture2D.whiteTexture);

        if (headingStyle != null)
        {
            GUI.color = new Color(1f, 0.25f, 0.25f, 0.95f);
            GUI.Label(new Rect(0, Screen.height * 0.76f, Screen.width, 50), "SIGNAL TERMINATED", headingStyle);
        }
        GUI.color = old;
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


