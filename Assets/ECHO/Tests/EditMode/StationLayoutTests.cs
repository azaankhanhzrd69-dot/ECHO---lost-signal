using NUnit.Framework;
using UnityEngine;
using Echo.World;
using System.Collections.Generic;

namespace Echo.Tests
{
    [TestFixture]
    public sealed class StationLayoutTests
    {
        private StationLayout _layout;
        private StationLayoutValidator.ValidationResult _validation;

        [SetUp]
        public void SetUp()
        {
            _layout = StationLayout.CreateDefault();
            _validation = StationLayoutValidator.Validate(_layout);
        }

        [Test]
        public void NoOverlappingRooms_Default()
        {
            for (int i = 0; i < _layout.Rooms.Count; i++)
            {
                for (int j = i + 1; j < _layout.Rooms.Count; j++)
                {
                    var r1 = _layout.Rooms[i];
                    var r2 = _layout.Rooms[j];
                    bool overlap = (r1.XMin < r2.XMax && r1.XMax > r2.XMin && r1.ZMin < r2.ZMax && r1.ZMax > r2.ZMin);
                    Assert.IsFalse(overlap, $"Rooms '{r1.Name}' and '{r2.Name}' overlap!");
                }
            }
        }

        [Test]
        public void NoBlockRoomOverlaps_Default()
        {
            foreach (var b in _layout.Blocks)
            {
                foreach (var r in _layout.Rooms)
                {
                    bool overlap = (b.XMin < r.XMax && b.XMax > r.XMin && b.ZMin < r.ZMax && b.ZMax > r.ZMin);
                    Assert.IsFalse(overlap, $"Block '{b.Name}' overlaps room '{r.Name}'!");
                }
            }
        }

        [Test]
        public void DoorsOnRoomWalls_Default()
        {
            foreach (var d in _layout.Doors)
            {
                bool onWall = false;
                foreach (var r in _layout.Rooms)
                {
                    if (Mathf.Abs(d.Center.z - r.ZMax) <= 0.35f && d.Center.x >= r.XMin - 0.2f && d.Center.x <= r.XMax + 0.2f)
                        onWall = true;
                    else if (Mathf.Abs(d.Center.z - r.ZMin) <= 0.35f && d.Center.x >= r.XMin - 0.2f && d.Center.x <= r.XMax + 0.2f)
                        onWall = true;
                    else if (Mathf.Abs(d.Center.x - r.XMax) <= 0.35f && d.Center.z >= r.ZMin - 0.2f && d.Center.z <= r.ZMax + 0.2f)
                        onWall = true;
                    else if (Mathf.Abs(d.Center.x - r.XMin) <= 0.35f && d.Center.z >= r.ZMin - 0.2f && d.Center.z <= r.ZMax + 0.2f)
                        onWall = true;

                    if (onWall) break;
                }
                Assert.IsTrue(onWall, $"Door '{d.Name}' is not on any room wall face!");
            }
        }

        [Test]
        public void LaneWidths_AllAtLeast2p5m_Default()
        {
            Assert.GreaterOrEqual(_validation.NarrowestLaneWidth, 2.5f, "Hallway narrowest lane width must be >= 2.5m");
        }

        [Test]
        public void AllAnchorsExist_Default()
        {
            string[] requiredAnchors = new[]
            {
                // Start
                "Anchor_Start_Player", "Anchor_Landing_ShipParked", "Anchor_Landing_ExteriorCamera",
                "Anchor_Vent", "Anchor_Keypad", "Anchor_ShuttleWreck_Inspect",
                // MedBay & hides
                "Anchor_Body", "Anchor_Lunge_MonsterEat", "Anchor_Lunge_DoorPoint", "Anchor_Lunge_PlayerFallback",
                "HideSpot_Intro_Locker", "HideSpot_Intro_Table", "HideSpot_Intro_Chair",
                "HideSpot_Closet_1", "HideSpot_Closet_2", "HideSpot_Closet_3",
                // Cores & reactor
                "Core_0", "Core_1", "Core_2",
                "ReactorSocket_0", "ReactorSocket_1", "ReactorSocket_2", "Anchor_ReactorPanel",
                // Radio & secret code
                "Radio_Console", "Anchor_Morse_Note", "Anchor_SecretCode_Camera",
                // Escape
                "PodReachedTrigger_Volume", "Anchor_Escape_PlayerStart", "Anchor_Escape_MonsterStart",
                "Pod_Hatch", "Anchor_Escape_SuckTarget",
                // Chase
                "Anchor_Chase_PlayerStart", "Anchor_Chase_MonsterStart", "Anchor_Chase_PodPoint",
                // Checkpoints
                "CP_HideIntro_Player", "CP_HideIntro_Monster",
                "CP_Sneak_Player", "CP_Sneak_Monster",
                "CP_ShuttleWreck_Player", "CP_ShuttleWreck_Monster",
                "CP_ReactorEmpty_Player", "CP_ReactorEmpty_Monster",
                "CP_CoreHunt_Player", "CP_CoreHunt_Monster",
                "CP_PowerOn_Player", "CP_PowerOn_Monster",
                "CP_Chase_Player", "CP_Chase_Monster"
            };

            var anchorNames = new HashSet<string>();
            foreach (var a in _layout.Anchors)
            {
                Assert.IsFalse(anchorNames.Contains(a.Name), $"Duplicate anchor found: '{a.Name}'");
                anchorNames.Add(a.Name);
            }

            foreach (var req in requiredAnchors)
            {
                Assert.IsTrue(anchorNames.Contains(req), $"Required anchor '{req}' is missing from StationLayout!");
            }
        }

        [Test]
        public void ReachabilityFromStart_Default()
        {
            Assert.IsEmpty(_validation.Errors, $"Validation encountered errors: {string.Join(", ", _validation.Errors)}");
        }

        [Test]
        public void RouteLength_WithSeals_CalculatedAndReported()
        {
            Assert.Greater(_validation.RouteLengthWithSeals, 0f, "Route with seals must be passable.");
            Assert.Greater(_validation.RouteLengthWithSeals, _validation.RouteLengthWithoutSeals, "Route with seals must be longer than route without seals.");
            Debug.Log($"[StationLayoutTests] Route WITHOUT seals: {_validation.RouteLengthWithoutSeals:F1}m, WITH seals: {_validation.RouteLengthWithSeals:F1}m");
        }

        [Test]
        public void FullValidator_Passes()
        {
            Assert.IsTrue(_validation.IsValid, _validation.Summary);
        }
    }
}

