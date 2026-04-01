using Godot;
using Signal.Interaction;

namespace Signal.Rooms;

public partial class HubRoom3 : Node2D
{
    public override void _Ready()
    {
        // Dark orange-brown
        RoomBuilder.AddBackground(this, new Color(0.12f, 0.08f, 0.06f));

        // Door back to Room 2 — left side
        RoomBuilder.AddHotspot(this, "DoorToRoom2",
            center: new Vector2(-400, 0),
            size: new Vector2(100, 160),
            action: new HotspotData { Type = HotspotType.Door, TargetScene = "Section1_Hub_Room2" });
        RoomBuilder.AddLabel(this, "< Door Back", new Vector2(-400, 90));

        // Power console — right-center
        RoomBuilder.AddHotspot(this, "PowerConsole",
            center: new Vector2(150, 0),
            size: new Vector2(160, 100),
            action: new HotspotData { Type = HotspotType.Narration, NarrativeEntryId = "hub_power_console" });
        RoomBuilder.AddLabel(this, "[ Power Console ]", new Vector2(150, 60));
    }
}
