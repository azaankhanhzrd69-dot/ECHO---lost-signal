using UnityEngine;
using UnityEngine.UI;
using Echo.Shared;

namespace Echo.Interact
{
    /// <summary>
    /// Shows objective hints and pacing reminders for each story beat.
    /// Subscribes to EchoEvents.BeatChanged only — no Core or World references.
    /// </summary>
    public class ObjectiveHints : MonoBehaviour
    {
        [Header("Tuning (InteractTuning)")]
        [SerializeField] private float fadeInSeconds = 1f;
        [SerializeField] private float visibleSeconds = 5f;
        [SerializeField] private float minReminderInterval = 3f;
        [SerializeField] private float counterPollInterval = 0.25f;

        [Header("References")]
        [SerializeField] private Text hintText;
        [SerializeField] private ObjectHintsDataAsset hintsData;

        private float counterTimer;
        private int lastCoresFound;
        private float reminderTimer;
        private float minReminderTimer;
        private bool isSuppressed;
        private string currentObjectiveText;
        private bool isShowing;

        private void OnEnable()
        {
            EchoEvents.BeatChanged += OnBeatChanged;
            counterTimer = Time.time;
            lastCoresFound = Progress.CoresFound;
            reminderTimer = 0f;
            minReminderTimer = 0f;
            isShowing = false;
            currentObjectiveText = "";
            if (hintText != null) { hintText.text = ""; hintText.color = new Color32(255,255,255,0); }
        }

        private void OnDisable()
        {
            EchoEvents.BeatChanged -= OnBeatChanged;
        }

        private void Update()
        {
            // CoreHunt counter: poll Progress.CoresFound on a timer, no Find* every frame
            if (Time.time - counterTimer >= counterPollInterval)
            {
                counterTimer = Time.time;
                UpdateCoreCounter();
            }

            // Fade-out: after visibleSeconds, fade the objective text out
            if (isShowing && hintText != null)
            {
                float age = Time.time - showStartTime;
                if (age >= visibleSeconds)
                {
                    // Fade out over fadeInSeconds
                    float t = (age - visibleSeconds) / fadeInSeconds;
                    hintText.color = new Color32(255,255,255, Mathf.Max(0, 255 - (int)(t * 255 / fadeInSeconds)));
                    if (age >= visibleSeconds + fadeInSeconds)
                    {
                        isShowing = false;
                        hintText.color = new Color32(255,255,255,0);
                        hintText.text = "";
                    }
                }
                else
                {
                    // Keep fully visible while within the visible period
                    if (hintText.color.a < 255)
                    {
                        hintText.color = new Color32(255,255,255,255);
                    }
                }
            }

            // Reminder scheduling: one at a time, minimum interval enforced
            if (!isShowing && !isSuppressed && !string.IsNullOrEmpty(currentObjectiveText))
            {
                reminderTimer += Time.deltaTime;
                minReminderTimer += Time.deltaTime;

                if (minReminderTimer >= visibleSeconds + minReminderInterval)
                {
                    minReminderTimer = 0f;
                    ShowReminder();
                }
            }
        }

        private void UpdateCoreCounter()
        {
            int currentCores = Progress.CoresFound;
            if (currentCores != lastCoresFound)
            {
                lastCoresFound = currentCores;
                if (hintText != null) hintText.text = $"Cores: {currentCores}/3";
            }
        }

        private void OnBeatChanged(Beat newBeat)
        {
            // Cancel any pending objective/reminder when beat changes
            isShowing = false;
            reminderTimer = 0f;
            minReminderTimer = 0f;
            if (hintText != null) { hintText.color = new Color32(255,255,255,0); hintText.text = ""; }

            // Read the beat's objective info from CSV via the data asset
            if (hintsData != null && hintsData.TryGetBeatInfo(newBeat, out var info))
            {
                currentObjectiveText = info.objectiveText;
                if (!isSuppressed && info.showObjective && !string.IsNullOrEmpty(info.objectiveText))
                {
                    ShowObjective(info.objectiveText);
                    isShowing = true;
                    showStartTime = Time.time;
                }
                else
                {
                    currentObjectiveText = "";
                    if (hintText != null) { hintText.text = ""; }
                }
            }
            else
            {
                currentObjectiveText = "";
                if (hintText != null) { hintText.text = ""; }
            }
        }

        private void ShowObjective(string text)
        {
            if (hintText == null) return;
            currentObjectiveText = text;
            hintText.text = text;
            hintText.color = new Color32(255,255,255,0);
            hintText.CrossFadeAlpha(1f, fadeInSeconds, false);
            isShowing = true;
            showStartTime = Time.time;
        }

        private void ShowReminder()
        {
            if (isSuppressed) return;
            if (hintText == null || string.IsNullOrEmpty(currentObjectiveText)) return;
            hintText.CrossFadeAlpha(1f, fadeInSeconds, false);
            isShowing = true;
            showStartTime = Time.time;
        }

        public void SetSuppressed(bool suppressed)
        {
            isSuppressed = suppressed;
            if (suppressed)
            {
                isShowing = false;
                reminderTimer = 0f;
                minReminderTimer = 0f;
                if (hintText != null) { hintText.color = new Color32(255,255,255,0); hintText.text = ""; }
                currentObjectiveText = "";
            }
        }

        private void UpdateCoreCounter()
        {
            int currentCores = Progress.CoresFound;
            if (currentCores != lastCoresFound)
            {
                lastCoresFound = currentCores;
                if (hintText != null) hintText.text = $"Cores: {currentCores}/3";
            }
        }
    }

    /// <summary>
    /// Data structure for objective hint info read from objectives.csv.
    /// </summary>
    public struct ObjectHintInfo
    {
        public Beat beat;
        public bool showObjective;
        public string objectiveText;
        public string reminderText;
        public float reminderDelaySeconds;
    }

    /// <summary>
    /// ScriptableObject lookup for objectives.csv data.
    /// Created via ECHO > Create > Interact > Objectives Data in the Editor.
    /// </summary>
    [CreateAssetMenu(menuName = "ECHO/Interact/Objectives Data")]
    public class ObjectHintsDataAsset : ScriptableObject
    {
        [SerializeField] private ObjectHintInfo[] beatInfos;

        public bool TryGetBeatInfo(Beat beat, out ObjectHintInfo info)
        {
            info = default;
            foreach (var entry in beatInfos)
            {
                if (entry.beat == beat)
                {
                    info = entry;
                    return true;
                }
            }
            return false;
        }
    }
}