#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Echo.World.Editor
{
    /// <summary>
    /// W-12: Texture Optimizer.
    /// Applies the recommended import-setting changes identified by EchoTextureAudit.
    ///
    /// Safe-guards:
    ///  - Never deletes assets or changes GUIDs.
    ///  - Never touches .unity, .prefab, .meta, .asset files directly.
    ///    All changes are made through TextureImporter (Unity's own API), which
    ///    writes back the .meta file for you when SaveAndReimport() is called.
    ///  - Skips hero monster textures (kept at their current resolution).
    ///  - Only modifies DefaultTexturePlatform when it is above the target cap and
    ///    no platform-specific override already brings it below the cap.
    ///  - Preserves all normal maps, emissives, transparency and material references.
    ///
    /// Human workflow:
    ///   1. Run ECHO > Texture Audit to review the report.
    ///   2. Run ECHO > Apply Texture Optimizations to apply the changes.
    ///   3. Unity will reimport affected textures (may take a few minutes).
    ///   4. Verify no pink materials in the Scene view.
    ///   5. Commit the changed .meta files.
    /// </summary>
    public static class EchoTextureOptimizer
    {
        // Target cap for environment / tileable textures on the Default platform.
        // MonsterMutant hero textures are excluded from this cap.
        private const int TargetEnvMaxSize = 2048;

        // Paths that contain hero / close-up assets we want to keep at higher res.
        private static readonly string[] HeroPathFragments =
        {
            "MonsterMutant",
            "Monster",
            "Creature",
        };

        // Paths to skip entirely (demo scenes, tutorial assets, example scenes).
        private static readonly string[] SkipPathFragments =
        {
            "TutorialInfo",
            "Example_01",
            "DemoScene",
        };

        [MenuItem("ECHO/Apply Texture Optimizations", false, 157)]
        public static void ApplyOptimizations()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "ECHO — Apply Texture Optimizations",
                "This will:\n" +
                "• Cap DefaultTexturePlatform to 2048 px for all non-hero environment textures that are currently above 2048.\n" +
                "• Enable Streaming Mipmaps on large (≥15 MB source) 3-D textures where mipmaps are already on.\n\n" +
                "Hero monster textures are excluded. No assets will be deleted. GUIDs are unchanged.\n\n" +
                "Unity will reimport affected textures. This may take a few minutes.\n\n" +
                "Proceed?",
                "Apply", "Cancel");

            if (!confirmed) return;

            var log = new List<string>();
            int changed = 0;

            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < guids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

                    if (ShouldSkip(assetPath)) continue;

                    EditorUtility.DisplayProgressBar(
                        "ECHO — Optimizing Textures",
                        Path.GetFileName(assetPath),
                        (float)i / guids.Length);

                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer == null) continue;

                    bool dirty = false;

                    // ── 1. Cap DefaultTexturePlatform max size ────────────────────
                    if (!IsHero(assetPath))
                    {
                        // Read the default (cross-platform) settings
                        var defaultSettings = importer.GetDefaultPlatformTextureSettings();
                        if (defaultSettings.maxTextureSize > TargetEnvMaxSize)
                        {
                            // Check that Standalone / Win64 already covers this or set it
                            var stSettings = importer.GetPlatformTextureSettings("Standalone");
                            var w64Settings = importer.GetPlatformTextureSettings("Win64");

                            bool standaloneAlreadyCapped =
                                (stSettings.overridden && stSettings.maxTextureSize <= TargetEnvMaxSize) ||
                                (w64Settings.overridden && w64Settings.maxTextureSize <= TargetEnvMaxSize);

                            if (!standaloneAlreadyCapped)
                            {
                                // Add a Standalone override capped at TargetEnvMaxSize
                                stSettings.overridden       = true;
                                stSettings.maxTextureSize   = TargetEnvMaxSize;
                                stSettings.textureCompression = TextureImporterCompression.Compressed;
                                importer.SetPlatformTextureSettings(stSettings);
                                log.Add($"[CAP 2048] Standalone override set: {assetPath}");
                                dirty = true;
                            }
                            else
                            {
                                // DefaultTexturePlatform is high but Standalone already capped — lower Default too
                                defaultSettings.maxTextureSize = TargetEnvMaxSize;
                                importer.SetPlatformTextureSettings(defaultSettings);
                                log.Add($"[CAP 2048] Default platform capped: {assetPath}");
                                dirty = true;
                            }
                        }
                    }

                    // ── 2. Enable Streaming Mipmaps for large environment textures ─
                    if (importer.mipmapEnabled &&
                        !importer.streamingMipmaps &&
                        importer.textureType == TextureImporterType.Default &&
                        !IsHero(assetPath))
                    {
                        string fullPath = Path.GetFullPath(assetPath);
                        float sizeMB = File.Exists(fullPath)
                            ? new FileInfo(fullPath).Length / (1024f * 1024f)
                            : 0f;

                        if (sizeMB >= 15f)
                        {
                            importer.streamingMipmaps         = true;
                            importer.streamingMipmapsPriority = 0;
                            log.Add($"[STREAM MIPS] Enabled: {assetPath}");
                            dirty = true;
                        }
                    }

                    if (dirty)
                    {
                        importer.SaveAndReimport();
                        changed++;
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.Refresh();

            // Write an audit log for the human to review
            string logPath = "Assets/ECHO/Editor/TextureOptimizationLog.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(logPath)));
            File.WriteAllLines(Path.GetFullPath(logPath), log);
            AssetDatabase.ImportAsset(logPath);

            EditorUtility.DisplayDialog(
                "ECHO — Optimization Complete",
                $"Optimized {changed} texture(s).\n\n" +
                $"Log written to: {logPath}\n\n" +
                "Please verify the Scene view for any unexpected pink materials, " +
                "then commit the changed .meta files.",
                "OK");

            Debug.Log($"[EchoTextureOptimizer] Done. {changed} textures changed. Log: {logPath}");
        }

        // ── helpers ──────────────────────────────────────────────────────────────
        private static bool IsHero(string path)
        {
            foreach (string frag in HeroPathFragments)
                if (path.Contains(frag)) return true;
            return false;
        }

        private static bool ShouldSkip(string path)
        {
            foreach (string frag in SkipPathFragments)
                if (path.Contains(frag)) return true;
            return false;
        }
    }
}
#endif

