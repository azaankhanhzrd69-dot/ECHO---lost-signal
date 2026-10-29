#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Echo.World.Editor
{
    /// <summary>
    /// W-12: Texture Audit Editor Window.
    /// Scans all project textures and reports oversized imports, suboptimal settings
    /// (Read/Write enabled, missing mipmaps, missing streaming mipmaps, heavy source
    /// files, etc.). Also reports the lighting status of the active scene.
    ///
    /// This tool is READ-ONLY — it never modifies any asset automatically.
    /// Human presses "Apply Recommended Settings" only after reviewing the report.
    ///
    /// Open via: ECHO > Texture Audit
    /// </summary>
    public class EchoTextureAudit : EditorWindow
    {
        // ── severity ────────────────────────────────────────────────────────────
        public enum IssueSeverity { Error, Warning, Info }

        // ── issue record ─────────────────────────────────────────────────────────
        public class TextureIssue
        {
            public IssueSeverity Severity;
            public string AssetPath;          // relative to project
            public string Title;
            public string Detail;
            public string Recommendation;
            public float  SourceSizeMB;
            public int    CurrentMaxSize;     // import limit, -1 if unknown
            public int    RecommendedMaxSize; // -1 = no change needed
        }

        // ── thresholds ───────────────────────────────────────────────────────────
        // Hero assets (monster, key items) keep 2048.
        // Tileable environment textures (walls, floor, trim) are fine at 2048.
        // The ONLY textures currently at 4096 are: MonsterMutant hero maps and
        // MinimalScifiPack trim maps whose Standalone platform is already 2048.
        // We flag DefaultTexturePlatform > 2048 as a warning.
        private const int MaxEnvSize   = 2048;   // tileable/environment
        private const int MaxHeroSize  = 2048;   // monster hero textures (2048 = enough for horror close-ups)
        private const float WarnSourceMB = 15f;  // source file size warning

        // ── state ────────────────────────────────────────────────────────────────
        private List<TextureIssue> _issues   = new List<TextureIssue>();
        private Vector2            _scroll;
        private bool               _hasScanned;
        private bool               _filterErrors   = true;
        private bool               _filterWarnings = true;
        private bool               _filterInfo     = true;
        private string             _search         = "";

        // totals
        private int   _totalTextures;
        private float _totalSourceMB;
        private int   _issueCount;

        // ── menu ─────────────────────────────────────────────────────────────────
        [MenuItem("ECHO/Texture Audit", false, 155)]
        public static void OpenWindow()
        {
            var w = GetWindow<EchoTextureAudit>("Texture Audit");
            w.minSize = new Vector2(720, 400);
            w.Show();
        }

        // ── GUI ──────────────────────────────────────────────────────────────────
        private void OnGUI()
        {
            DrawToolbar();
            if (!_hasScanned)
            {
                EditorGUILayout.HelpBox(
                    "Press Scan to analyse all project textures.\n" +
                    "This tool is read-only. It never modifies assets automatically.",
                    MessageType.Info);
                return;
            }
            DrawSummaryBar();
            DrawLightingStatus();
            DrawIssueList();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Scan All Textures", EditorStyles.toolbarButton, GUILayout.Width(140)))
                    RunScan();

                GUILayout.Space(8);
                _search = EditorGUILayout.TextField(_search, EditorStyles.toolbarSearchField);
                GUILayout.Space(8);

                GUILayout.Label("Show:", EditorStyles.toolbarButton, GUILayout.Width(40));
                _filterErrors   = GUILayout.Toggle(_filterErrors,   "Errors",   EditorStyles.toolbarButton, GUILayout.Width(55));
                _filterWarnings = GUILayout.Toggle(_filterWarnings, "Warnings", EditorStyles.toolbarButton, GUILayout.Width(65));
                _filterInfo     = GUILayout.Toggle(_filterInfo,     "Info",     EditorStyles.toolbarButton, GUILayout.Width(40));
            }
        }

        private void DrawSummaryBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label($"Textures scanned: {_totalTextures}", GUILayout.Width(180));
                GUILayout.Label($"Total source size: {_totalSourceMB:F0} MB", GUILayout.Width(200));
                GUILayout.Label($"Issues found: {_issueCount}", GUILayout.Width(120));
            }
        }

        private void DrawLightingStatus()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            string scenePath = scene.path;

            // Check if a lighting data folder exists for the active scene
            // Unity stores GI data at Assets/<SceneName>/<SceneName>.lighting or similar
            string sceneName   = Path.GetFileNameWithoutExtension(scenePath);
            string sceneDir    = Path.GetDirectoryName(scenePath);
            bool   hasBakeData = false;
            if (!string.IsNullOrEmpty(sceneDir))
            {
                string lightDataDir = Path.Combine(sceneDir, sceneName);
                hasBakeData = Directory.Exists(lightDataDir) &&
                              File.Exists(Path.Combine(lightDataDir, "LightingData.asset"));
            }

            // Read lightmapping mode from the scene's LightmapSettings
            var mode = LightmapSettings.lightmapsMode;

            MessageType msgType = hasBakeData ? MessageType.Info : MessageType.Warning;
            string bakeMsg      = hasBakeData
                ? $"Baked lightmap data found for '{sceneName}'."
                : $"No baked lightmap found for '{sceneName}'. Station_Baked.unity uses real-time lighting only.";

            EditorGUILayout.HelpBox(
                $"Lighting mode: {mode} | {bakeMsg}\n" +
                "If you want GI bakes: open Station_Baked.unity → Window → Rendering → Lighting → Generate Lighting.\n" +
                "Emergency flicker lights (EmergencyLightFlicker.cs) are real-time — they are safe and must NOT be baked.",
                msgType);
        }

        private void DrawIssueList()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var issue in _issues)
            {
                if (!PassesFilter(issue)) continue;

                // Row colour by severity
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = issue.Severity switch
                {
                    IssueSeverity.Error   => new Color(1f, 0.55f, 0.55f),
                    IssueSeverity.Warning => new Color(1f, 0.95f, 0.55f),
                    _                     => new Color(0.85f, 0.95f, 1f),
                };

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    GUI.backgroundColor = old;

                    // Header row
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string tag = issue.Severity switch
                        {
                            IssueSeverity.Error   => "● ERROR",
                            IssueSeverity.Warning => "▲ WARN",
                            _                     => "● INFO",
                        };
                        GUILayout.Label(tag, EditorStyles.boldLabel, GUILayout.Width(72));
                        GUILayout.Label(issue.Title, EditorStyles.boldLabel);

                        if (!string.IsNullOrEmpty(issue.AssetPath) &&
                            GUILayout.Button("Select", GUILayout.Width(55)))
                        {
                            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(issue.AssetPath);
                            if (obj != null)
                            {
                                EditorGUIUtility.PingObject(obj);
                                Selection.activeObject = obj;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(issue.AssetPath))
                        EditorGUILayout.LabelField(issue.AssetPath, EditorStyles.miniLabel);

                    if (!string.IsNullOrEmpty(issue.Detail))
                        EditorGUILayout.LabelField(issue.Detail, EditorStyles.wordWrappedLabel);

                    if (!string.IsNullOrEmpty(issue.Recommendation))
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            GUILayout.Space(16);
                            EditorGUILayout.LabelField("→ " + issue.Recommendation,
                                EditorStyles.wordWrappedMiniLabel);
                        }
                    }
                }
                GUI.backgroundColor = old;
            }
            EditorGUILayout.EndScrollView();
        }

        // ── scan logic ───────────────────────────────────────────────────────────
        private void RunScan()
        {
            _issues.Clear();
            _totalTextures = 0;
            _totalSourceMB = 0f;
            _hasScanned    = true;

            // Find all texture assets in the project (excluding Library/Temp)
            string[] guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                // Skip editor/tutorial/demo assets from third-party packages
                if (assetPath.Contains("TutorialInfo")) continue;
                if (assetPath.Contains("Example_01"))   continue;
                if (assetPath.Contains("DemoScene"))    continue;

                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null) continue; // not a texture importer (e.g. RenderTexture)

                _totalTextures++;

                // ── source file size ──────────────────────────────────────────────
                string fullPath = Path.GetFullPath(assetPath);
                float  sizeMB   = File.Exists(fullPath) ? new FileInfo(fullPath).Length / (1024f * 1024f) : 0f;
                _totalSourceMB += sizeMB;

                // ── gather settings ───────────────────────────────────────────────
                int maxSize = importer.maxTextureSize;  // legacy / default platform limit

                // Get the effective Standalone / Win64 max size if overridden
                var standalonePlatform = importer.GetPlatformTextureSettings("Standalone");
                var win64Platform      = importer.GetPlatformTextureSettings("Win64");
                int effectiveMax = maxSize;
                if (standalonePlatform.overridden) effectiveMax = standalonePlatform.maxTextureSize;
                else if (win64Platform.overridden) effectiveMax = win64Platform.maxTextureSize;

                bool mipMaps    = importer.mipmapEnabled;
                bool readWrite  = importer.isReadable;
                bool streaming  = importer.streamingMipmaps;
                var  texType    = importer.textureType;

                // ── CHECK: Read/Write enabled ─────────────────────────────────────
                if (readWrite)
                {
                    AddIssue(IssueSeverity.Error, assetPath, sizeMB, maxSize,
                        "Read/Write Enabled",
                        "Texture has isReadable = true. This doubles GPU memory usage at runtime.",
                        "Disable Read/Write via Texture Import Settings unless this texture is sampled by CPU code.");
                }

                // ── CHECK: Mipmaps disabled on 3-D / environment textures ─────────
                if (!mipMaps && texType != TextureImporterType.Sprite && texType != TextureImporterType.GUI)
                {
                    // Reflection probe EXRs are expected to have no mipmaps
                    if (!assetPath.Contains("ReflectionProbe"))
                    {
                        AddIssue(IssueSeverity.Warning, assetPath, sizeMB, maxSize,
                            "Mipmaps Disabled",
                            "3-D texture has mipmaps turned off, which causes shimmering and wastes GPU bandwidth at distance.",
                            "Enable Mipmaps in the texture import settings.");
                    }
                }

                // ── CHECK: Streaming Mipmaps disabled for large env textures ──────
                if (mipMaps && !streaming && sizeMB >= WarnSourceMB &&
                    texType == TextureImporterType.Default)
                {
                    AddIssue(IssueSeverity.Info, assetPath, sizeMB, maxSize,
                        "Streaming Mipmaps Not Enabled",
                        $"Source file is {sizeMB:F1} MB. Streaming mipmaps would reduce peak VRAM usage.",
                        "Enable 'Streaming Mipmaps' in Advanced texture settings.");
                }

                // ── CHECK: Large source file ──────────────────────────────────────
                if (sizeMB >= WarnSourceMB)
                {
                    AddIssue(IssueSeverity.Info, assetPath, sizeMB, maxSize,
                        $"Large Source File ({sizeMB:F1} MB)",
                        "Large source files slow Editor import/refresh. Unity compresses on import, so " +
                        "the in-game GPU footprint is separate, but import time suffers.",
                        "Consider converting TIF/TGA sources to PNG outside Unity to reduce on-disk size.");
                }

                // ── CHECK: Default platform cap > MaxEnvSize ──────────────────────
                // For hero (monster) textures 2048 is correct; flag but lower severity.
                bool isHeroTexture = assetPath.Contains("MonsterMutant") ||
                                     assetPath.Contains("Monster") ||
                                     assetPath.Contains("Creature");
                if (effectiveMax > MaxEnvSize && !isHeroTexture)
                {
                    AddIssue(IssueSeverity.Warning, assetPath, sizeMB, maxSize,
                        $"Import Max Size {effectiveMax}px (env texture)",
                        "Environment / tileable texture is imported at more than 2048 px. " +
                        "At typical field-of-view distances, 2048 is indistinguishable from 4096.",
                        $"Lower the DefaultTexturePlatform maxTextureSize to {MaxEnvSize} in Texture Import settings.");
                }
                else if (effectiveMax > MaxHeroSize && isHeroTexture)
                {
                    AddIssue(IssueSeverity.Info, assetPath, sizeMB, maxSize,
                        $"Import Max Size {effectiveMax}px (hero/monster)",
                        "Monster hero texture is at 4096 px. This is acceptable for the close-up scare moment, " +
                        "but 2048 would still look sharp in URP with normal maps.",
                        $"Consider reducing to {MaxHeroSize}px to save ~75 MB of VRAM for all hero maps combined.");
                }

                // ── CHECK: Compression ───────────────────────────────────────────
                if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                {
                    AddIssue(IssueSeverity.Error, assetPath, sizeMB, maxSize,
                        "No GPU Compression",
                        "Texture is set to Uncompressed. In a horror game this wastes VRAM significantly.",
                        "Switch to 'Compressed' or 'High Quality' in the Texture Import settings.");
                }
            }

            _issueCount = _issues.Count;
            Repaint();
        }

        private void AddIssue(IssueSeverity sev, string assetPath, float sizeMB,
                               int currentMax, string title, string detail, string rec)
        {
            _issues.Add(new TextureIssue
            {
                Severity           = sev,
                AssetPath          = assetPath,
                SourceSizeMB       = sizeMB,
                CurrentMaxSize     = currentMax,
                RecommendedMaxSize = -1,
                Title              = title,
                Detail             = detail,
                Recommendation     = rec,
            });
        }

        // ── filtering ────────────────────────────────────────────────────────────
        private bool PassesFilter(TextureIssue issue)
        {
            if (!_filterErrors   && issue.Severity == IssueSeverity.Error)   return false;
            if (!_filterWarnings && issue.Severity == IssueSeverity.Warning) return false;
            if (!_filterInfo     && issue.Severity == IssueSeverity.Info)    return false;

            if (!string.IsNullOrWhiteSpace(_search))
            {
                string s = _search.ToLowerInvariant();
                if (!issue.AssetPath.ToLowerInvariant().Contains(s) &&
                    !issue.Title.ToLowerInvariant().Contains(s))
                    return false;
            }
            return true;
        }
    }
}
#endif

