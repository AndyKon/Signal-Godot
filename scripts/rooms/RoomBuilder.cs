using Godot;
using Signal.Interaction;

namespace Signal.Rooms;

/// <summary>
/// Shared helper for building rooms programmatically.
/// Godot 2D origin is top-left (0,0). Screen is 1280x720.
/// Center of screen is (640, 360).
/// </summary>
public static class RoomBuilder
{
    public const float ScreenW = 1280;
    public const float ScreenH = 720;
    public const float CenterX = ScreenW / 2;
    public const float CenterY = ScreenH / 2;

    public static ColorRect AddBackground(Node2D parent, Color color)
    {
        var bg = new ColorRect();
        bg.Color = color;
        bg.Position = new Vector2(0, 0);
        bg.Size = new Vector2(ScreenW, ScreenH);
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
