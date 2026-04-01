using Godot;
using Signal.Interaction;

namespace Signal.Rooms;

public partial class HubRoom1 : Node2D
{
    public override void _Ready()
    {
        // Dark blue background
        RoomBuilder.AddBackground(this, new Color(0.08f, 0.1f, 0.18f));

        // Intro terminal — center of screen
        RoomBuilder.AddHotspot(this, "IntroTerminal",
            center: new Vector2(0, 0),
            size: new Vector2(160, 100),
            action: new HotspotData { Type = HotspotType.Narration, NarrativeEntryId = "hub_reboot_01" });
        RoomBuilder.AddLabel(this, "[ Intro Terminal ]", new Vector2(0, 60));

        // Optional terminal — left side
        RoomBuilder.AddHotspot(this, "OptionalTerminal",
            center: new Vector2(-350, 0),
            size: new Vector2(120, 100),
            action: new HotspotData { Type = HotspotType.Narration, NarrativeEntryId = "hub_optional_terminal" });
        RoomBuilder.AddLabel(this, "Optional Terminal", new Vector2(-350, 60));

        // Door to Room 2 — right side
        RoomBuilder.AddHotspot(this, "DoorToRoom2",
            center: new Vector2(400, 0),
            size: new Vector2(100, 160),
            action: new HotspotData { Type = HotspotType.Door, TargetScene = "Section1_Hub_Room2" });
        RoomBuilder.AddLabel(this, "Door >", new Vector2(400, 90));
    }
}
