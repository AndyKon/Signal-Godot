using Godot;
using Signal.Core;

namespace Signal.Interaction;

public partial class Hotspot : Area2D
{
    [Export] public HotspotCondition Condition { get; set; }
    [Export] public HotspotData Action { get; set; }
    [Export] public HotspotData AltAction { get; set; }
    [Export] public string AltConditionFlag { get; set; } = "";

    [Signal] public delegate void ClickedEventHandler(Hotspot hotspot);

    private ColorRect _highlight;

    public override void _Ready()
    {
        InputEvent += OnInput;

        // Create highlight visual
        _highlight = new ColorRect();
        _highlight.Color = new Color(1, 1, 1, 0.15f);
        _highlight.Size = GetHighlightSize();
        _highlight.Position = -_highlight.Size / 2;
        _highlight.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_highlight);
    }

    private Vector2 GetHighlightSize()
    {
        var collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (collisionShape?.Shape is RectangleShape2D rect)
            return rect.Size;
        return new Vector2(64, 64);
    }

    public bool IsAvailable()
    {
        if (GameManager.Instance == null) return true;
        var state = GameManager.Instance.State;

        if (!string.IsNullOrEmpty(Condition?.RequiredFlag) && !state.HasFlag(Condition.RequiredFlag))
            return false;
        if (!string.IsNullOrEmpty(Condition?.RequiredItem) && !state.HasItem(Condition.RequiredItem))
            return false;
        if (!string.IsNullOrEmpty(Condition?.BlockedByFlag) && state.HasFlag(Condition.BlockedByFlag))
            return false;

        return true;
    }

    public HotspotData GetAction()
    {
        if (!string.IsNullOrEmpty(AltConditionFlag) && GameManager.Instance?.State.HasFlag(AltConditionFlag) == true)
            return AltAction ?? Action;
        return Action;
    }

    public void SetHighlight(bool on)
    {
        if (_highlight != null)
            _highlight.Visible = on;
    }

    private void OnInput(Node viewport, InputEvent @event, long shapeIdx)
    {
        if (@event is InputEventMouseButton mouseButton &&
            mouseButton.Pressed &&
            mouseButton.ButtonIndex == MouseButton.Left &&
            IsAvailable())
        {
            EmitSignal(SignalName.Clicked, this);
        }
    }

    public override void _Process(double delta)
    {
        Visible = IsAvailable();
    }
}
