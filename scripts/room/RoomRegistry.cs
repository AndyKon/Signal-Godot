using System.Collections.Generic;
using Godot;
using Signal.Interaction;

namespace Signal.Room;

public static class RoomRegistry
{
    public static readonly Dictionary<string, RoomDefinition> Rooms = new();

    static RoomRegistry()
    {
        Register(CreatePressureLockControl());
        Register(CreateEquipmentStorage());
        Register(CreatePowerJunction());
    }

    private static void Register(RoomDefinition room) => Rooms[room.Id] = room;

    public static RoomDefinition Get(string id) =>
        Rooms.TryGetValue(id, out var room) ? room : null;

    // ── Section 1: Pressure Lock Bay ──────────────────────────────────────

    private static RoomDefinition CreatePressureLockControl() => new()
    {
        Id = "section1_pressure_control",
        DisplayName = "Pressure Lock Control",
        Section = 1,
        // DEBUG: bright colors to verify layer rendering
        BgColor = new Color(0.15f, 0.2f, 0.4f),
        MidColor = new Color(0.2f, 0.3f, 0.15f),
        FgColor = new Color(0.3f, 0.15f, 0.15f),
        EntryNarrative = "Systems initializing. Platform damage detected. Restoration directive active.",
        DustCount = 6,
        HasEmergencyLights = true,
        HasViewport = true,
        Hotspots = new List<RoomHotspotDef>
        {
            new("main_terminal",
                new Vector2(1400, 650), new Vector2(180, 120), RoomLayer.Mid,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.Narration,
                    ExamineText = "NEREUS PLATFORM OS v4.2.1\nBoot sequence complete.\nECHO unit sensor suite: active. Diagnostic scan [Shift].\nCrew status: evacuated. Platform status: seismic damage detected.\nDirective: restore critical subsystems.",
                    FlagToSet = "read_boot_message",
                    EvidenceToDiscover = "nereus_boot_message"
                }),

            new("viewport",
                new Vector2(1440, 280), new Vector2(260, 260), RoomLayer.Bg,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Examine,
                    ExamineText = "Deep ocean. Bioluminescent organisms drift past the viewport. Hydrothermal vents glow in the distance. Peaceful.",
                    EvidenceToDiscover = "exterior_view"
                }),

            new("pressure_gauge",
                new Vector2(700, 500), new Vector2(100, 80), RoomLayer.Mid,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Examine,
                    ExamineText = "Pressure readings. Sections 2 through 4 show fluctuations at 02:14 UTC. The pattern is irregular — not consistent with a single seismic event."
                }),

            new("door_to_storage",
                new Vector2(2500, 600), new Vector2(120, 220), RoomLayer.Mid,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.Door,
                    TargetScene = "Section1_EquipmentStorage"
                }),
        }
    };

    private static RoomDefinition CreateEquipmentStorage() => new()
    {
        Id = "section1_equipment_storage",
        DisplayName = "Equipment Storage",
        Section = 1,
        BgColor = new Color(0.08f, 0.10f, 0.18f),
        MidColor = new Color(0.10f, 0.13f, 0.22f),
        FgColor = new Color(0.05f, 0.07f, 0.12f),
        EntryNarrative = "Storage bay. Dim lighting. Equipment racks line the walls.",
        DustCount = 4,
        HasEmergencyLights = true,
        HasViewport = false,
        Hotspots = new List<RoomHotspotDef>
        {
            new("locked_terminal",
                new Vector2(800, 550), new Vector2(160, 120), RoomLayer.Mid,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Terminal,
                    ExamineText = "Platform seismic sensors recorded a magnitude 4.2 event at 02:14 UTC. Origin point consistent with subsurface geological activity. No structural damage detected. Emergency systems responded within normal parameters.",
                    FlagToSet = "read_seismic_report",
                    EvidenceToDiscover = "seismic_report"
                },
                requiresPuzzle: true),

            new("equipment_locker",
                new Vector2(1600, 450), new Vector2(120, 140), RoomLayer.Mid,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Examine,
                    ExamineText = "Standard maintenance equipment. Wrenches, sealant, diagnostic cables. Everything in its place. Nothing unusual."
                }),

            new("keycard",
                new Vector2(1200, 900), new Vector2(80, 60), RoomLayer.Fg,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.PickUp,
                    ExamineText = "A keycard on the floor, half under the shelf. Power Junction access.",
                    ItemToGrant = "keycard_power_junction",
                    FlagToSet = "picked_up_power_keycard"
                }),

            new("emergency_kit",
                new Vector2(400, 800), new Vector2(100, 70), RoomLayer.Fg,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Examine,
                    ExamineText = "Emergency kit. Sealed, unopened. Nobody used it."
                }),

            new("door_to_control",
                new Vector2(200, 600), new Vector2(120, 220), RoomLayer.Mid,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.Door,
                    TargetScene = "Section1_PressureLockControl"
                }),

            new("door_to_power",
                new Vector2(2600, 600), new Vector2(120, 220), RoomLayer.Mid,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.Door,
                    TargetScene = "Section1_PowerJunction"
                },
                condition: new HotspotCondition { RequiredItem = "keycard_power_junction" }),
        }
    };

    private static RoomDefinition CreatePowerJunction() => new()
    {
        Id = "section1_power_junction",
        DisplayName = "Power Junction",
        Section = 1,
        BgColor = new Color(0.18f, 0.10f, 0.08f),
        MidColor = new Color(0.22f, 0.13f, 0.10f),
        FgColor = new Color(0.10f, 0.06f, 0.05f),
        EntryNarrative = "Warning lights active. This room runs hot. Exposed wiring on every surface.",
        DustCount = 3,
        HasEmergencyLights = true,
        HasViewport = false,
        Hotspots = new List<RoomHotspotDef>
        {
            new("power_console",
                new Vector2(1400, 550), new Vector2(200, 140), RoomLayer.Mid,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.Terminal,
                    ExamineText = "Hub power restored. Life support systems coming online. Section 2 pressure locks releasing.",
                    FlagToSet = "hub_power_restored"
                }),

            new("exposed_wiring",
                new Vector2(900, 350), new Vector2(140, 100), RoomLayer.Mid,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Examine,
                    ExamineText = "Repair work, interrupted. The cuts are clean — deliberate, not damage."
                }),

            new("warning_sign",
                new Vector2(1800, 180), new Vector2(160, 60), RoomLayer.Bg,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Examine,
                    ExamineText = "CAUTION: Pressure differential — seal doors before maintenance."
                }),

            new("door_to_storage",
                new Vector2(200, 600), new Vector2(120, 220), RoomLayer.Mid,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.Door,
                    TargetScene = "Section1_EquipmentStorage"
                }),
        }
    };
}
