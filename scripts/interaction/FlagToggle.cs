using Godot;
using Signal.Core;

namespace Signal.Interaction;

public partial class FlagToggle : Node2D
{
    public enum ToggleMode
    {
        ShowWhenSet,
        HideWhenSet,
        SwapWhenSet
    }

    [Export] public string Flag { get; set; } = "";
    [Export] public ToggleMode Mode { get; set; } = ToggleMode.ShowWhenSet;
    [Export] public NodePath AltObjectPath { get; set; }

    public override void _Ready()
    {
        Apply();
    }

    private void Apply()
    {
        bool flagSet = GameManager.Instance?.State.HasFlag(Flag) == true;

        switch (Mode)
        {
            case ToggleMode.ShowWhenSet:
                Visible = flagSet;
                break;
            case ToggleMode.HideWhenSet:
                Visible = !flagSet;
                break;
            case ToggleMode.SwapWhenSet:
                Visible = !flagSet;
                if (AltObjectPath != null)
                {
                    var alt = GetNodeOrNull<Node2D>(AltObjectPath);
                    if (alt != null) alt.Visible = flagSet;
                }
                break;
        }
    }
}
