using Godot;
using Signal.Core;

namespace Signal.Interaction;

public partial class CursorManager : Node
{
    public static CursorManager Instance { get; private set; }

    private Resource _defaultCursor;
    private Resource _interactCursor;

    public override void _Ready()
    {
        Instance = this;
        // Will use system cursors — custom cursor textures can be added later
        GameLog.ManagerReady("CursorManager");
    }

    public void SetDefault()
    {
        Input.SetDefaultCursorShape(Input.CursorShape.Arrow);
    }

    public void SetInteract()
    {
        Input.SetDefaultCursorShape(Input.CursorShape.PointingHand);
    }
}
