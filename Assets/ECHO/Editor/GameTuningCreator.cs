using System.IO;
using UnityEditor;
using UnityEngine;
using Echo.Shared;

namespace Echo.Editor
{
    /// <summary>
    /// Editor tool to create or update the GameTuning ScriptableObject asset
    /// according to the exact specifications in Tune.md.
    /// Access via menu: ECHO > Create or Update GameTuning Asset.
    /// </summary>
    public static class GameTuningCreator
    {
        private const string AssetDir = "Assets/ECHO/Data";
        private const string AssetPath = "Assets/ECHO/Data/GameTuning.asset";

        [MenuItem("ECHO/Create or Update GameTuning Asset", priority = 50)]
        public static void CreateOrUpdateGameTuning()
        {
            if (!Directory.Exists(AssetDir))
            {
                Directory.CreateDirectory(AssetDir);
                AssetDatabase.Refresh();
            }

            GameTuning tuning = AssetDatabase.LoadAssetAtPath<GameTuning>(AssetPath);
            bool isNew = (tuning == null);

            if (isNew)
            {
                tuning = ScriptableObject.CreateInstance<GameTuning>();
            }

            // ── Player Noise & Footsteps ──────────────────────────────────────────
            tuning.crouchLoudness           = 0.10f;   // nearly silent
            tuning.walkLoudness             = 0.40f;   // medium range
            tuning.runLoudness              = 1.00f;   // full radius
            tuning.crouchStepDistance       = 1.5f;
            tuning.walkStepDistance         = 2.0f;
            tuning.runStepDistance          = 2.5f;
            tuning.runContinuousInterval    = 0.35f;   // extra pulse every 0.35 s while sprinting
            tuning.footstepIntervalCrouch   = 0.9f;
            tuning.footstepIntervalWalk     = 0.55f;
            tuning.footstepIntervalRun      = 0.35f;

            // ── Monster Hearing & Alert (C-05) ────────────────────────────────────
            tuning.hearingRadius         = 18.0f;
            tuning.crouchHearingRadius   = 2.0f;
            tuning.alertGainScale        = 1.0f;
            tuning.alertDecayPerSecond   = 0.15f;
            tuning.investigateThreshold  = 0.40f;   // raised: one crouch step won't trigger
            tuning.chaseThreshold        = 1.20f;   // raised: needs sustained noise to Chase
            tuning.searchTimeout         = 12.0f;

            // ── Monster Movement, Search & Memory (C-05) ──────────────────────────
            tuning.speedInvestigate = 2.2f;
            tuning.speedChase = 5.0f;
            tuning.speedSearch = 1.8f;
            tuning.killRange = 1.45f;
            tuning.searchRadius = 8.0f;
            tuning.noiseMemory = 6.0f;

            // ── Lunge Sequence (C-07) ─────────────────────────────────────────────
            tuning.lungeNoticePause = 1.5f;
            tuning.lungeDashSpeed = 8.0f;
            tuning.lungeBangDuration = 6.0f;
            tuning.lungeBangInterval = 0.8f;

            // ── Hide Mechanics & Rules (C-08 / I-04) ──────────────────────────────
            tuning.safeHidesBeforeDeath = 3;
            tuning.instantKillDelay = 1.2f;
            tuning.monsterArriveTimeout = 5.0f;
            tuning.hideBangInterval = 0.8f;
            tuning.hideCooldown = 2.0f;
            tuning.bangFillPerSecond = 0.35f;
            tuning.bangDecayPerSecond = 0.5f;

            if (isNew)
            {
                AssetDatabase.CreateAsset(tuning, AssetPath);
                Debug.Log($"[ECHO] Created new GameTuning asset at {AssetPath} with Tune.md settings.");
            }
            else
            {
                EditorUtility.SetDirty(tuning);
                Debug.Log($"[ECHO] Updated existing GameTuning asset at {AssetPath} with Tune.md settings.");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Ping and select in project window
            Selection.activeObject = tuning;
            EditorGUIUtility.PingObject(tuning);
        }
    }
}

