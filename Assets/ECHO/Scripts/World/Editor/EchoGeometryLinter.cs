#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace Echo.World.Editor
{
    /// <summary>
    /// Geometry Linter Editor Window (W-03).
    /// Detects overlapping or duplicate geometry, duplicate colliders, and potential z-fighting
    /// in space station scenes without modifying any scene objects.
    /// </summary>
    public class EchoGeometryLinter : EditorWindow
    {
        public enum IssueSeverity
        {
            Error,
            Warning,
            Information
        }

        public enum IssueCategory
        {
            DuplicateGameObject,
            OverlappingWalls,
            ColliderProblem,
            ZFighting
        }

        [Serializable]
        public class LintIssue
        {
            public IssueSeverity Severity;
            public IssueCategory Category;
            public string Title;
            public string Description;
            public GameObject PrimaryObject;
            public GameObject SecondaryObject;
            public string PrimaryPath;
            public string SecondaryPath;
            public Vector3 Location;
        }
        private readonly struct EntityPair : IEquatable<EntityPair>
        {
            public readonly EntityId A;
            public readonly EntityId B;

            public EntityPair(EntityId idA, EntityId idB)
            {
                if (idA < idB)
                {
                    A = idA;
                    B = idB;
                }
                else
                {
                    A = idB;
                    B = idA;
                }
            }

            public bool Equals(EntityPair other) => A == other.A && B == other.B;
            public override bool Equals(object obj) => obj is EntityPair other && Equals(other);
            public override int GetHashCode() => unchecked((A.GetHashCode() * 397) ^ B.GetHashCode());
        }


        // Search / Scan Settings
        private bool _scanAllLoadedScenes = false;
        private bool _filterErrors = true;
        private bool _filterWarnings = true;
        private bool _filterInfo = true;
        private string _searchFilter = "";

        // Detection thresholds
        private float _positionCoincidenceTolerance = 0.005f; // 5mm
        private float _wallOverlapMinVolume = 0.05f; // min volume overlap in cubic meters
        private float _zFightPlaneDistanceThreshold = 0.008f; // 8mm
        private float _gridCellSize = 8.0f; // Spatial hash cell size in units

        private List<LintIssue> _issues = new List<LintIssue>();
        private Vector2 _scrollPosition;
        private int _totalScannedObjects = 0;
        private int _totalRenderers = 0;
        private int _totalColliders = 0;
        private bool _hasScanned = false;

        [MenuItem("ECHO/Geometry Linter", false, 150)]
        public static void OpenWindow()
        {
            EchoGeometryLinter window = GetWindow<EchoGeometryLinter>("Geometry Linter");
            window.minSize = new Vector2(450, 400);
            window.Show();
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawToolbar();
            DrawFilterBar();
            DrawSummary();
            DrawIssuesList();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(5);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("ECHO Geometry Linter (W-03)", EditorStyles.boldLabel);
            }
            EditorGUILayout.HelpBox(
                "Read-only geometry analyzer. Detects duplicate objects, overlapping walls, collider anomalies, and potential z-fighting. " +
                "Does NOT modify or delete any scene objects.",
                MessageType.None);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _scanAllLoadedScenes = EditorGUILayout.ToggleLeft("Scan All Loaded Scenes", _scanAllLoadedScenes, GUILayout.Width(170));
                    GUILayout.FlexibleSpace();

                    GUI.backgroundColor = new Color(0.35f, 0.75f, 1.0f);
                    if (GUILayout.Button("Scan Geometry", GUILayout.Height(24), GUILayout.Width(130)))
                    {
                        RunScan();
                    }
                    GUI.backgroundColor = Color.white;

                    if (GUILayout.Button("Clear", GUILayout.Height(24), GUILayout.Width(60)))
                    {
                        _issues.Clear();
                        _hasScanned = false;
                        _totalScannedObjects = 0;
                        _totalRenderers = 0;
                        _totalColliders = 0;
                    }
                }
            }
        }

        private void DrawFilterBar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Filter:", GUILayout.Width(40));
                _filterErrors = GUILayout.Toggle(_filterErrors, $"Errors ({CountSeverity(IssueSeverity.Error)})", EditorStyles.toolbarButton);
                _filterWarnings = GUILayout.Toggle(_filterWarnings, $"Warnings ({CountSeverity(IssueSeverity.Warning)})", EditorStyles.toolbarButton);
                _filterInfo = GUILayout.Toggle(_filterInfo, $"Info ({CountSeverity(IssueSeverity.Information)})", EditorStyles.toolbarButton);

                GUILayout.Space(10);
                GUILayout.Label("Search:", GUILayout.Width(48));
                _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
                if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
                {
                    _searchFilter = "";
                    GUI.FocusControl(null);
                }
            }
        }

        private void DrawSummary()
        {
            if (!_hasScanned)
            {
                EditorGUILayout.HelpBox("Click 'Scan Geometry' to scan the current scene.", MessageType.Info);
                return;
            }

            int errorCount = CountSeverity(IssueSeverity.Error);
            int warnCount = CountSeverity(IssueSeverity.Warning);
            int infoCount = CountSeverity(IssueSeverity.Information);

            string summary = $"Scanned: {_totalScannedObjects} GameObjects, {_totalRenderers} MeshRenderers, {_totalColliders} Colliders. " +
                             $"Found: {errorCount} Errors, {warnCount} Warnings, {infoCount} Info issues.";

            MessageType boxType = errorCount > 0 ? MessageType.Error : (warnCount > 0 ? MessageType.Warning : MessageType.Info);
            EditorGUILayout.HelpBox(summary, boxType);
        }

        private void DrawIssuesList()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            if (_hasScanned && _issues.Count == 0)
            {
                EditorGUILayout.LabelField("No geometry issues detected! Scene looks clean.", EditorStyles.wordWrappedLabel);
            }
            else
            {
                int displayedCount = 0;
                for (int i = 0; i < _issues.Count; i++)
                {
                    LintIssue issue = _issues[i];

                    // Filters
                    if (issue.Severity == IssueSeverity.Error && !_filterErrors) continue;
                    if (issue.Severity == IssueSeverity.Warning && !_filterWarnings) continue;
                    if (issue.Severity == IssueSeverity.Information && !_filterInfo) continue;

                    if (!string.IsNullOrEmpty(_searchFilter))
                    {
                        bool matchTitle = issue.Title.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                        bool matchDesc = issue.Description.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                        bool matchPrimary = issue.PrimaryPath != null && issue.PrimaryPath.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                        bool matchSecondary = issue.SecondaryPath != null && issue.SecondaryPath.IndexOf(_searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!matchTitle && !matchDesc && !matchPrimary && !matchSecondary)
                            continue;
                    }

                    displayedCount++;
                    DrawIssueItem(issue, i);
                }

                if (_hasScanned && displayedCount == 0 && _issues.Count > 0)
                {
                    EditorGUILayout.LabelField("No issues match the active filter criteria.", EditorStyles.centeredGreyMiniLabel);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawIssueItem(LintIssue issue, int index)
        {
            Color originalBg = GUI.backgroundColor;
            switch (issue.Severity)
            {
                case IssueSeverity.Error:
                    GUI.backgroundColor = new Color(1.0f, 0.7f, 0.7f);
                    break;
                case IssueSeverity.Warning:
                    GUI.backgroundColor = new Color(1.0f, 0.95f, 0.65f);
                    break;
                case IssueSeverity.Information:
                    GUI.backgroundColor = new Color(0.8f, 0.9f, 1.0f);
                    break;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUI.backgroundColor = originalBg;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUIContent icon = GetSeverityIcon(issue.Severity);
                    GUILayout.Label(icon, GUILayout.Width(20), GUILayout.Height(20));

                    GUILayout.Label($"[{issue.Category}] {issue.Title}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(45)))
                    {
                        if (issue.PrimaryObject != null)
                        {
                            EditorGUIUtility.PingObject(issue.PrimaryObject);
                            Selection.activeGameObject = issue.PrimaryObject;
                        }
                    }

                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        SelectIssueObjects(issue);
                    }

                    if (GUILayout.Button("Frame", EditorStyles.miniButton, GUILayout.Width(50)))
                    {
                        SelectAndFrame(issue);
                    }
                }

                EditorGUILayout.LabelField(issue.Description, EditorStyles.wordWrappedLabel);

                EditorGUILayout.Space(2);
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Object A:");
                    if (issue.PrimaryObject != null)
                    {
                        if (GUILayout.Button(issue.PrimaryPath, EditorStyles.linkLabel))
                        {
                            Selection.activeGameObject = issue.PrimaryObject;
                            EditorGUIUtility.PingObject(issue.PrimaryObject);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField(issue.PrimaryPath + " (Missing/Deleted)");
                    }
                }

                if (!string.IsNullOrEmpty(issue.SecondaryPath))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel("Object B:");
                        if (issue.SecondaryObject != null)
                        {
                            if (GUILayout.Button(issue.SecondaryPath, EditorStyles.linkLabel))
                            {
                                Selection.activeGameObject = issue.SecondaryObject;
                                EditorGUIUtility.PingObject(issue.SecondaryObject);
                            }
                        }
                        else
                        {
                            EditorGUILayout.LabelField(issue.SecondaryPath + " (Missing/Deleted)");
                        }
                    }
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Location: ({issue.Location.x:F2}, {issue.Location.y:F2}, {issue.Location.z:F2})", EditorStyles.miniLabel);
                }
            }
            EditorGUILayout.Space(2);
        }

        private void SelectIssueObjects(LintIssue issue)
        {
            List<GameObject> selection = new List<GameObject>();
            if (issue.PrimaryObject != null) selection.Add(issue.PrimaryObject);
            if (issue.SecondaryObject != null) selection.Add(issue.SecondaryObject);

            if (selection.Count > 0)
            {
                Selection.objects = selection.ToArray();
                EditorGUIUtility.PingObject(selection[0]);
            }
        }

        private void SelectAndFrame(LintIssue issue)
        {
            SelectIssueObjects(issue);
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.FrameSelected();
            }
        }

        private GUIContent GetSeverityIcon(IssueSeverity severity)
        {
            switch (severity)
            {
                case IssueSeverity.Error:
                    return EditorGUIUtility.IconContent("console.erroricon.sml");
                case IssueSeverity.Warning:
                    return EditorGUIUtility.IconContent("console.warnicon.sml");
                default:
                    return EditorGUIUtility.IconContent("console.infoicon.sml");
            }
        }

        private int CountSeverity(IssueSeverity severity)
        {
            int count = 0;
            for (int i = 0; i < _issues.Count; i++)
            {
                if (_issues[i].Severity == severity) count++;
            }
            return count;
        }

        // =========================================================================
        // SCANNING LOGIC
        // =========================================================================
        public void RunScan()
        {
            _issues.Clear();
            _hasScanned = true;

            List<GameObject> rootObjects = new List<GameObject>();

            if (_scanAllLoadedScenes)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded)
                    {
                        rootObjects.AddRange(scene.GetRootGameObjects());
                    }
                }
            }
            else
            {
                Scene activeScene = SceneManager.GetActiveScene();
                if (activeScene.isLoaded)
                {
                    rootObjects.AddRange(activeScene.GetRootGameObjects());
                }
            }

            List<GameObject> allObjects = new List<GameObject>();
            for (int i = 0; i < rootObjects.Count; i++)
            {
                CollectAllChildren(rootObjects[i].transform, allObjects);
            }

            _totalScannedObjects = allObjects.Count;

            // Collect renderers & colliders
            List<MeshFilter> meshFilters = new List<MeshFilter>();
            List<Collider> colliders = new List<Collider>();

            for (int i = 0; i < allObjects.Count; i++)
            {
                GameObject go = allObjects[i];
                MeshFilter mf = go.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && go.GetComponent<MeshRenderer>() != null)
                {
                    meshFilters.Add(mf);
                }

                Collider[] cols = go.GetComponents<Collider>();
                for (int c = 0; c < cols.Length; c++)
                {
                    if (cols[c] != null) colliders.Add(cols[c]);
                }
            }

            _totalRenderers = meshFilters.Count;
            _totalColliders = colliders.Count;

            try
            {
                EditorUtility.DisplayProgressBar("Geometry Linter", "Analyzing Colliders...", 0.2f);
                LintColliders(allObjects, colliders);

                EditorUtility.DisplayProgressBar("Geometry Linter", "Detecting Duplicate Objects...", 0.4f);
                LintDuplicates(allObjects);

                EditorUtility.DisplayProgressBar("Geometry Linter", "Analyzing Walls & Bounds...", 0.7f);
                LintWallBounds(meshFilters);

                EditorUtility.DisplayProgressBar("Geometry Linter", "Analyzing Z-Fighting...", 0.9f);
                LintZFighting(meshFilters);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Repaint();
        }

        private static void CollectAllChildren(Transform t, List<GameObject> results)
        {
            results.Add(t.gameObject);
            for (int i = 0; i < t.childCount; i++)
            {
                CollectAllChildren(t.GetChild(i), results);
            }
        }

        private static string GetHierarchyPath(GameObject obj)
        {
            if (obj == null) return "None";
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }

        // -------------------------------------------------------------------------
        // A. DUPLICATE GAMEOBJECTS
        // -------------------------------------------------------------------------
        private void LintDuplicates(List<GameObject> objects)
        {
            // Spatial hash by cell size
            Dictionary<Vector3Int, List<GameObject>> spatialHash = new Dictionary<Vector3Int, List<GameObject>>();
            for (int i = 0; i < objects.Count; i++)
            {
                GameObject go = objects[i];
                Vector3 pos = go.transform.position;
                Vector3Int cell = new Vector3Int(
                    Mathf.FloorToInt(pos.x / _gridCellSize),
                    Mathf.FloorToInt(pos.y / _gridCellSize),
                    Mathf.FloorToInt(pos.z / _gridCellSize));

                if (!spatialHash.TryGetValue(cell, out List<GameObject> list))
                {
                    list = new List<GameObject>();
                    spatialHash[cell] = list;
                }
                list.Add(go);
            }

            HashSet<EntityPair> reportedPairs = new HashSet<EntityPair>();

            foreach (var kvp in spatialHash)
            {
                List<GameObject> cellObjects = kvp.Value;
                int count = cellObjects.Count;

                for (int i = 0; i < count; i++)
                {
                    GameObject a = cellObjects[i];
                    if (a == null) continue;

                    for (int j = i + 1; j < count; j++)
                    {
                        GameObject b = cellObjects[j];
                        if (b == null || a == b) continue;

                        EntityPair pairKey = new EntityPair(a.GetEntityId(), b.GetEntityId());

                        if (reportedPairs.Contains(pairKey)) continue;

                        float posDist = Vector3.Distance(a.transform.position, b.transform.position);
                        if (posDist <= _positionCoincidenceTolerance)
                        {
                            float rotAngle = Quaternion.Angle(a.transform.rotation, b.transform.rotation);
                            float scaleDist = Vector3.Distance(a.transform.lossyScale, b.transform.lossyScale);

                            MeshFilter mfA = a.GetComponent<MeshFilter>();
                            MeshFilter mfB = b.GetComponent<MeshFilter>();

                            bool sameMesh = (mfA != null && mfB != null && mfA.sharedMesh != null && mfA.sharedMesh == mfB.sharedMesh);

                            if (sameMesh && rotAngle < 0.1f && scaleDist < 0.01f)
                            {
                                reportedPairs.Add(pairKey);
                                _issues.Add(new LintIssue
                                {
                                    Severity = IssueSeverity.Error,
                                    Category = IssueCategory.DuplicateGameObject,
                                    Title = $"Exact Duplicate GameObject: '{a.name}' and '{b.name}'",
                                    Description = $"Two GameObjects share identical mesh ({mfA.sharedMesh.name}), position, rotation, and scale at the exact same coordinates. This is almost certainly an accidental duplicate causing severe rendering and physics overhead.",
                                    PrimaryObject = a,
                                    SecondaryObject = b,
                                    PrimaryPath = GetHierarchyPath(a),
                                    SecondaryPath = GetHierarchyPath(b),
                                    Location = a.transform.position
                                });
                            }
                            else if (rotAngle < 0.1f && scaleDist < 0.01f && a.name.Equals(b.name, StringComparison.OrdinalIgnoreCase))
                            {
                                reportedPairs.Add(pairKey);
                                _issues.Add(new LintIssue
                                {
                                    Severity = IssueSeverity.Warning,
                                    Category = IssueCategory.DuplicateGameObject,
                                    Title = $"Duplicate Transform / Object: '{a.name}'",
                                    Description = $"Two GameObjects with identical names occupy identical transform coordinates ({a.transform.position}). Check if one is a duplicate.",
                                    PrimaryObject = a,
                                    SecondaryObject = b,
                                    PrimaryPath = GetHierarchyPath(a),
                                    SecondaryPath = GetHierarchyPath(b),
                                    Location = a.transform.position
                                });
                            }
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------------
        // B. OVERLAPPING WALLS
        // -------------------------------------------------------------------------
        private void LintWallBounds(List<MeshFilter> meshFilters)
        {
            // Focus on objects likely representing walls or structural geometry
            List<MeshFilter> walls = new List<MeshFilter>();
            for (int i = 0; i < meshFilters.Count; i++)
            {
                MeshFilter mf = meshFilters[i];
                string name = mf.gameObject.name.ToLowerInvariant();
                string path = GetHierarchyPath(mf.gameObject).ToLowerInvariant();
                if (name.Contains("wall") || path.Contains("wall") || name.Contains("corridor") || name.Contains("doorway") || name.Contains("cube"))
                {
                    walls.Add(mf);
                }
            }

            // Spatial hash for walls
            Dictionary<Vector3Int, List<MeshFilter>> wallGrid = new Dictionary<Vector3Int, List<MeshFilter>>();
            for (int i = 0; i < walls.Count; i++)
            {
                MeshFilter mf = walls[i];
                Renderer ren = mf.GetComponent<Renderer>();
                if (ren == null) continue;

                Bounds b = ren.bounds;
                Vector3Int minCell = new Vector3Int(
                    Mathf.FloorToInt(b.min.x / _gridCellSize),
                    Mathf.FloorToInt(b.min.y / _gridCellSize),
                    Mathf.FloorToInt(b.min.z / _gridCellSize));
                Vector3Int maxCell = new Vector3Int(
                    Mathf.FloorToInt(b.max.x / _gridCellSize),
                    Mathf.FloorToInt(b.max.y / _gridCellSize),
                    Mathf.FloorToInt(b.max.z / _gridCellSize));

                for (int x = minCell.x; x <= maxCell.x; x++)
                {
                    for (int y = minCell.y; y <= maxCell.y; y++)
                    {
                        for (int z = minCell.z; z <= maxCell.z; z++)
                        {
                            Vector3Int c = new Vector3Int(x, y, z);
                            if (!wallGrid.TryGetValue(c, out List<MeshFilter> list))
                            {
                                list = new List<MeshFilter>();
                                wallGrid[c] = list;
                            }
                            list.Add(mf);
                        }
                    }
                }
            }

            HashSet<EntityPair> processedPairs = new HashSet<EntityPair>();

            foreach (var kvp in wallGrid)
            {
                List<MeshFilter> list = kvp.Value;
                int count = list.Count;

                for (int i = 0; i < count; i++)
                {
                    MeshFilter mfA = list[i];
                    Renderer renA = mfA.GetComponent<Renderer>();
                    if (renA == null) continue;

                    for (int j = i + 1; j < count; j++)
                    {
                        MeshFilter mfB = list[j];
                        if (mfA == mfB) continue;

                        EntityPair pairKey = new EntityPair(mfA.GetEntityId(), mfB.GetEntityId());

                        if (processedPairs.Contains(pairKey)) continue;
                        processedPairs.Add(pairKey);

                        Renderer renB = mfB.GetComponent<Renderer>();
                        if (renB == null) continue;

                        Bounds bA = renA.bounds;
                        Bounds bB = renB.bounds;

                        if (bA.Intersects(bB))
                        {
                            // Calculate intersection box
                            Vector3 min = Vector3.Max(bA.min, bB.min);
                            Vector3 max = Vector3.Min(bA.max, bB.max);
                            Vector3 size = max - min;

                            if (size.x > 0.05f && size.y > 0.05f && size.z > 0.05f)
                            {
                                float overlapVolume = size.x * size.y * size.z;
                                float volA = bA.size.x * bA.size.y * bA.size.z;
                                float volB = bB.size.x * bB.size.y * bB.size.z;
                                float minVol = Mathf.Min(volA, volB);

                                if (minVol > 0.001f)
                                {
                                    float overlapRatio = overlapVolume / minVol;
                                    // If walls overlap more than 60% of their volume, flag as heavy overlap
                                    if (overlapRatio > 0.6f && overlapVolume > _wallOverlapMinVolume)
                                    {
                                        _issues.Add(new LintIssue
                                        {
                                            Severity = IssueSeverity.Warning,
                                            Category = IssueCategory.OverlappingWalls,
                                            Title = $"Suspicious Overlapping Wall Modules: '{mfA.gameObject.name}' & '{mfB.gameObject.name}'",
                                            Description = $"Wall segments share {overlapRatio * 100f:F1}% bounding volume ({overlapVolume:F2} m³ overlap). This usually indicates duplicate or misplaced wall modules clipped together.",
                                            PrimaryObject = mfA.gameObject,
                                            SecondaryObject = mfB.gameObject,
                                            PrimaryPath = GetHierarchyPath(mfA.gameObject),
                                            SecondaryPath = GetHierarchyPath(mfB.gameObject),
                                            Location = (min + max) * 0.5f
                                        });
                                    }
                                    else if (overlapRatio > 0.25f && overlapVolume > _wallOverlapMinVolume * 2f)
                                    {
                                        _issues.Add(new LintIssue
                                        {
                                            Severity = IssueSeverity.Information,
                                            Category = IssueCategory.OverlappingWalls,
                                            Title = $"Intersecting Wall Segments: '{mfA.gameObject.name}' & '{mfB.gameObject.name}'",
                                            Description = $"Wall geometry intersects by {overlapVolume:F2} m³ ({overlapRatio * 100f:F1}%). Verify if this is an intentional corner junction or an accidental double wall.",
                                            PrimaryObject = mfA.gameObject,
                                            SecondaryObject = mfB.gameObject,
                                            PrimaryPath = GetHierarchyPath(mfA.gameObject),
                                            SecondaryPath = GetHierarchyPath(mfB.gameObject),
                                            Location = (min + max) * 0.5f
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // -------------------------------------------------------------------------
        // C. COLLIDER PROBLEMS
        // -------------------------------------------------------------------------
        private void LintColliders(List<GameObject> allObjects, List<Collider> colliders)
        {
            // 1. Duplicate colliders on the same GameObject
            for (int i = 0; i < allObjects.Count; i++)
            {
                GameObject go = allObjects[i];
                Collider[] cols = go.GetComponents<Collider>();
                if (cols.Length > 1)
                {
                    // Check for duplicate types and identical bounds
                    for (int a = 0; a < cols.Length; a++)
                    {
                        for (int b = a + 1; b < cols.Length; b++)
                        {
                            Collider colA = cols[a];
                            Collider colB = cols[b];

                            if (colA.GetType() == colB.GetType() && colA.isTrigger == colB.isTrigger)
                            {
                                float dist = Vector3.Distance(colA.bounds.center, colB.bounds.center);
                                float sizeDiff = Vector3.Distance(colA.bounds.size, colB.bounds.size);

                                if (dist < 0.01f && sizeDiff < 0.01f)
                                {
                                    _issues.Add(new LintIssue
                                    {
                                        Severity = IssueSeverity.Error,
                                        Category = IssueCategory.ColliderProblem,
                                        Title = $"Duplicate Collider on same GameObject: '{go.name}'",
                                        Description = $"GameObject has two identical {colA.GetType().Name} components with matching center and size (isTrigger={colA.isTrigger}). One is redundant.",
                                        PrimaryObject = go,
                                        SecondaryObject = null,
                                        PrimaryPath = GetHierarchyPath(go),
                                        SecondaryPath = null,
                                        Location = go.transform.position
                                    });
                                }
                            }
                        }
                    }
                }
            }

            // 2. Spatial collider overlaps and geometry fit
            Dictionary<Vector3Int, List<Collider>> colGrid = new Dictionary<Vector3Int, List<Collider>>();
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider c = colliders[i];
                Bounds b = c.bounds;
                Vector3Int minCell = new Vector3Int(
                    Mathf.FloorToInt(b.min.x / _gridCellSize),
                    Mathf.FloorToInt(b.min.y / _gridCellSize),
                    Mathf.FloorToInt(b.min.z / _gridCellSize));
                Vector3Int maxCell = new Vector3Int(
                    Mathf.FloorToInt(b.max.x / _gridCellSize),
                    Mathf.FloorToInt(b.max.y / _gridCellSize),
                    Mathf.FloorToInt(b.max.z / _gridCellSize));

                for (int x = minCell.x; x <= maxCell.x; x++)
                {
                    for (int y = minCell.y; y <= maxCell.y; y++)
                    {
                        for (int z = minCell.z; z <= maxCell.z; z++)
                        {
                            Vector3Int cell = new Vector3Int(x, y, z);
                            if (!colGrid.TryGetValue(cell, out List<Collider> list))
                            {
                                list = new List<Collider>();
                                colGrid[cell] = list;
                            }
                            list.Add(c);
                        }
                    }
                }
            }

            HashSet<EntityPair> checkedPairs = new HashSet<EntityPair>();

            foreach (var kvp in colGrid)
            {
                List<Collider> list = kvp.Value;
                int count = list.Count;

                for (int i = 0; i < count; i++)
                {
                    Collider colA = list[i];
                    if (colA == null) continue;

                    for (int j = i + 1; j < count; j++)
                    {
                        Collider colB = list[j];
                        if (colB == null || colA == colB) continue;
                        if (colA.gameObject == colB.gameObject) continue;

                        EntityPair pairKey = new EntityPair(colA.GetEntityId(), colB.GetEntityId());

                        if (checkedPairs.Contains(pairKey)) continue;
                        checkedPairs.Add(pairKey);

                        Bounds bA = colA.bounds;
                        Bounds bB = colB.bounds;

                        if (bA.Intersects(bB))
                        {
                            // If non-trigger solid colliders have nearly identical bounds across different objects
                            float centerDist = Vector3.Distance(bA.center, bB.center);
                            float sizeDist = Vector3.Distance(bA.size, bB.size);

                            if (centerDist < 0.05f && sizeDist < 0.05f)
                            {
                                if (!colA.isTrigger && !colB.isTrigger)
                                {
                                    _issues.Add(new LintIssue
                                    {
                                        Severity = IssueSeverity.Error,
                                        Category = IssueCategory.ColliderProblem,
                                        Title = $"Coincident Solid Colliders: '{colA.gameObject.name}' & '{colB.gameObject.name}'",
                                        Description = $"Two separate GameObjects possess identical solid physical colliders occupying the exact same space. Can cause severe PhysX jitter or ghost collision.",
                                        PrimaryObject = colA.gameObject,
                                        SecondaryObject = colB.gameObject,
                                        PrimaryPath = GetHierarchyPath(colA.gameObject),
                                        SecondaryPath = GetHierarchyPath(colB.gameObject),
                                        Location = bA.center
                                    });
                                }
                            }
                            else if (colA.isTrigger && colB.isTrigger && centerDist < 0.05f && sizeDist < 0.05f)
                            {
                                _issues.Add(new LintIssue
                                {
                                    Severity = IssueSeverity.Warning,
                                    Category = IssueCategory.ColliderProblem,
                                    Title = $"Duplicate Trigger Volume: '{colA.gameObject.name}' & '{colB.gameObject.name}'",
                                    Description = $"Two trigger colliders occupy identical bounds. This will trigger OnTriggerEnter/Exit twice for player/monster.",
                                    PrimaryObject = colA.gameObject,
                                    SecondaryObject = colB.gameObject,
                                    PrimaryPath = GetHierarchyPath(colA.gameObject),
                                    SecondaryPath = GetHierarchyPath(colB.gameObject),
                                    Location = bA.center
                                });
                            }
                        }
                    }
                }
            }

            // 3. Check for Colliders that heavily exceed their mesh geometry or Triggers without purpose
            for (int i = 0; i < colliders.Count; i++)
            {
                Collider col = colliders[i];
                if (col == null) continue;

                Renderer ren = col.GetComponent<Renderer>();
                if (ren != null)
                {
                    Bounds colB = col.bounds;
                    Bounds renB = ren.bounds;

                    float colVol = colB.size.x * colB.size.y * colB.size.z;
                    float renVol = renB.size.x * renB.size.y * renB.size.z;

                    if (renVol > 0.01f && colVol > renVol * 3.5f && colVol > 1.0f && !col.isTrigger)
                    {
                        _issues.Add(new LintIssue
                        {
                            Severity = IssueSeverity.Warning,
                            Category = IssueCategory.ColliderProblem,
                            Title = $"Oversized Collider on '{col.gameObject.name}'",
                            Description = $"Collider volume ({colVol:F2} m³) is more than 3.5x larger than its visual mesh bounds ({renVol:F2} m³). Player may hit invisible collision.",
                            PrimaryObject = col.gameObject,
                            SecondaryObject = null,
                            PrimaryPath = GetHierarchyPath(col.gameObject),
                            SecondaryPath = null,
                            Location = colB.center
                        });
                    }
                }

                // Check triggers that have no scripts attached (potentially dead / misplaced trigger)
                if (col.isTrigger)
                {
                    MonoBehaviour[] scripts = col.GetComponents<MonoBehaviour>();
                    if (scripts.Length == 0 && col.transform.parent != null)
                    {
                        scripts = col.transform.parent.GetComponents<MonoBehaviour>();
                    }

                    if (scripts.Length == 0)
                    {
                        _issues.Add(new LintIssue
                        {
                            Severity = IssueSeverity.Information,
                            Category = IssueCategory.ColliderProblem,
                            Title = $"Trigger Collider without Script: '{col.gameObject.name}'",
                            Description = $"Collider is marked 'isTrigger = true' but has no MonoBehaviour script on itself or parent to handle OnTriggerEnter/Stay.",
                            PrimaryObject = col.gameObject,
                            SecondaryObject = null,
                            PrimaryPath = GetHierarchyPath(col.gameObject),
                            SecondaryPath = null,
                            Location = col.bounds.center
                        });
                    }
                }
            }
        }

        // -------------------------------------------------------------------------
        // D. POTENTIAL Z-FIGHTING
        // -------------------------------------------------------------------------
        private void LintZFighting(List<MeshFilter> meshFilters)
        {
            Dictionary<Vector3Int, List<MeshFilter>> meshGrid = new Dictionary<Vector3Int, List<MeshFilter>>();
            for (int i = 0; i < meshFilters.Count; i++)
            {
                MeshFilter mf = meshFilters[i];
                Renderer ren = mf.GetComponent<Renderer>();
                if (ren == null) continue;

                Bounds b = ren.bounds;
                Vector3Int minCell = new Vector3Int(
                    Mathf.FloorToInt(b.min.x / _gridCellSize),
                    Mathf.FloorToInt(b.min.y / _gridCellSize),
                    Mathf.FloorToInt(b.min.z / _gridCellSize));
                Vector3Int maxCell = new Vector3Int(
                    Mathf.FloorToInt(b.max.x / _gridCellSize),
                    Mathf.FloorToInt(b.max.y / _gridCellSize),
                    Mathf.FloorToInt(b.max.z / _gridCellSize));

                for (int x = minCell.x; x <= maxCell.x; x++)
                {
                    for (int y = minCell.y; y <= maxCell.y; y++)
                    {
                        for (int z = minCell.z; z <= maxCell.z; z++)
                        {
                            Vector3Int c = new Vector3Int(x, y, z);
                            if (!meshGrid.TryGetValue(c, out List<MeshFilter> list))
                            {
                                list = new List<MeshFilter>();
                                meshGrid[c] = list;
                            }
                            list.Add(mf);
                        }
                    }
                }
            }

            HashSet<EntityPair> testedPairs = new HashSet<EntityPair>();

            foreach (var kvp in meshGrid)
            {
                List<MeshFilter> list = kvp.Value;
                int count = list.Count;

                for (int i = 0; i < count; i++)
                {
                    MeshFilter mfA = list[i];
                    Renderer renA = mfA.GetComponent<Renderer>();
                    if (renA == null) continue;

                    for (int j = i + 1; j < count; j++)
                    {
                        MeshFilter mfB = list[j];
                        if (mfA == mfB) continue;

                        EntityPair pairKey = new EntityPair(mfA.GetEntityId(), mfB.GetEntityId());

                        if (testedPairs.Contains(pairKey)) continue;
                        testedPairs.Add(pairKey);

                        Renderer renB = mfB.GetComponent<Renderer>();
                        if (renB == null) continue;

                        Bounds bA = renA.bounds;
                        Bounds bB = renB.bounds;

                        if (bA.Intersects(bB))
                        {
                            // Check for near-coplanar face alignment (Z-fighting)
                            // 1. Min/Max planes along X, Y, or Z that match within _zFightPlaneDistanceThreshold
                            CheckCoplanarBoundFaces(mfA, mfB, bA, bB);
                        }
                    }
                }
            }
        }

        private void CheckCoplanarBoundFaces(MeshFilter mfA, MeshFilter mfB, Bounds bA, Bounds bB)
        {
            // Test X planes
            TestFacePair(mfA, mfB, bA.min.x, bB.min.x, bA, bB, Vector3.right, "X-min");
            TestFacePair(mfA, mfB, bA.max.x, bB.max.x, bA, bB, Vector3.right, "X-max");

            // Test Y planes (Floors / Ceilings)
            TestFacePair(mfA, mfB, bA.min.y, bB.min.y, bA, bB, Vector3.up, "Y-min (Floor/Ceiling)");
            TestFacePair(mfA, mfB, bA.max.y, bB.max.y, bA, bB, Vector3.up, "Y-max (Floor/Ceiling)");

            // Test Z planes
            TestFacePair(mfA, mfB, bA.min.z, bB.min.z, bA, bB, Vector3.forward, "Z-min");
            TestFacePair(mfA, mfB, bA.max.z, bB.max.z, bA, bB, Vector3.forward, "Z-max");
        }

        private void TestFacePair(MeshFilter mfA, MeshFilter mfB, float coordA, float coordB, Bounds bA, Bounds bB, Vector3 axis, string planeName)
        {
            float dist = Mathf.Abs(coordA - coordB);
            if (dist <= _zFightPlaneDistanceThreshold)
            {
                // Measure overlapping surface area perpendicular to axis
                float overlapArea = 0f;
                if (axis == Vector3.up)
                {
                    float minX = Mathf.Max(bA.min.x, bB.min.x);
                    float maxX = Mathf.Min(bA.max.x, bB.max.x);
                    float minZ = Mathf.Max(bA.min.z, bB.min.z);
                    float maxZ = Mathf.Min(bA.max.z, bB.max.z);
                    if (maxX > minX && maxZ > minZ) overlapArea = (maxX - minX) * (maxZ - minZ);
                }
                else if (axis == Vector3.right)
                {
                    float minY = Mathf.Max(bA.min.y, bB.min.y);
                    float maxY = Mathf.Min(bA.max.y, bB.max.y);
                    float minZ = Mathf.Max(bA.min.z, bB.min.z);
                    float maxZ = Mathf.Min(bA.max.z, bB.max.z);
                    if (maxY > minY && maxZ > minZ) overlapArea = (maxY - minY) * (maxZ - minZ);
                }
                else if (axis == Vector3.forward)
                {
                    float minX = Mathf.Max(bA.min.x, bB.min.x);
                    float maxX = Mathf.Min(bA.max.x, bB.max.x);
                    float minY = Mathf.Max(bA.min.y, bB.min.y);
                    float maxY = Mathf.Min(bA.max.y, bB.max.y);
                    if (maxX > minX && maxY > minY) overlapArea = (maxX - minX) * (maxY - minY);
                }

                if (overlapArea > 0.25f) // at least a quarter square meter of coplanar surface
                {
                    Vector3 loc = (Vector3.Max(bA.min, bB.min) + Vector3.Min(bA.max, bB.max)) * 0.5f;

                    // If they are exactly the same mesh, higher severity
                    bool sameMesh = mfA.sharedMesh != null && mfA.sharedMesh == mfB.sharedMesh;
                    IssueSeverity sev = sameMesh ? IssueSeverity.Warning : IssueSeverity.Information;

                    _issues.Add(new LintIssue
                    {
                        Severity = sev,
                        Category = IssueCategory.ZFighting,
                        Title = $"Potential Z-Fighting ({planeName}): '{mfA.gameObject.name}' & '{mfB.gameObject.name}'",
                        Description = $"Surfaces lie on virtually the same plane (offset {dist * 1000f:F1} mm) with {overlapArea:F2} m² of coincident planar overlap. This can produce visible depth-buffer flickering/z-fighting in-game.",
                        PrimaryObject = mfA.gameObject,
                        SecondaryObject = mfB.gameObject,
                        PrimaryPath = GetHierarchyPath(mfA.gameObject),
                        SecondaryPath = GetHierarchyPath(mfB.gameObject),
                        Location = loc
                    });
                }
            }
        }
    }
}
#endif
