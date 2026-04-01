using Godot;
using Signal.Interaction;

namespace Signal.Rooms;

public partial class HubRoom3 : Node2D
{
    public override void _Ready()
    {
        BuildRoom();
    }

    private void BuildRoom()
    {
        var bg = new ColorRect();
        bg.Color = new Color(0.12f, 0.08f, 0.06f);
        bg.Position = new Vector2(-640, -360);
        bg.Size = new Vector2(1280, 720);
        AddChild(bg);

        // Door back to Room 2
        AddChild(CreateHotspot("DoorToRoom2", new Vector2(-384, 0), new Vector2(128, 192),
            new HotspotData { Type = HotspotType.Door, TargetScene = "Section1_Hub_Room2" }));

        // Power console
        AddChild(CreateHotspot("PowerConsole", new Vector2(192, 0), new Vector2(192, 128),
            new HotspotData { Type = HotspotType.Narration, NarrativeEntryId = "hub_power_console" }));

        foreach (var child in GetChildren())
        {
            if (child is Hotspot hotspot)
                InteractionManager.Instance?.ConnectHotspot(hotspot);
        }
    }

    private Hotspot CreateHotspot(string name, Vector2 position, Vector2 size, HotspotData action, HotspotCondition condition = null)
    {
        var hotspot = new Hotspot();
        hotspot.Name = name;
        hotspot.Position = position;
        hotspot.Action = action;
        hotspot.Condition = condition;

        var shape = new CollisionShape2D();
        var rect = new RectangleShape2D();
        rect.Size = size;
        shape.Shape = rect;
        hotspot.AddChild(shape);

        return hotspot;
    }
}
