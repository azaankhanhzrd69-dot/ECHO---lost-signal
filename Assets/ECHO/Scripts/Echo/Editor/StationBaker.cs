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
    /// Generates the station into the active scene in Edit Mode and persists procedural assets.
    /// </summary>
    public static class StationBaker
    {
        private const string MaterialDir = "Assets/ECHO/Generated/Materials";
        private const string TextureDir = "Assets/ECHO/Generated/Textures";
        private const string MeshDir = "Assets/ECHO/Generated/Meshes";
        private const string SceneDir = "Assets/ECHO/Scenes";

        [MenuItem("ECHO/World Builder/Generate Complete Station", priority = 100)]
        public static void GenerateCompleteStation()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("ECHO World Builder", "Exit Play Mode before generating the station.", "OK");
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();
            if (Application.isBatchMode && (!scene.IsValid() || string.IsNullOrEmpty(scene.path)))
            {
                scene = EditorSceneManager.OpenScene(
                    "Assets/ECHO/Scenes/Station_Baked.unity",
                    OpenSceneMode.Single);
            }

            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("ECHO World Builder", "There is no valid active scene.", "OK");
                return;
            }

            string savePath = scene.path;
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = EditorUtility.SaveFilePanelInProject(
                    "Save Generated ECHO Scene", "Station", "unity",
                    "Choose where to save the generated station scene.", SceneDir);
                if (string.IsNullOrEmpty(savePath))
                    return;
            }

            if (!Application.isBatchMode && !EditorUtility.DisplayDialog(
                    "Generate Complete Station",
                    "Regenerate the station in the active scene and save it. Other scene objects will be preserved.",
                    "Generate",
                    "Cancel"))
                return;

            EnsureDirectories();

            const string generatedRootName = "Station - Deck 03";
            const string stagingRootName = "__ECHO_StationBuild";
            const string bakedModeFieldName = "useBakedStation";
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Generate Complete ECHO Station");
            Undo.IncrementCurrentGroup();

            GameObject tempController = new GameObject("__StationBaker_TempController");
            tempController.SetActive(false);
            SceneManager.MoveGameObjectToScene(tempController, scene);
            EchoGame game = tempController.AddComponent<EchoGame>();
            FieldInfo instanceField = GetRequiredField(typeof(EchoGame), "instance", flags);
            instanceField.SetValue(null, game);
            GameObject worldRootGO = new GameObject(stagingRootName);
            SceneManager.MoveGameObjectToScene(worldRootGO, scene);
            Undo.RegisterCreatedObjectUndo(worldRootGO, "Generate Complete ECHO Station");
            GameObject[] oldStationRoots = GetStationRoots(scene, generatedRootName);
            bool generationCompleted = false;

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

                InvokeRequired(game, "BuildNebulaSkybox", flags);
                InvokeRequired(game, "CreateMaterials", flags);
                InvokeRequired(game, "LoadStationKit", flags);

                FieldInfo offsetField = GetRequiredField(typeof(EchoGame), "StationEditorOffset", flags);
                Vector3 offset = (Vector3)offsetField.GetValue(null);
                worldRootGO.transform.position = offset;

                FieldInfo worldRootField = GetRequiredField(typeof(EchoGame), "worldRoot", flags);
                worldRootField.SetValue(game, worldRootGO.transform);

                InvokeRequired(game, "BuildStation", flags);
                InvokeRequired(game, "BuildModularAnnex", flags);
                InvokeRequired(game, "BuildSpaceBackdrop", flags);
                InvokeRequired(game, "BuildBlackHole", flags);
                InvokeRequired(game, "BuildEscapeShip", flags);
                InvokeRequired(game, "BuildDecor", flags);

                // Add station interactables (cores, consoles, relays, notes, wire leads)
                MethodInfo addInteractableMethod = GetRequiredMethod(typeof(EchoGame), "AddInteractable", flags);
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

                    FieldInfo wiresField = GetRequiredField(typeof(EchoGame), "reactorWires", flags);
                    Array wiresArray = wiresField.GetValue(game) as Array;
                    if (wiresArray == null)
                        throw new InvalidOperationException("EchoGame.reactorWires is not an array.");
                    for (int i = 0; i < wiresArray.Length; i++)
                    {
                        addInteractableMethod.Invoke(game, new object[] { "WIRE", i, "Connect reactor lead " + (i + 1), new Vector3(18.2f + i * 0.8f, 0.95f, -1.9f), false });
                    }
                }

                // Create station stencils / signs
                MethodInfo createSignMethod = GetRequiredMethod(typeof(EchoGame), "CreateSign", flags);
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

                InvokeRequired(game, "BuildRoomLockersAndProps", flags);
                CleanUpNonSolidColliders(worldRootGO);

                Renderer[] allRenderers = worldRootGO.GetComponentsInChildren<Renderer>(true);
                for (int i = 0; i < allRenderers.Length; i++)
                {
                    allRenderers[i].enabled = true;
                }

                PersistGeneratedAssets(worldRootGO);
                Debug.Log("[ECHO] All procedural station assets persisted.");

                OrganizeStationHierarchy(worldRootGO.transform);
                worldRootGO.name = generatedRootName;
                EchoGame sceneGame = GetOrCreateSceneGame(scene, game);
                ConfigureBakedGame(sceneGame, bakedModeFieldName);
                for (int i = 0; i < oldStationRoots.Length; i++)
                {
                    if (oldStationRoots[i] != null && oldStationRoots[i] != worldRootGO)
                        Undo.DestroyObjectImmediate(oldStationRoots[i]);
                }

                generationCompleted = true;
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                throw;
            }
            finally
            {
                instanceField.SetValue(null, null);
                if (tempController != null)
                {
                    UnityEngine.Object.DestroyImmediate(tempController);
                }
                if (!generationCompleted && worldRootGO != null)
                    UnityEngine.Object.DestroyImmediate(worldRootGO);
            }

            if (!EditorSceneManager.SaveScene(scene, savePath))
                throw new InvalidOperationException("Unity could not save the generated station scene to " + savePath + ".");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = worldRootGO;
            EditorGUIUtility.PingObject(worldRootGO);
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
                sceneView.FrameSelected();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "ECHO World Builder",
                    "Station generated in the active scene and saved to:\n" + savePath +
                    "\n\nGenerated materials, textures, and meshes are saved under Assets/ECHO/Generated/.",
                    "OK");
            }
            Debug.Log("[ECHO] Complete station generated in " + savePath);
        }

        private static FieldInfo GetRequiredField(Type type, string name, BindingFlags flags)
        {
            FieldInfo field = type.GetField(name, flags);
            if (field == null)
                throw new MissingFieldException(type.FullName, name);
            return field;
        }

        private static MethodInfo GetRequiredMethod(Type type, string name, BindingFlags flags)
        {
            MethodInfo method = type.GetMethod(name, flags);
            if (method == null)
                throw new MissingMethodException(type.FullName, name);
            return method;
        }

        private static void InvokeRequired(EchoGame game, string methodName, BindingFlags flags)
        {
            GetRequiredMethod(typeof(EchoGame), methodName, flags).Invoke(game, null);
        }

        private static GameObject[] GetStationRoots(Scene scene, string rootName)
        {
            List<GameObject> roots = new List<GameObject>();
            GameObject[] sceneRoots = scene.GetRootGameObjects();
            for (int i = 0; i < sceneRoots.Length; i++)
            {
                if (sceneRoots[i].name == rootName)
                    roots.Add(sceneRoots[i]);
            }
            return roots.ToArray();
        }

        private static EchoGame GetOrCreateSceneGame(Scene scene, EchoGame temporaryGame)
        {
            EchoGame[] sceneGames = UnityEngine.Object.FindObjectsByType<EchoGame>(FindObjectsInactive.Include);
            EchoGame found = null;
            for (int i = 0; i < sceneGames.Length; i++)
            {
                if (sceneGames[i] == temporaryGame || sceneGames[i].gameObject.scene != scene)
                    continue;
                if (found != null)
                    throw new InvalidOperationException("The active scene contains more than one EchoGame component.");
                found = sceneGames[i];
            }

            if (found != null)
                return found;

            GameObject host = new GameObject("ECHO - THE LAST SIGNAL");
            SceneManager.MoveGameObjectToScene(host, scene);
            Undo.RegisterCreatedObjectUndo(host, "Add ECHO Gameplay Host");
            return host.AddComponent<EchoGame>();
        }

        private static void ConfigureBakedGame(EchoGame game, string bakedModeFieldName)
        {
            SerializedObject serializedGame = new SerializedObject(game);
            SerializedProperty bakedMode = serializedGame.FindProperty(bakedModeFieldName);
            if (bakedMode == null)
                throw new MissingFieldException(typeof(EchoGame).FullName, bakedModeFieldName);

            Undo.RecordObject(game, "Enable Baked ECHO Station");
            bakedMode.boolValue = true;
            serializedGame.ApplyModifiedProperties();
            EditorUtility.SetDirty(game);
        }

        private static void OrganizeStationHierarchy(Transform stationRoot)
        {
            Transform core = CreateGroup(stationRoot, "01 - Station Core");
            Transform annex = CreateGroup(stationRoot, "02 - Modular Annex");
            Transform extraction = CreateGroup(stationRoot, "03 - Extraction");
            Transform exterior = CreateGroup(stationRoot, "04 - Exterior and Backdrop");

            List<Transform> generatedObjects = new List<Transform>();
            for (int i = 0; i < stationRoot.childCount; i++)
            {
                Transform child = stationRoot.GetChild(i);
                if (child != core && child != annex && child != extraction && child != exterior)
                    generatedObjects.Add(child);
            }

            for (int i = 0; i < generatedObjects.Count; i++)
            {
                Transform child = generatedObjects[i];
                Vector3 position = child.localPosition;
                Transform group;
                if (Mathf.Abs(position.z) > 30f || position.x < -48f || position.x > 90f)
                    group = exterior;
                else if (position.x < 32f)
                    group = core;
                else if (position.x <= 83f)
                    group = annex;
                else
                    group = extraction;

                child.SetParent(group, false);
            }
        }

        private static Transform CreateGroup(Transform parent, string name)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);
            return group.transform;
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
                    skyboxMat.name = Path.GetFileNameWithoutExtension(skyMatPath);
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
            if (loaded == null)
                throw new InvalidOperationException("Unity failed to import generated texture asset " + pngPath + ".");

            cache[sourceTex] = loaded;
            return loaded;
        }

        private static Mesh PersistMesh(Mesh sourceMesh, string fallbackName, Dictionary<Mesh, Mesh> cache)
        {
            if (cache.TryGetValue(sourceMesh, out Mesh cached))
                return cached;

            string safeName = SanitizeName(string.IsNullOrEmpty(sourceMesh.name) ? fallbackName : sourceMesh.name);
            sourceMesh.name = safeName;
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
            sourceMat.name = safeName;
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
