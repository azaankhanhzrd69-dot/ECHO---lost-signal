#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Echo.World.Editor
{
    /// <summary>
    /// W-10b / Atmosphere & Lighting tool:
    /// Configures room-based box-projected Reflection Probes across the space station,
    /// darkens the ambient lighting, enhances fog and post-processing, and sets up
    /// an eerie, dark horror atmosphere.
    ///
    /// Usage in Unity Editor:
    ///   ECHO > Lighting > Setup Dark Eerie Atmosphere & Reflections
    ///   ECHO > Lighting > Remove Generated Reflection Probes
    /// </summary>
    public static class EchoLightingSetup
    {
        private const string RootName = "[ECHO_LIGHTING_AND_REFLECTIONS]";

        // Room and corridor bounding boxes defined by the station layout
        private struct ProbeZone
        {
            public string Name;
            public Vector3 Position;
            public Vector3 BoxSize;
            public Vector3 BoxOffset;

            public ProbeZone(string name, Vector3 pos, Vector3 size, Vector3 offset)
            {
                Name = name;
                Position = pos;
                BoxSize = size;
                BoxOffset = offset;
            }
        }

        // Zones covering the primary station corridors and rooms on Deck 03:
        private static readonly ProbeZone[] Zones = new[]
        {
            // 1. Spawn / Engineering Airlock (-32 to -13, Z: -4 to 4)
            new ProbeZone("Probe_Spawn_Airlock", new Vector3(-22f, 2f, 0f), new Vector3(22f, 5f, 10f), Vector3.zero),

            // 2. West Corridor Hub (-13 to 0, Z: -4 to 4)
            new ProbeZone("Probe_West_Corridor", new Vector3(-6.5f, 2f, 0f), new Vector3(15f, 5f, 10f), Vector3.zero),

            // 3. Central Hub & Decompression Junction (0 to 14, Z: -4 to 4)
            new ProbeZone("Probe_Central_Corridor", new Vector3(7f, 2f, 0f), new Vector3(16f, 5f, 10f), Vector3.zero),

            // 4. MedBay (North Wing: X: -13 to 0, Z: 4 to 20)
            new ProbeZone("Probe_MedBay_North", new Vector3(-6.5f, 2f, 12f), new Vector3(15f, 5f, 18f), Vector3.zero),

            // 5. Crew Quarters (South Wing: X: -13 to 0, Z: -20 to -4)
            new ProbeZone("Probe_CrewQuarters_South", new Vector3(-6.5f, 2f, -12f), new Vector3(15f, 5f, 18f), Vector3.zero),

            // 6. Reactor Approach & Storage (North: X: 0 to 13, Z: 4 to 20)
            new ProbeZone("Probe_Storage_North", new Vector3(6.5f, 2f, 12f), new Vector3(15f, 5f, 18f), Vector3.zero),

            // 7. Life Support Wing (South: X: 0 to 13, Z: -20 to -4)
            new ProbeZone("Probe_LifeSupport_South", new Vector3(6.5f, 2f, -12f), new Vector3(15f, 5f, 18f), Vector3.zero),

            // 8. Main Reactor Core Room (X: 13 to 32, Z: -12 to 12)
            new ProbeZone("Probe_Reactor_Room", new Vector3(22.5f, 2.2f, 0f), new Vector3(21f, 6f, 26f), Vector3.zero),

            // 9. Annex Transition Corridor (X: 32 to 50, Z: -4 to 4)
            new ProbeZone("Probe_Annex_Corridor_West", new Vector3(41f, 2f, 0f), new Vector3(20f, 5f, 10f), Vector3.zero),

            // 10. Hydroponics Lab (North Annex: X: 42 to 62, Z: 6 to 20)
            new ProbeZone("Probe_Hydroponics_North", new Vector3(52f, 2.2f, 13f), new Vector3(22f, 5.5f, 16f), Vector3.zero),

            // 11. Maintenance Bay (South Annex: X: 42 to 62, Z: -20 to -6)
            new ProbeZone("Probe_Maintenance_South", new Vector3(52f, 2.2f, -13f), new Vector3(22f, 5.5f, 16f), Vector3.zero),

            // 12. Cargo Bay & Comms Center (X: 62 to 80, Z: -18 to 18)
            new ProbeZone("Probe_CargoBay_East", new Vector3(71f, 2.5f, 0f), new Vector3(20f, 6f, 38f), Vector3.zero),
        };

        [MenuItem("ECHO/Lighting/Setup Dark Eerie Atmosphere & Reflections", false, 160)]
        public static void SetupAtmosphereAndReflections()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Setup Dark Eerie Atmosphere & Reflections");

            // 1. Locate or create root GameObject in active scene
            GameObject root = GameObject.Find(RootName);
            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
            }

            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Atmosphere Root");

            // 2. Set up Room-Based Reflection Probes with Box Projection
            GameObject probesGroup = new GameObject("ReflectionProbes");
            probesGroup.transform.SetParent(root.transform, false);

            for (int i = 0; i < Zones.Length; i++)
            {
                ProbeZone z = Zones[i];
                GameObject probeGO = new GameObject(z.Name);
                probeGO.transform.SetParent(probesGroup.transform, false);
                probeGO.transform.position = z.Position;

                ReflectionProbe probe = probeGO.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Realtime;
                probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
                probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.AllFacesAtOnce;

                // Box projection gives metallic floors and walls realistic perspective-correct reflections
                probe.boxProjection = true;
                probe.size = z.BoxSize;
                probe.center = z.BoxOffset;

                // Probe resolution & clipping
                probe.resolution = 256;
                probe.nearClipPlane = 0.2f;
                probe.farClipPlane = 45f;
                probe.intensity = 0.95f;
                probe.blendDistance = 1.5f;
                probe.clearFlags = ReflectionProbeClearFlags.SolidColor;
                probe.backgroundColor = new Color(0.0005f, 0.0008f, 0.0012f, 1f);
            }

            // 3. Configure Eerie Horror Environment Lighting & Fog
            RenderSettings.ambientMode = AmbientMode.Flat;
            // Pitch-black ambient baseline (OLED blacks) with deep eerie navy tone
            RenderSettings.ambientLight = new Color(0.0003f, 0.0006f, 0.001f);
            RenderSettings.ambientIntensity = 0.002f;

            // Fog: Deep claustrophobic horror fog
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.0002f, 0.0004f, 0.0007f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.038f;

            // 4. Create Global Post-Processing Volume for Tonemapping, Vignette, Bloom, FilmGrain
            GameObject volumeGO = new GameObject("Global_Horror_PostVolume");
            volumeGO.transform.SetParent(root.transform, false);

            Volume volume = volumeGO.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;

            // Create or configure volume profile
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "ECHO_Horror_Atmosphere_Profile";

            // Tonemapping: ACES for high-contrast cinematic horror
            Tonemapping tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            // Vignette: Heavy dark edges framing player view
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.color.overrideState = true;
            vignette.color.value = Color.black;
            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.42f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.35f;

            // Bloom: Subtle cold glow on computer consoles and emergency lights
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.9f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.45f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.65f;
            bloom.tint.overrideState = true;
            bloom.tint.value = new Color(0.8f, 0.9f, 1.0f);

            // Film Grain: Gritty sci-fi horror sensor noise
            FilmGrain filmGrain = profile.Add<FilmGrain>(true);
            filmGrain.type.overrideState = true;
            filmGrain.type.value = FilmGrainLookup.Medium1;
            filmGrain.intensity.overrideState = true;
            filmGrain.intensity.value = 0.28f;
            filmGrain.response.overrideState = true;
            filmGrain.response.value = 0.8f;

            // Color Adjustments: Cold, high-contrast desaturated look
            ColorAdjustments colorAdjustments = profile.Add<ColorAdjustments>(true);
            colorAdjustments.contrast.overrideState = true;
            colorAdjustments.contrast.value = 18f;
            colorAdjustments.saturation.overrideState = true;
            colorAdjustments.saturation.value = -15f;
            colorAdjustments.postExposure.overrideState = true;
            colorAdjustments.postExposure.value = -0.2f;

            // Shadows, Midtones, Highlights: Push shadows toward deep inky cyan/black
            ShadowsMidtonesHighlights smh = profile.Add<ShadowsMidtonesHighlights>(true);
            smh.shadows.overrideState = true;
            smh.shadows.value = new Vector4(0.85f, 0.95f, 1.05f, -0.05f);

            volume.profile = profile;

            // Save profile asset to Generated folder so it persists cleanly
            const string profilePath = "Assets/ECHO/Generated/ECHO_Horror_Atmosphere_Profile.asset";
            AssetDatabase.CreateAsset(profile, profilePath);
            AssetDatabase.SaveAssets();

            Undo.CollapseUndoOperations(undoGroup);
            EditorUtility.SetDirty(root);

            Debug.Log($"[ECHO Lighting] Configured {Zones.Length} box-projected Reflection Probes, eerie atmospheric fog, deep blacks, and cinematic horror post-processing!");
            EditorUtility.DisplayDialog("ECHO Lighting & Reflections",
                $"Successfully created {Zones.Length} room reflection probes and applied dark eerie horror post-processing & fog.\n\n" +
                $"Root object: {RootName}\n" +
                $"Profile: {profilePath}",
                "OK");
        }

        [MenuItem("ECHO/Lighting/Remove Generated Reflection Probes", false, 161)]
        public static void RemoveGeneratedAtmosphere()
        {
            GameObject root = GameObject.Find(RootName);
            if (root != null)
            {
                Undo.DestroyObjectImmediate(root);
                Debug.Log("[ECHO Lighting] Removed generated lighting and reflection probes.");
            }
            else
            {
                Debug.Log("[ECHO Lighting] No generated atmosphere root found.");
            }
        }
    }
}
#endif
