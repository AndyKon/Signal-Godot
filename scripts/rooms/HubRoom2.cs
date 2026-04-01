using Godot;
using Signal.Interaction;

namespace Signal.Rooms;

public partial class HubRoom2 : Node2D
{
    public override void _Ready()
    {
        // Slightly lighter blue
        RoomBuilder.AddBackground(this, new Color(0.1f, 0.12f, 0.2f));

        // Door back to Room 1 — left side
        RoomBuilder.AddHotspot(this, "DoorToRoom1",
            center: new Vector2(-400, 0),
            size: new Vector2(100, 160),
            action: new HotspotData { Type = HotspotType.Door, TargetScene = "Section1_Hub_Room1" });
        RoomBuilder.AddLabel(this, "< Door Back", new Vector2(-400, 90));

        // Keycard pickup — center
        RoomBuilder.AddHotspot(this, "KeycardPickup",
            center: new Vector2(0, 50),
            size: new Vector2(120, 80),
            action: new HotspotData
            {
                Type = HotspotType.PickUp,
                ExamineText = "A keycard. Might open the power room.",
                ItemToGrant = "keycard_hub",
                FlagToSet = "picked_up_hub_keycard"
            },
            condition: new HotspotCondition { BlockedByFlag = "picked_up_hub_keycard" });
        RoomBuilder.AddLabel(this, "[ Keycard ]", new Vector2(0, 100));

        // Door to Room 3 — right side (needs keycard)
        RoomBuilder.AddHotspot(this, "DoorToRoom3",
            center: new Vector2(400, 0),
            size: new Vector2(100, 160),
            action: new HotspotData { Type = HotspotType.Door, TargetScene = "Section1_Hub_Room3" },
            condition: new HotspotCondition { RequiredItem = "keycard_hub" });
        RoomBuilder.AddLabel(this, "Door (Locked) >", new Vector2(400, 90));
    }
}
