using Godot;
using Signal.Interaction;

namespace Signal.Rooms;

public partial class HubRoom2 : Node2D
{
    public override void _Ready()
    {
        BuildRoom();
    }

    private void BuildRoom()
    {
        var bg = new ColorRect();
        bg.Color = new Color(0.1f, 0.12f, 0.2f);
        bg.Position = new Vector2(-640, -360);
        bg.Size = new Vector2(1280, 720);
        AddChild(bg);

        // Door back to Room 1
        AddChild(CreateHotspot("DoorToRoom1", new Vector2(-384, 0), new Vector2(128, 192),
            new HotspotData { Type = HotspotType.Door, TargetScene = "Section1_Hub_Room1" }));

        // Keycard pickup
        AddChild(CreateHotspot("KeycardPickup", new Vector2(0, 0), new Vector2(128, 96),
            new HotspotData
            {
                Type = HotspotType.PickUp,
                ExamineText = "A keycard. Might open the power room.",
                ItemToGrant = "keycard_hub",
                FlagToSet = "picked_up_hub_keycard"
            },
            new HotspotCondition { BlockedByFlag = "picked_up_hub_keycard" }));

        // Door to Room 3 (needs keycard)
        AddChild(CreateHotspot("DoorToRoom3", new Vector2(384, 0), new Vector2(128, 192),
            new HotspotData { Type = HotspotType.Door, TargetScene = "Section1_Hub_Room3" },
            new HotspotCondition { RequiredItem = "keycard_hub" }));

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
