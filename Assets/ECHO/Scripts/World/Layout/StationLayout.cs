using System;
using System.Collections.Generic;
using UnityEngine;

namespace Echo.World
{
    /// <summary>
    /// Pure data representation of the Echo station deck layout.
    /// Drives both runtime generation (EchoGame.Build.cs) and Edit-Mode scene baking (StationBaker.cs).
    /// snap-to-1m layout defined in W-13 task specification.
    /// </summary>
    public sealed class StationLayout
    {
        [Serializable]
        public struct RoomDef
        {
            public string Name;
            public float XMin;
            public float XMax;
            public float ZMin;
            public float ZMax;
            public float CeilingHeight;

            public float Width => XMax - XMin;
            public float Depth => ZMax - ZMin;
            public Vector3 Center => new Vector3((XMin + XMax) * 0.5f, CeilingHeight * 0.5f, (ZMin + ZMax) * 0.5f);

            public RoomDef(string name, float xMin, float xMax, float zMin, float zMax, float ceilingHeight = 3.5f)
            {
                Name = name;
                XMin = xMin;
                XMax = xMax;
                ZMin = zMin;
                ZMax = zMax;
                CeilingHeight = ceilingHeight;
            }
        }

        [Serializable]
        public struct DoorDef
        {
            public string Name;
            public Vector3 Center;
            public string Wall; // "North", "South", "East", "West"
            public bool HasLeaf;
            public bool IsKeypadLocked;
            public string LeafName;
            public float Width;
            public float Height;

            public DoorDef(string name, Vector3 center, string wall, bool hasLeaf = false, bool isKeypadLocked = false, string leafName = "", float width = 2.4f, float height = 3.0f)
            {
                Name = name;
                Center = center;
                Wall = wall;
                HasLeaf = hasLeaf;
                IsKeypadLocked = isKeypadLocked;
                LeafName = leafName;
                Width = width;
                Height = height;
            }
        }

        [Serializable]
        public struct BlockDef
        {
            public string Name;
            public float XMin;
            public float XMax;
            public float ZMin;
            public float ZMax;
            public float Height;

            public Vector3 Center => new Vector3((XMin + XMax) * 0.5f, Height * 0.5f, (ZMin + ZMax) * 0.5f);
            public Vector3 Size => new Vector3(XMax - XMin, Height, ZMax - ZMin);

            public BlockDef(string name, float xMin, float xMax, float zMin, float zMax, float height = 3.5f)
            {
                Name = name;
                XMin = xMin;
                XMax = xMax;
                ZMin = zMin;
                ZMax = zMax;
                Height = height;
            }
        }

        [Serializable]
        public struct PartitionDef
        {
            public string Name;
            public char Axis; // 'X' (constant X, runs along Z) or 'Z' (constant Z, runs along X)
            public float FixedCoord;
            public float Min;
            public float Max;
            public float Thickness;
            public float Height;

            public Vector3 Center
            {
                get
                {
                    if (Axis == 'Z')
                        return new Vector3((Min + Max) * 0.5f, Height * 0.5f, FixedCoord);
                    else
                        return new Vector3(FixedCoord, Height * 0.5f, (Min + Max) * 0.5f);
                }
            }

            public Vector3 Size
            {
                get
                {
                    if (Axis == 'Z')
                        return new Vector3(Max - Min, Height, Thickness);
                    else
                        return new Vector3(Thickness, Height, Max - Min);
                }
            }

            public PartitionDef(string name, char axis, float fixedCoord, float min, float max, float thickness = 0.3f, float height = 3.5f)
            {
                Name = name;
                Axis = char.ToUpperInvariant(axis);
                FixedCoord = fixedCoord;
                Min = min;
                Max = max;
                Thickness = thickness;
                Height = height;
            }
        }

        [Serializable]
        public struct ChaseSealDef
        {
            public string Name;
            public Vector3 Position;
            public float Width;
            public char AcrossAxis; // 'X' or 'Z'

            public ChaseSealDef(string name, Vector3 position, float width, char acrossAxis = 'X')
            {
                Name = name;
                Position = position;
                Width = width;
                AcrossAxis = acrossAxis;
            }
        }

        [Serializable]
        public struct AnchorDef
        {
            public string Name;
            public string Group;
            public Vector3 Position;
            public float Yaw;

            public AnchorDef(string name, string group, Vector3 position, float yaw = 0f)
            {
                Name = name;
                Group = group;
                Position = position;
                Yaw = yaw;
            }
        }

        [Serializable]
        public struct LightDef
        {
            public string Name;
            public Vector3 Position;
            public Color Color;
            public float Range;
            public float Intensity;
            public bool IsEmergency;

            public LightDef(string name, Vector3 position, Color color, float range, float intensity, bool isEmergency = false)
            {
                Name = name;
                Position = position;
                Color = color;
                Range = range;
                Intensity = intensity;
                IsEmergency = isEmergency;
            }
        }

        [Serializable]
        public struct SignDef
        {
            public string Text;
            public Vector3 Position;
            public float Yaw;

            public SignDef(string text, Vector3 position, float yaw)
            {
                Text = text;
                Position = position;
                Yaw = yaw;
            }
        }

        // ── Deck Dimensions ───────────────────────────────────────────────────────────
        public float MainDeckXMin { get; private set; } = -24f;
        public float MainDeckXMax { get; private set; } = 24f;
        public float MainDeckZMin { get; private set; } = -24f;
        public float MainDeckZMax { get; private set; } = 24f;

        public float DockingBayXMin { get; private set; } = -4f;
        public float DockingBayXMax { get; private set; } = 8f;
        public float DockingBayZMin { get; private set; } = -36f;
        public float DockingBayZMax { get; private set; } = -24f;

        public float WallThickness { get; private set; } = 0.3f;
        public float StandardCeilingHeight { get; private set; } = 3.5f;

        // ── Collections ───────────────────────────────────────────────────────────────
        public List<RoomDef> Rooms { get; } = new List<RoomDef>();
        public List<DoorDef> Doors { get; } = new List<DoorDef>();
        public List<BlockDef> Blocks { get; } = new List<BlockDef>();
        public List<PartitionDef> Partitions { get; } = new List<PartitionDef>();
        public List<ChaseSealDef> ChaseSeals { get; } = new List<ChaseSealDef>();
        public List<AnchorDef> Anchors { get; } = new List<AnchorDef>();
        public List<LightDef> Lights { get; } = new List<LightDef>();
        public List<SignDef> Signs { get; } = new List<SignDef>();

        // ── Default Singleton ─────────────────────────────────────────────────────────
        private static StationLayout _defaultInstance;
        public static StationLayout Default
        {
            get
            {
                if (_defaultInstance == null)
                    _defaultInstance = CreateDefault();
                return _defaultInstance;
            }
        }

        public static StationLayout CreateDefault()
        {
            var layout = new StationLayout();

            // 1. Rooms (inside faces)
            layout.Rooms.Add(new RoomDef("Sleeping Quarters", -8f, 10f, 14f, 24f, 3.5f));
            layout.Rooms.Add(new RoomDef("Washroom", 14f, 24f, 14f, 24f, 3.5f));
            layout.Rooms.Add(new RoomDef("Escape Pod", -24f, -12f, 10f, 18f, 3.5f));
            layout.Rooms.Add(new RoomDef("Reactor", 14f, 24f, 2f, 10f, 5.0f));
            layout.Rooms.Add(new RoomDef("Cockpit", -24f, -12f, -10f, 4f, 3.5f));
            layout.Rooms.Add(new RoomDef("MedBay", 12f, 24f, -18f, -8f, 3.5f));
            layout.Rooms.Add(new RoomDef("Radio", -24f, -14f, -24f, -18f, 3.5f));
            layout.Rooms.Add(new RoomDef("Docking Bay", -4f, 8f, -36f, -24f, 4.5f));

            // 2. Doors (center, wall)
            layout.Doors.Add(new DoorDef("Door_Entry", new Vector3(2f, 0f, -24f), "North", hasLeaf: true, isKeypadLocked: true, leafName: "Door_Entry_Leaf"));
            layout.Doors.Add(new DoorDef("Sleeping D1", new Vector3(1f, 0f, 14f), "South", hasLeaf: false));
            layout.Doors.Add(new DoorDef("Sleeping D2", new Vector3(-5f, 0f, 14f), "South", hasLeaf: false));
            layout.Doors.Add(new DoorDef("Washroom", new Vector3(14f, 0f, 19f), "West", hasLeaf: false));
            layout.Doors.Add(new DoorDef("Escape Pod", new Vector3(-12f, 0f, 14f), "East", hasLeaf: true, leafName: "Door_Pod_Leaf"));
            layout.Doors.Add(new DoorDef("Reactor D1", new Vector3(14f, 0f, 6f), "West", hasLeaf: false));
            layout.Doors.Add(new DoorDef("Reactor D2", new Vector3(19f, 0f, 2f), "South", hasLeaf: false));
            layout.Doors.Add(new DoorDef("Cockpit D1", new Vector3(-12f, 0f, -3f), "East", hasLeaf: false));
            layout.Doors.Add(new DoorDef("Cockpit D2", new Vector3(-18f, 0f, 4f), "North", hasLeaf: false));
            layout.Doors.Add(new DoorDef("MedBay", new Vector3(12f, 0f, -13f), "West", hasLeaf: true, leafName: "Door_MedBay_Leaf"));
            layout.Doors.Add(new DoorDef("Radio", new Vector3(-14f, 0f, -21f), "East", hasLeaf: false));

            // 3. Hallway obstacles (solid full-height blocks)
            layout.Blocks.Add(new BlockDef("Block C", -6f, 6f, -4f, 6f, 3.5f));
            layout.Blocks.Add(new BlockDef("Block S1", -10f, -4f, -16f, -10f, 3.5f));
            layout.Blocks.Add(new BlockDef("Block S2", 2f, 8f, -16f, -10f, 3.5f));

            // 4. Thin partitions (0.3m wall)
            // Note: P1 is configured from X=-8 to 0 to maintain hallway connectivity to the Escape Pod door during Chase.
            layout.Partitions.Add(new PartitionDef("P1", 'Z', 10f, -8f, 0f, 0.3f, 3.5f));
            layout.Partitions.Add(new PartitionDef("P2", 'X', 9f, -6f, 4f, 0.3f, 3.5f));
            layout.Partitions.Add(new PartitionDef("P3", 'X', -9f, -8f, 2f, 0.3f, 3.5f));
            layout.Partitions.Add(new PartitionDef("P4", 'Z', -14f, -24f, -16f, 0.3f, 3.5f));

            // 5. Chase seals (open by default, closed during Beat.Chase)
            layout.ChaseSeals.Add(new ChaseSealDef("ChaseSeal_NorthHall_A", new Vector3(-4f, 0f, 8f), 4f, 'X'));
            layout.ChaseSeals.Add(new ChaseSealDef("ChaseSeal_NorthHall_B", new Vector3(-4f, 0f, 12f), 4f, 'X'));
            layout.ChaseSeals.Add(new ChaseSealDef("ChaseSeal_Sleeping2", new Vector3(-5f, 0f, 14f), 2.4f, 'X'));

            // 6. Anchors
            // Start
            layout.Anchors.Add(new AnchorDef("Anchor_Start_Player", "Start", new Vector3(2f, 0f, -26f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Landing_ShipParked", "Start", new Vector3(2f, 0f, -30.5f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Landing_ExteriorCamera", "Start", new Vector3(2f, 6f, -56f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Vent", "Start", new Vector3(-3.7f, 0.5f, -30f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Keypad", "Start", new Vector3(3.6f, 1.3f, -24.2f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_ShuttleWreck_Inspect", "Start", new Vector3(2f, 1.2f, -33.5f), 0f));

            // MedBay and hides
            layout.Anchors.Add(new AnchorDef("Anchor_Body", "MedBay", new Vector3(20.5f, 0f, -13f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Lunge_MonsterEat", "MedBay", new Vector3(20.5f, 0f, -12f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Lunge_DoorPoint", "MedBay", new Vector3(13.2f, 0f, -13f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Lunge_PlayerFallback", "MedBay", new Vector3(9.6f, 0f, -13f), 90f));
            layout.Anchors.Add(new AnchorDef("HideSpot_Intro_Locker", "MedBay", new Vector3(9.6f, 0f, -9.3f), 0f));
            layout.Anchors.Add(new AnchorDef("HideSpot_Intro_Table", "MedBay", new Vector3(9.6f, 0f, -15.5f), 0f));
            layout.Anchors.Add(new AnchorDef("HideSpot_Intro_Chair", "MedBay", new Vector3(8.8f, 0f, -12f), 0f));
            layout.Anchors.Add(new AnchorDef("HideSpot_Closet_1", "MedBay", new Vector3(-11.7f, 0f, -6f), 0f));
            layout.Anchors.Add(new AnchorDef("HideSpot_Closet_2", "MedBay", new Vector3(4f, 0f, 13.7f), 0f));
            layout.Anchors.Add(new AnchorDef("HideSpot_Closet_3", "MedBay", new Vector3(-7f, 0f, -16.3f), 0f));

            // Cores and reactor
            layout.Anchors.Add(new AnchorDef("Core_0", "Cores", new Vector3(-4f, 0.25f, 21f), 0f));
            layout.Anchors.Add(new AnchorDef("Core_1", "Cores", new Vector3(20.5f, 0.25f, 20.5f), 0f));
            layout.Anchors.Add(new AnchorDef("Core_2", "Cores", new Vector3(-21f, 0.25f, -7f), 0f));
            layout.Anchors.Add(new AnchorDef("ReactorSocket_0", "Cores", new Vector3(22.5f, 0.9f, 5f), 0f));
            layout.Anchors.Add(new AnchorDef("ReactorSocket_1", "Cores", new Vector3(22.5f, 0.9f, 6f), 0f));
            layout.Anchors.Add(new AnchorDef("ReactorSocket_2", "Cores", new Vector3(22.5f, 0.9f, 7f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_ReactorPanel", "Cores", new Vector3(22.5f, 1.2f, 4f), 0f));

            // Radio and secret code
            layout.Anchors.Add(new AnchorDef("Radio_Console", "Radio", new Vector3(-21f, 0f, -22.4f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Morse_Note", "Radio", new Vector3(-20f, 0.9f, -2f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_SecretCode_Camera", "Radio", new Vector3(-18.5f, 1.7f, -20.5f), 0f));

            // Escape
            layout.Anchors.Add(new AnchorDef("PodReachedTrigger_Volume", "Escape", new Vector3(-19f, 1.5f, 14f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Escape_PlayerStart", "Escape", new Vector3(-18f, 0f, 14f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Escape_MonsterStart", "Escape", new Vector3(-13f, 0f, 14f), 0f));
            layout.Anchors.Add(new AnchorDef("Pod_Hatch", "Escape", new Vector3(-24f, 0f, 14f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Escape_SuckTarget", "Escape", new Vector3(-30f, 1.5f, 14f), 0f));

            // Chase
            layout.Anchors.Add(new AnchorDef("Anchor_Chase_PlayerStart", "Chase", new Vector3(13f, 0f, 6f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Chase_MonsterStart", "Chase", new Vector3(9f, 0f, 10f), 0f));
            layout.Anchors.Add(new AnchorDef("Anchor_Chase_PodPoint", "Chase", new Vector3(-18f, 0f, 10f), 0f));

            // Checkpoints
            layout.Anchors.Add(new AnchorDef("CP_HideIntro_Player", "Checkpoints", new Vector3(9.6f, 0f, -13f), 90f));
            layout.Anchors.Add(new AnchorDef("CP_HideIntro_Monster", "Checkpoints", new Vector3(13.2f, 0f, -13f), 0f));
            layout.Anchors.Add(new AnchorDef("CP_Sneak_Player", "Checkpoints", new Vector3(9.6f, 0f, -11f), 0f));
            layout.Anchors.Add(new AnchorDef("CP_Sneak_Monster", "Checkpoints", new Vector3(13.2f, 0f, -13f), 0f));
            layout.Anchors.Add(new AnchorDef("CP_ShuttleWreck_Player", "Checkpoints", new Vector3(2f, 0f, -26f), 0f));
            layout.Anchors.Add(new AnchorDef("CP_ShuttleWreck_Monster", "Checkpoints", new Vector3(13.2f, 0f, -13f), 0f));
            layout.Anchors.Add(new AnchorDef("CP_ReactorEmpty_Player", "Checkpoints", new Vector3(13f, 0f, 6f), 0f));
            layout.Anchors.Add(new AnchorDef("CP_ReactorEmpty_Monster", "Checkpoints", new Vector3(13.2f, 0f, -13f), 0f));
            layout.Anchors.Add(new AnchorDef("CP_CoreHunt_Player", "Checkpoints", new Vector3(13f, 0f, 6f), 0f));
            layout.Anchors.Add(new AnchorDef("CP_CoreHunt_Monster", "Checkpoints", new Vector3(-2f, 0f, -14f), 0f));
            layout.Anchors.Add(new AnchorDef("CP_PowerOn_Player", "Checkpoints", new Vector3(18f, 0f, 6f), 0f));
            layout.Anchors.Add(new AnchorDef("CP_PowerOn_Monster", "Checkpoints", new Vector3(-2f, 0f, -14f), 0f));
            layout.Anchors.Add(new AnchorDef("CP_Chase_Player", "Checkpoints", new Vector3(13f, 0f, 6f), 0f));
            layout.Anchors.Add(new AnchorDef("CP_Chase_Monster", "Checkpoints", new Vector3(9f, 0f, 10f), 0f));

            // 7. Signs
            layout.Signs.Add(new SignDef("DOCKING BAY", new Vector3(2f, 2.65f, -24.3f), 180f));
            layout.Signs.Add(new SignDef("SLEEPING QUARTERS", new Vector3(1f, 2.65f, 13.7f), 0f));
            layout.Signs.Add(new SignDef("WASHROOM", new Vector3(13.7f, 2.65f, 19f), 90f));
            layout.Signs.Add(new SignDef("ESCAPE POD", new Vector3(-11.7f, 2.65f, 14f), 270f));
            layout.Signs.Add(new SignDef("REACTOR", new Vector3(13.7f, 2.65f, 6f), 90f));
            layout.Signs.Add(new SignDef("COCKPIT", new Vector3(-11.7f, 2.65f, -3f), 270f));
            layout.Signs.Add(new SignDef("MEDBAY", new Vector3(11.7f, 2.65f, -13f), 90f));
            layout.Signs.Add(new SignDef("RADIO", new Vector3(-13.7f, 2.65f, -21f), 270f));

            // 8. Lights (atmospheric moods)
            // Emergency red in MedBay
            layout.Lights.Add(new LightDef("EmergencyLight_MedBay_1", new Vector3(18f, 2.8f, -13f), new Color(0.9f, 0.15f, 0.1f), 12f, 1.2f, isEmergency: true));
            // Cold blue in Reactor
            layout.Lights.Add(new LightDef("Light_Reactor_1", new Vector3(19f, 4.0f, 6f), new Color(0.2f, 0.5f, 0.95f), 15f, 1.0f));
            // Dim hallway lights
            layout.Lights.Add(new LightDef("EmergencyLight_NorthHall_1", new Vector3(0f, 2.8f, 10f), new Color(0.8f, 0.6f, 0.3f), 8f, 0.5f, isEmergency: true));
            layout.Lights.Add(new LightDef("EmergencyLight_EastHall_1", new Vector3(10f, 2.8f, 0f), new Color(0.8f, 0.6f, 0.3f), 8f, 0.5f, isEmergency: true));
            layout.Lights.Add(new LightDef("EmergencyLight_SouthHall_1", new Vector3(0f, 2.8f, -20f), new Color(0.8f, 0.6f, 0.3f), 8f, 0.5f, isEmergency: true));
            layout.Lights.Add(new LightDef("EmergencyLight_WestHall_1", new Vector3(-10f, 2.8f, 0f), new Color(0.8f, 0.6f, 0.3f), 8f, 0.5f, isEmergency: true));
            // Docking bay
            layout.Lights.Add(new LightDef("Light_DockingBay_1", new Vector3(2f, 3.5f, -30f), new Color(0.4f, 0.6f, 0.8f), 14f, 0.8f));

            return layout;
        }
    }
}

