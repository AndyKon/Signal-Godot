using Godot;
using Signal.Interaction;
using static Signal.Rooms.RoomBuilder;

namespace Signal.Rooms;

public partial class HubRoom2 : Node2D
{
    public override void _Ready()
    {
        AddBackground(this, new Color(0.1f, 0.12f, 0.2f));

        // Door back to Room 1 — left side
        AddHotspot(this, "DoorToRoom1",
            center: new Vector2(180, CenterY),
            size: new Vector2(120, 180),
            action: new HotspotData { Type = HotspotType.Door, TargetScene = "Section1_Hub_Room1" });
        AddLabel(this, "< Door Back", new Vector2(180, CenterY + 110));

        // Keycard pickup — center
        AddHotspot(this, "KeycardPickup",
            center: new Vector2(CenterX, CenterY + 40),
            size: new Vector2(120, 80),
            action: new HotspotData
            {
                Type = HotspotType.PickUp,
                ExamineText = "A keycard. Might open the power room.",
                ItemToGrant = "keycard_hub",
                FlagToSet = "picked_up_hub_keycard"
            },
            condition: new HotspotCondition { BlockedByFlag = "picked_up_hub_keycard" });
        AddLabel(this, "[ Keycard ]", new Vector2(CenterX, CenterY + 100));

        // Door to Room 3 — right side (needs keycard)
        AddHotspot(this, "DoorToRoom3",
            center: new Vector2(1100, CenterY),
            size: new Vector2(120, 180),
            action: new HotspotData { Type = HotspotType.Door, TargetScene = "Section1_Hub_Room3" },
            condition: new HotspotCondition { RequiredItem = "keycard_hub" });
        AddLabel(this, "Door (Locked) >", new Vector2(1100, CenterY + 110));
    }
}
