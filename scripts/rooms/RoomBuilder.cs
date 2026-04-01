using Godot;
using Signal.Interaction;

namespace Signal.Rooms;

/// <summary>
/// Shared helper for building rooms programmatically.
/// All positions are relative to screen center (0,0).
/// Screen extents: (-640,-360) to (640,360) at 1280x720.
/// </summary>
public static class RoomBuilder
{
    public static ColorRect AddBackground(Node2D parent, Color color)
    {
        var bg = new ColorRect();
        bg.Color = color;
        bg.Position = new Vector2(-640, -360);
        bg.Size = new Vector2(1280, 720);
        bg.ZIndex = -10;
        bg.MouseFilter = Control.MouseFilterEnum.Ignore;
        parent.AddChild(bg);
        return bg;
    }

    public static Hotspot AddHotspot(Node2D parent, string name, Vector2 center, Vector2 size, HotspotData action, HotspotCondition condition = null)
    {
        var hotspot = new Hotspot();
        hotspot.Name = name;
        hotspot.Position = center;
        hotspot.Action = action;
        hotspot.Condition = condition;

        var shape = new CollisionShape2D();
        var rect = new RectangleShape2D();
        rect.Size = size;
        shape.Shape = rect;
        hotspot.AddChild(shape);

        parent.AddChild(hotspot);

        InteractionManager.Instance?.ConnectHotspot(hotspot);

        return hotspot;
    }

    public static Label AddLabel(Node2D parent, string text, Vector2 position)
    {
        var label = new Label();
        label.Text = text;
        label.HorizontalAlignment = HorizontalAlignment.Center;
        label.Position = position - new Vector2(80, 0);
        label.Size = new Vector2(160, 30);
        label.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.7f));
        label.AddThemeFontSizeOverride("font_size", 14);
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        parent.AddChild(label);
        return label;
    }
}
