using Godot;
using Signal.Interaction;
using static Signal.Rooms.RoomBuilder;

namespace Signal.Rooms;

public partial class HubRoom1 : Node2D
{
    public override void _Ready()
    {
        // Dark blue background
        AddBackground(this, new Color(0.08f, 0.1f, 0.18f));

        // Intro terminal — center of screen
        AddHotspot(this, "IntroTerminal",
            center: new Vector2(CenterX, CenterY),
            size: new Vector2(160, 100),
            action: new HotspotData { Type = HotspotType.Narration, NarrativeEntryId = "hub_reboot_01" });
        AddLabel(this, "[ Intro Terminal ]", new Vector2(CenterX, CenterY + 70));

        // Optional terminal — left side
        AddHotspot(this, "OptionalTerminal",
            center: new Vector2(250, CenterY),
            size: new Vector2(120, 100),
            action: new HotspotData { Type = HotspotType.Narration, NarrativeEntryId = "hub_optional_terminal" });
        AddLabel(this, "Optional Terminal", new Vector2(250, CenterY + 70));

        // Door to Room 2 — right side
        AddHotspot(this, "DoorToRoom2",
            center: new Vector2(1100, CenterY),
            size: new Vector2(120, 180),
            action: new HotspotData { Type = HotspotType.Door, TargetScene = "Section1_Hub_Room2" });
        AddLabel(this, "Door >", new Vector2(1100, CenterY + 110));
    }
}
