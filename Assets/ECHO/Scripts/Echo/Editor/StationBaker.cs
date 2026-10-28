using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Echo.Editor
{
    /// <summary>
    /// Editor tool to bake the procedural station into a permanent scene (Assets/ECHO/Scenes/Station_Baked.unity).
    /// Runs station generation in Edit Mode via reflection on EchoGame without modifying runtime gameplay code.
    /// Persists procedural materials, textures (PNG), and meshes as assets.
    /// </summary>
    public static class StationBaker
    {
        private const string ScenePath = "Assets/ECHO/Scenes/Station_Baked.unity";
        private const string MaterialDir = "Assets/ECHO/Generated/Materials";
        private const string TextureDir = "Assets/ECHO/Generated/Textures";
        private const string MeshDir = "Assets/ECHO/Generated/Meshes";
        private const string SceneDir = "Assets/ECHO/Scenes";

        [MenuItem("ECHO/Bake Station", priority = 100)]
        public static void BakeStation()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("ECHO Station Baker", "Cannot bake station while in Play Mode. Please exit Play Mode first.", "OK");
                return;
            }

            Debug.Log("[ECHO] Starting station bake process...");

            // 1. Ensure output directories exist
            EnsureDirectories();

            // 2. Create fresh empty scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 3. Create temporary GameObject holding EchoGame (kept inactive so Awake does not execute runtime player/creature/audio logic)
            GameObject tempController = new GameObject("__StationBaker_TempController");
            tempController.SetActive(false);
            EchoGame game = tempController.AddComponent<EchoGame>();

            // Reflection binding flags
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

            // Set static instance so MakeMaterial -> Instance.materialTemplate resolves
            FieldInfo instanceField = typeof(EchoGame).GetField("instance", flags);
            instanceField?.SetValue(null, game);

            GameObject worldRootGO = null;

            try
            {
                // 4. Configure environmental RenderSettings
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.0003f, 0.0005f, 0.0008f);
                RenderSettings.ambientIntensity = 0.002f;
                RenderSettings.fog = true;
                RenderSettings.fogColor = new Color(0.0002f, 0.0003f, 0.0005f);
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogDensity = 0.065f;

                // Build panoramic nebula skybox via reflection
                MethodInfo buildSkyboxMethod = typeof(EchoGame).GetMethod("BuildNebulaSkybox", flags);
                buildSkyboxMethod?.Invoke(game, null);

                // Create station materials via reflection
                MethodInfo createMaterialsMethod = typeof(EchoGame).GetMethod("CreateMaterials", flags);
                createMaterialsMethod?.Invoke(game, null);

                // Load station kit prefabs via reflection (does NOT call LoadStationAudio to avoid missing audio crashes)
                MethodInfo loadStationKitMethod = typeof(EchoGame).GetMethod("LoadStationKit", flags);
                loadStationKitMethod?.Invoke(game, null);

                // 5. Create world root ("Station - Deck 03")
                worldRootGO = new GameObject("Station - Deck 03");
                FieldInfo offsetField = typeof(EchoGame).GetField("StationEditorOffset", flags);
                Vector3 offset = offsetField != null ? (Vector3)offsetField.GetValue(null) : Vector3.zero;
                worldRootGO.transform.position = offset;

                FieldInfo worldRootField = typeof(EchoGame).GetField("worldRoot", flags);
                worldRootField?.SetValue(game, worldRootGO.transform);

                // 6. Invoke procedural building methods via reflection
                MethodInfo buildStationMethod = typeof(EchoGame).GetMethod("BuildStation", flags);
                buildStationMethod?.Invoke(game, null);

                MethodInfo buildAnnexMethod = typeof(EchoGame).GetMethod("BuildModularAnnex", flags);
                buildAnnexMethod?.Invoke(game, null);

                MethodInfo buildBackdropMethod = typeof(EchoGame).GetMethod("BuildSpaceBackdrop", flags);
                buildBackdropMethod?.Invoke(game, null);

                MethodInfo buildBlackHoleMethod = typeof(EchoGame).GetMethod("BuildBlackHole", flags);
                buildBlackHoleMethod?.Invoke(game, null);

                MethodInfo buildEscapeShipMethod = typeof(EchoGame).GetMethod("BuildEscapeShip", flags);
                buildEscapeShipMethod?.Invoke(game, null);

                MethodInfo buildDecorMethod = typeof(EchoGame).GetMethod("BuildDecor", flags);
                buildDecorMethod?.Invoke(game, null);

                // Add station interactables (cores, consoles, relays, notes, wire leads)
                MethodInfo addInteractableMethod = typeof(EchoGame).GetMethod("AddInteractable", flags);
                if (addInteractableMethod != null)
                {
                    addInteractableMethod.Invoke(game, new object[] { "CORE", 0, "Take power core", new Vector3(8f, 0.25f, 12f), true });
                    addInteractableMethod.Invoke(game, new object[] { "CORE", 1, "Take power core", new Vector3(8f, 0.25f, -12f), true });
                    addInteractableMethod.Invoke(game, new object[] { "CORE", 2, "Take power core", new Vector3(18f, 0.25f, 6.5f), true });
                    addInteractableMethod.Invoke(game, new object[] { "PUZZLE", 0, "Access breaker panel", new Vector3(19f, 0.1f, -1.5f), false });
                    addInteractableMethod.Invoke(game, new object[] { "NOTE", 0, "Read maintenance note", new Vector3(-5.3f, 0.1f, 11f), false });
                    addInteractableMethod.Invoke(game, new object[] { "EXIT", 0, "Open the final airlock", new Vector3(76f, 0.1f, 0f), false });
                    addInteractableMethod.Invoke(game, new object[] { "RELAY", 0, "Align long-range relay", new Vector3(52f, 0.1f, 14f), false });
                    addInteractableMethod.Invoke(game, new object[] { "RELAY", 1, "Align backup relay", new Vector3(52f, 0.1f, -14f), false });
                    addInteractableMethod.Invoke(game, new object[] { "RELAY", 2, "Align cargo-bay relay", new Vector3(70f, 0.1f, 14f), false });
                    addInteractableMethod.Invoke(game, new object[] { "LOG", 0, "Play crew recording", new Vector3(47f, 0.1f, 16f), false });
                    addInteractableMethod.Invoke(game, new object[] { "LOG", 1, "Play crew recording", new Vector3(47f, 0.1f, -16f), false });
                    addInteractableMethod.Invoke(game, new object[] { "LOG", 2, "Play crew recording", new Vector3(70f, 0.1f, -14f), false });
                    addInteractableMethod.Invoke(game, new object[] { "NOTE", 1, "Read relay calibration note", new Vector3(46f, 0.1f, 17f), false });
                    addInteractableMethod.Invoke(game, new object[] { "SUPPLY", 0, "Open emergency supply case", new Vector3(57f, 0.1f, 17f), false });
                    addInteractableMethod.Invoke(game, new object[] { "SUPPLY", 1, "Open emergency supply case", new Vector3(57f, 0.1f, -17f), false });
                    addInteractableMethod.Invoke(game, new object[] { "SUPPLY", 2, "Open emergency supply case", new Vector3(72f, 0.1f, -11f), false });
                    addInteractableMethod.Invoke(game, new object[] { "COMMS", 0, "Calibrate the long-range array", new Vector3(72f, 0.1f, 0f), false });

                    FieldInfo wiresField = typeof(EchoGame).GetField("reactorWires", flags);
                    Array wiresArray = wiresField?.GetValue(game) as Array;
                    int wireCount = wiresArray != null ? wiresArray.Length : 3;
                    for (int i = 0; i < wireCount; i++)
                    {
                        addInteractableMethod.Invoke(game, new object[] { "WIRE", i, "Connect reactor lead " + (i + 1), new Vector3(18.2f + i * 0.8f, 0.95f, -1.9f), false });
                    }
                }

                // Create station stencils / signs
                MethodInfo createSignMethod = typeof(EchoGame).GetMethod("CreateSign", flags);
                if (createSignMethod != null)
                {
                    createSignMethod.Invoke(game, new object[] { "SPAWN  /  00", new Vector3(-25f, 2.65f, 3.72f), 0f });
                    createSignMethod.Invoke(game, new object[] { "ENGINEERING  /  01", new Vector3(-8f, 2.65f, 3.72f), 0f });
                    createSignMethod.Invoke(game, new object[] { "CREW QUARTERS  /  02", new Vector3(2f, 2.65f, 17.68f), 180f });
                    createSignMethod.Invoke(game, new object[] { "CREW QUARTERS  /  03", new Vector3(2f, 2.65f, -17.68f), 0f });
                    createSignMethod.Invoke(game, new object[] { "REACTOR  /  04", new Vector3(13.28f, 2.65f, 0f), 90f });
                    createSignMethod.Invoke(game, new object[] { "AIRLOCK  /  05", new Vector3(25.9f, 2.65f, 0f), 90f });
                    createSignMethod.Invoke(game, new object[] { "HYDROPONICS  /  06", new Vector3(51f, 2.8f, 19.5f), 180f });
                    createSignMethod.Invoke(game, new object[] { "MAINTENANCE  /  07", new Vector3(51f, 2.8f, -19.5f), 0f });
                    createSignMethod.Invoke(game, new object[] { "CARGO BAY  /  08", new Vector3(70f, 2.8f, 19.5f), 180f });
                }

                // 7. Post-process colliders on non-solid and decorative props
                // (In Edit Mode, Object.Destroy logs an error and fails to remove components; DestroyImmediate ensures correct collision state)
                CleanUpNonSolidColliders(worldRootGO);

                // 8. Re-enable all mesh renderers so the baked station is fully visible
                // (EchoPulseReveal.Prepare disables renderers for the dropped sonar ping)
                Renderer[] allRenderers = worldRootGO.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < allRenderers.Length; i++)
                {
                    allRenderers[i].enabled = true;
                }

                // 9. Persist all procedural assets (Textures, Meshes, Materials) to disk
                PersistGeneratedAssets(worldRootGO);

                Debug.Log("[ECHO] All procedural station assets persisted.");
            }
            finally
            {
                // 10. Clean up temporary controller
                instanceField?.SetValue(null, null);
                if (tempController != null)
                {
                    UnityEngine.Object.DestroyImmediate(tempController);
                }
            }

            // 11. Save baked scene
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("ECHO Station Baker", $"Station successfully baked to:\n{ScenePath}\n\nGenerated assets saved under Assets/ECHO/Generated/", "OK");
            Debug.Log($"[ECHO] Station bake complete. Saved scene to {ScenePath}");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(SceneDir)) Directory.CreateDirectory(SceneDir);
            if (!Directory.Exists(MaterialDir)) Directory.CreateDirectory(MaterialDir);
            if (!Directory.Exists(TextureDir)) Directory.CreateDirectory(TextureDir);
            if (!Directory.Exists(MeshDir)) Directory.CreateDirectory(MeshDir);
        }

        private static void CleanUpNonSolidColliders(GameObject root)
        {
            // Remove colliders from celestial bodies, stars, and non-solid cosmetic blocks
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider col = colliders[i];
                if (col == null) continue;
                string objName = col.gameObject.name;

                bool shouldRemove =
                    objName == "Distant star" ||
                    objName == "Event horizon / escape destination" ||
                    objName == "Blue planet beyond the viewport" ||
                    objName == "Distant moon beyond the viewport" ||
                    objName == "Relay carrier emitter" ||
                    objName == "Relay stand" ||
                    objName.StartsWith("Reactor lead connector") ||
                    objName.StartsWith("Floor seam") ||
                    objName.StartsWith("Power cell /");

                if (shouldRemove)
                {
                    UnityEngine.Object.DestroyImmediate(col);
                }
            }
        }

        private static void PersistGeneratedAssets(GameObject root)
        {
            Dictionary<Texture2D, Texture2D> savedTextures = new Dictionary<Texture2D, Texture2D>();
            Dictionary<Mesh, Mesh> savedMeshes = new Dictionary<Mesh, Mesh>();
            Dictionary<Material, Material> savedMaterials = new Dictionary<Material, Material>();

            // 1. Skybox Texture and Material
            Material skyboxMat = RenderSettings.skybox;
            if (skyboxMat != null)
            {
                if (skyboxMat.HasProperty("_MainTex"))
                {
                    Texture2D skyTex = skyboxMat.GetTexture("_MainTex") as Texture2D;
                    if (skyTex != null && !AssetDatabase.Contains(skyTex))
                    {
                        Texture2D persistedTex = PersistTexture(skyTex, "Skybox_AuroraNebula_Tex", savedTextures);
                        skyboxMat.SetTexture("_MainTex", persistedTex);
                    }
                }

                if (!AssetDatabase.Contains(skyboxMat))
                {
                    string skyMatPath = $"{MaterialDir}/Skybox_AuroraNebula.mat";
                    Material existingSkyMat = AssetDatabase.LoadAssetAtPath<Material>(skyMatPath);
                    if (existingSkyMat != null)
                    {
                        EditorUtility.CopySerialized(skyboxMat, existingSkyMat);
                        RenderSettings.skybox = existingSkyMat;
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(skyboxMat, skyMatPath);
                        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>(skyMatPath);
                    }
                }
            }

            // 2. Meshes (MeshFilter and MeshCollider)
            MeshFilter[] meshFilters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < meshFilters.Length; i++)
            {
                Mesh mesh = meshFilters[i].sharedMesh;
                if (mesh != null && !AssetDatabase.Contains(mesh))
                {
                    meshFilters[i].sharedMesh = PersistMesh(mesh, meshFilters[i].gameObject.name, savedMeshes);
                }
            }

            MeshCollider[] meshColliders = root.GetComponentsInChildren<MeshCollider>(true);
            for (int i = 0; i < meshColliders.Length; i++)
            {
                Mesh mesh = meshColliders[i].sharedMesh;
                if (mesh != null && !AssetDatabase.Contains(mesh))
                {
                    meshColliders[i].sharedMesh = PersistMesh(mesh, meshColliders[i].gameObject.name + "_Col", savedMeshes);
                }
            }

            // 3. Materials on Renderers
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer rend = renderers[r];
                Material[] mats = rend.sharedMaterials;
                bool changed = false;

                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat != null && !AssetDatabase.Contains(mat))
                    {
                        mats[m] = PersistMaterial(mat, savedMaterials, savedTextures);
                        changed = true;
                    }
                }

                if (changed)
                {
                    rend.sharedMaterials = mats;
                }
            }

            // 4. Materials on LineRenderers (Reactor wires)
            LineRenderer[] lines = root.GetComponentsInChildren<LineRenderer>(true);
            for (int l = 0; l < lines.Length; l++)
            {
                Material mat = lines[l].sharedMaterial;
                if (mat != null && !AssetDatabase.Contains(mat))
                {
                    lines[l].sharedMaterial = PersistMaterial(mat, savedMaterials, savedTextures);
                }
            }
        }

        private static Texture2D PersistTexture(Texture2D sourceTex, string defaultName, Dictionary<Texture2D, Texture2D> cache)
        {
            if (cache.TryGetValue(sourceTex, out Texture2D cached))
                return cached;

            string safeName = SanitizeName(string.IsNullOrEmpty(sourceTex.name) ? defaultName : sourceTex.name);
            string pngPath = $"{TextureDir}/{safeName}.png";

            try
            {
                byte[] bytes = sourceTex.EncodeToPNG();
                File.WriteAllBytes(pngPath, bytes);
                AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);

                TextureImporter importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.wrapMode = TextureWrapMode.Repeat;
                    importer.SaveAndReimport();
                }

                Texture2D loaded = AssetDatabase.LoadAssetAtPath<Texture2D>(pngPath);
                cache[sourceTex] = loaded;
                return loaded;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ECHO] Failed to encode texture '{safeName}' to PNG: {ex.Message}");
                return sourceTex;
            }
        }

        private static Mesh PersistMesh(Mesh sourceMesh, string fallbackName, Dictionary<Mesh, Mesh> cache)
        {
            if (cache.TryGetValue(sourceMesh, out Mesh cached))
                return cached;

            string safeName = SanitizeName(string.IsNullOrEmpty(sourceMesh.name) ? fallbackName : sourceMesh.name);
            string assetPath = $"{MeshDir}/{safeName}.asset";

            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(sourceMesh, existing);
                cache[sourceMesh] = existing;
                return existing;
            }

            AssetDatabase.CreateAsset(sourceMesh, assetPath);
            Mesh saved = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            cache[sourceMesh] = saved;
            return saved;
        }

        private static Material PersistMaterial(Material sourceMat, Dictionary<Material, Material> matCache, Dictionary<Texture2D, Texture2D> texCache)
        {
            if (matCache.TryGetValue(sourceMat, out Material cached))
                return cached;

            // Check if material has procedural textures that must be saved first
            if (sourceMat.HasProperty("_BaseMap"))
            {
                if (sourceMat.GetTexture("_BaseMap") is Texture2D baseTex && !AssetDatabase.Contains(baseTex))
                {
                    sourceMat.SetTexture("_BaseMap", PersistTexture(baseTex, sourceMat.name + "_Base", texCache));
                }
            }
            if (sourceMat.HasProperty("_MainTex"))
            {
                if (sourceMat.GetTexture("_MainTex") is Texture2D mainTex && !AssetDatabase.Contains(mainTex))
                {
                    sourceMat.SetTexture("_MainTex", PersistTexture(mainTex, sourceMat.name + "_Main", texCache));
                }
            }

            string safeName = SanitizeName(string.IsNullOrEmpty(sourceMat.name) ? "StationMaterial" : sourceMat.name);
            string assetPath = $"{MaterialDir}/{safeName}.mat";

            Material existing = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (existing != null)
            {
                EditorUtility.CopySerialized(sourceMat, existing);
                matCache[sourceMat] = existing;
                return existing;
            }

            AssetDatabase.CreateAsset(sourceMat, assetPath);
            Material saved = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            matCache[sourceMat] = saved;
            return saved;
        }

        private static string SanitizeName(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Asset";
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                raw = raw.Replace(c, '_');
            }
            return raw.Replace('/', '_').Replace('\\', '_').Replace(' ', '_').Trim();
        }
    }
}

