using Godot;

namespace Signal.Room;

public partial class ParallaxRoomLoader : ParallaxRoom
{
    [Export] public string RoomId { get; set; } = "";

    public override void _Ready()
    {
        var roomDef = RoomRegistry.Get(RoomId);
        if (roomDef == null)
        {
            Core.GameLog.Error("Room", $"RoomId not found in registry: {RoomId}");
            return;
        }
        Initialize(roomDef);
        base._Ready();
    }
}
