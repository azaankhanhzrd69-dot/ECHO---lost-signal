using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class EchoKitImporter
{
    private const string SourceRoot = "Assets/Creepy_Cat/3D Scifi Kit Starter Kit_HD/Prefabs/";
    private const string SoundRoot = "Assets/Creepy_Cat/3D Scifi Kit Starter Kit_HD/Sounds/";
    private const string DestinationRoot = "Assets/ECHO/Resources/StationKit/";
    private const string SoundDestinationRoot = "Assets/ECHO/Resources/StationAudio/";
    private const string AnimationSourceRoot = "Assets/MonsterMutant 7/Animations/";
    private const string AnimationDestinationRoot = "Assets/ECHO/Resources/MonsterAnimations/";
    private const string ShipPrefab = "Assets/StarSparrow/Prefabs/Examples/StarSparrow1.prefab";

    private static readonly string[] Modules =
    {
        "Floors/Floor_Squared_01_6x6.prefab",
        "Doors/DoorWay_01_Large.prefab",
        "Walls/Porthole_01_Long.prefab",
        "Walls/Wall_Pipes_01_Large.prefab",
        "Walls/Wall_Console_01_Large.prefab",
        "Walls/Wall_Cube_01_Long.prefab",
        "Walls/Wall_Gear_01_Half.prefab",
        "Roofs/Roof_Squared_01_6x6.prefab",
        "Doors/Door_Vert_01.prefab",
        "Stuff/Intercom_01.prefab",
        "Stuff/Pipes_01.prefab",
        "Stuff/Air_Grid_01.prefab",
        "Stuff/Grid_01.prefab",
        "Lights/Light_Corridor_01.prefab",
        "Props/Crate_01.prefab"
    };

    private static readonly string[] Sounds =
    {
        "Ambiant_Loop.wav",
        "Computer_loop.wav",
        "Machin_Loop.wav",
        "Pneumatic-door.wav"
    };

    private static readonly string[] RuntimePrefabs =
    {
        "Assets/MonsterMutant 7/Prefab/Base mesh MonsterMutant7 skin1.prefab",
        "Assets/Energy/Type 1/EnergyCell_1/Prefabs/EnergyCell_1_1.prefab",
        "Assets/Energy/Type 2/EnergyCell_2/Prefabs/EnergyCell_2_1.prefab"
    };

    [MenuItem("ECHO/Import Modular Station Kit")]
    public static void ImportKit()
    {
        if (!AssetDatabase.IsValidFolder("Assets/ECHO/Resources"))
        {
            Directory.CreateDirectory("Assets/ECHO/Resources");
            AssetDatabase.Refresh();
        }

        if (!AssetDatabase.IsValidFolder(DestinationRoot.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/ECHO/Resources", "StationKit");

        CopyRuntimePrefab(RuntimePrefabs[0], DestinationRoot + "MonsterMutant7_Skin1.prefab");
        CopyRuntimePrefab(RuntimePrefabs[1], DestinationRoot + "EnergyCell_Type1.prefab");
        CopyRuntimePrefab(RuntimePrefabs[2], DestinationRoot + "EnergyCell_Type2.prefab");
        CopyRuntimePrefab(ShipPrefab, DestinationRoot + "StarSparrow1.prefab");

        if (!AssetDatabase.IsValidFolder(AnimationDestinationRoot.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/ECHO/Resources", "MonsterAnimations");

        CopyRuntimeAsset(AnimationSourceRoot + "MutantMonster2@idle1.fbx",
            AnimationDestinationRoot + "MutantMonster2@idle1.fbx");
        CopyRuntimeAsset(AnimationSourceRoot + "MutantMonster2@run1.fbx",
            AnimationDestinationRoot + "MutantMonster2@run1.fbx");

        foreach (string module in Modules)
        {
            string source = SourceRoot + module;
            string destination = DestinationRoot + Path.GetFileName(module);
            if (!File.Exists(source))
                throw new FileNotFoundException("The installed sci-fi prefab was not found.", source);

            if (File.Exists(destination))
                continue;

            if (!AssetDatabase.CopyAsset(source, destination))
                throw new IOException("Could not import the station prefab: " + source);
        }

        if (!AssetDatabase.IsValidFolder(SoundDestinationRoot.TrimEnd('/')))
            AssetDatabase.CreateFolder("Assets/ECHO/Resources", "StationAudio");

        foreach (string sound in Sounds)
        {
            string source = SoundRoot + sound;
            string destination = SoundDestinationRoot + sound;
            if (!File.Exists(source))
                throw new FileNotFoundException("The sci-fi kit sound was not found.", source);

            if (File.Exists(destination))
                continue;

            if (!AssetDatabase.CopyAsset(source, destination))
                throw new IOException("Coul d not import the station sound: " + source);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("ECHO: imported " + (Modules.Length + RuntimePrefabs.Length + 1) +
            " runtime prefabs and " + Sounds.Length + " station sounds.");
    }

    private static void CopyRuntimePrefab(string source, string destination)
    {
        if (!File.Exists(source))
            throw new FileNotFoundException("A required runtime prefab was not found.", source);

        if (File.Exists(destination))
            return;

        if (!AssetDatabase.CopyAsset(source, destination))
            throw new IOException("Could not import the runtime prefab: " + source);
    }

    private static void CopyRuntimeAsset(string source, string destination)
    {
        if (!File.Exists(source))
            throw new FileNotFoundException("A required runtime animation was not found.", source);

        if (File.Exists(destination))
            return;

        if (!AssetDatabase.CopyAsset(source, destination))
            throw new IOException("Could not import the runtime animation: " + source);
    }
}
