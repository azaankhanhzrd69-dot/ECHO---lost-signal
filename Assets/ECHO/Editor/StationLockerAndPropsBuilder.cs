#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

public static class StationLockerAndPropsBuilder
{
    private const string ConfigFolder = "Assets/ECHO/Resources";
    private const string ConfigPath = ConfigFolder + "/StationLockerAndProps.asset";
    private const string LockerPath =
        "Assets/Low-Poly 3D Lockers/Low-Poly 3D Lockers/Prefabs/M.C.C.prefab";
    private static readonly string[] PropPaths =
    {
        "Assets/Dzeruza/MinimalScifiPack/3DModels/Props/Prefabs/SM_Box1.prefab",
        "Assets/Dzeruza/MinimalScifiPack/3DModels/Props/Prefabs/SM_Canister1.prefab",
        "Assets/Dzeruza/MinimalScifiPack/3DModels/Props/Prefabs/SM_Trim_L_Container1.prefab",
        "Assets/Dzeruza/MinimalScifiPack/3DModels/Props/Prefabs/SM_Trim_L_Container2.prefab"
    };

    [MenuItem("ECHO/W-07/Configure Station Lockers and Props", priority = 230)]
    private static void ConfigureStationLockersAndProps()
    {
        GameObject locker = LoadPrefab(LockerPath);
        GameObject[] props = new GameObject[PropPaths.Length];
        for (int i = 0; i < PropPaths.Length; i++)
            props[i] = LoadPrefab(PropPaths[i]);

        if (!Directory.Exists(ConfigFolder))
            Directory.CreateDirectory(ConfigFolder);

        StationLockerAndPropsConfig config = AssetDatabase.LoadAssetAtPath<StationLockerAndPropsConfig>(ConfigPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<StationLockerAndPropsConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);
        }

        SerializedObject serializedConfig = new SerializedObject(config);
        serializedConfig.FindProperty("lockerPrefab").objectReferenceValue = locker;

        SerializedProperty propArray = serializedConfig.FindProperty("propPrefabs");
        propArray.arraySize = props.Length;
        for (int i = 0; i < props.Length; i++)
            propArray.GetArrayElementAtIndex(i).objectReferenceValue = props[i];

        StationLockerAndPropsConfig.LockerPlacement[] lockers =
        {
            Locker(new Vector3(-28f, 1f, 15f), 180f),
            Locker(new Vector3(-28f, 1f, -15f), 0f),
            Locker(new Vector3(-9f, 1f, 15f), 180f),
            Locker(new Vector3(-9f, 1f, -15f), 0f),
            Locker(new Vector3(9f, 1f, 14f), 180f),
            Locker(new Vector3(9f, 1f, -14f), 0f),
            Locker(new Vector3(22f, 1f, 8.5f), 90f),
            Locker(new Vector3(22f, 1f, -8.5f), 270f),
            Locker(new Vector3(38f, 1f, 15f), 180f),
            Locker(new Vector3(38f, 1f, -15f), 0f),
            Locker(new Vector3(57f, 1f, 15f), 180f),
            Locker(new Vector3(57f, 1f, -15f), 0f),
            Locker(new Vector3(75f, 1f, 15f), 180f),
            Locker(new Vector3(75f, 1f, -15f), 0f)
        };
        StationLockerAndPropsConfig.PropPlacement[] cornerProps =
        {
            Prop(0, new Vector3(-30f, 0.18f, 18f), 0f, 0.35f),
            Prop(1, new Vector3(-30f, 0.18f, -18f), 90f, 0.42f),
            Prop(2, new Vector3(-7f, 0.18f, 17f), 180f, 0.28f),
            Prop(1, new Vector3(-7f, 0.18f, -17f), 270f, 0.42f),
            Prop(0, new Vector3(10f, 0.18f, 16f), 90f, 0.35f),
            Prop(3, new Vector3(10f, 0.18f, -16f), 0f, 0.28f),
            Prop(2, new Vector3(24f, 0.18f, 8.5f), 180f, 0.28f),
            Prop(0, new Vector3(24f, 0.18f, -8.5f), 0f, 0.35f),
            Prop(1, new Vector3(37f, 0.18f, 18f), 0f, 0.42f),
            Prop(0, new Vector3(37f, 0.18f, -18f), 270f, 0.35f),
            Prop(3, new Vector3(56f, 0.18f, 18f), 90f, 0.28f),
            Prop(1, new Vector3(56f, 0.18f, -18f), 180f, 0.42f),
            Prop(0, new Vector3(77f, 0.18f, 18f), 180f, 0.35f),
            Prop(2, new Vector3(77f, 0.18f, -18f), 270f, 0.28f),
            Prop(1, new Vector3(-25f, 0.18f, 3f), 0f, 0.38f),
            Prop(0, new Vector3(-14f, 0.18f, -3f), 90f, 0.3f),
            Prop(2, new Vector3(2f, 0.18f, 3f), 180f, 0.25f),
            Prop(1, new Vector3(17f, 0.18f, -3f), 270f, 0.38f),
            Prop(0, new Vector3(40f, 0.18f, 5.2f), 0f, 0.3f),
            Prop(1, new Vector3(46f, 0.18f, -5.2f), 90f, 0.38f),
            Prop(2, new Vector3(56f, 0.18f, 5.2f), 180f, 0.25f),
            Prop(0, new Vector3(70f, 0.18f, -5.2f), 270f, 0.3f)
        };

        SetLockerPlacements(serializedConfig.FindProperty("lockers"), lockers);
        SetPropPlacements(serializedConfig.FindProperty("props"), cornerProps);
        serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[W-07] Station lockers and Minimal Scifi props are configured for runtime placement.");
    }

    private static GameObject LoadPrefab(string path)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new FileNotFoundException("Required station prefab was not found.", path);
        return prefab;
    }

    private static StationLockerAndPropsConfig.LockerPlacement Locker(Vector3 position, float yaw)
    {
        return new StationLockerAndPropsConfig.LockerPlacement { position = position, yaw = yaw };
    }

    private static StationLockerAndPropsConfig.PropPlacement Prop(int prefabIndex, Vector3 position, float yaw, float scale)
    {
        return new StationLockerAndPropsConfig.PropPlacement
        {
            prefabIndex = prefabIndex,
            position = position,
            yaw = yaw,
            scale = Vector3.one * scale
        };
    }

    private static void SetLockerPlacements(
        SerializedProperty property,
        StationLockerAndPropsConfig.LockerPlacement[] placements)
    {
        property.arraySize = placements.Length;
        for (int i = 0; i < placements.Length; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("position").vector3Value = placements[i].position;
            element.FindPropertyRelative("yaw").floatValue = placements[i].yaw;
        }
    }

    private static void SetPropPlacements(
        SerializedProperty property,
        StationLockerAndPropsConfig.PropPlacement[] placements)
    {
        property.arraySize = placements.Length;
        for (int i = 0; i < placements.Length; i++)
        {
            SerializedProperty element = property.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("prefabIndex").intValue = placements[i].prefabIndex;
            element.FindPropertyRelative("position").vector3Value = placements[i].position;
            element.FindPropertyRelative("yaw").floatValue = placements[i].yaw;
            element.FindPropertyRelative("scale").vector3Value = placements[i].scale;
        }
    }
}
#endif
