using Godot;
using Signal.Interaction;

namespace Signal.Interaction;

public partial class Room : Node2D
{
    public override void _Ready()
    {
        // Auto-connect all hotspots in the room to the InteractionManager
        foreach (var child in GetChildren())
        {
            if (child is Hotspot hotspot)
                InteractionManager.Instance?.ConnectHotspot(hotspot);
        }
    }

    public override void _ExitTree()
    {
        foreach (var child in GetChildren())
        {
            if (child is Hotspot hotspot)
                InteractionManager.Instance?.DisconnectHotspot(hotspot);
        }
    }
}
