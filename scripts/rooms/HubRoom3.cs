using Godot;
using Signal.Interaction;
using static Signal.Rooms.RoomBuilder;

namespace Signal.Rooms;

public partial class HubRoom3 : Node2D
{
    public override void _Ready()
    {
        AddBackground(this, new Color(0.12f, 0.08f, 0.06f));

        // Door back to Room 2 — left side
        AddHotspot(this, "DoorToRoom2",
            center: new Vector2(180, CenterY),
            size: new Vector2(120, 180),
            action: new HotspotData { Type = HotspotType.Door, TargetScene = "Section1_Hub_Room2" });
        AddLabel(this, "< Door Back", new Vector2(180, CenterY + 110));

        // Power console — right-center
        AddHotspot(this, "PowerConsole",
            center: new Vector2(800, CenterY),
            size: new Vector2(160, 100),
            action: new HotspotData { Type = HotspotType.Narration, NarrativeEntryId = "hub_power_console" });
        AddLabel(this, "[ Power Console ]", new Vector2(800, CenterY + 70));
    }
}
