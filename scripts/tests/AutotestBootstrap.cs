using Godot;

namespace Signal.Tests;

/// <summary>
/// Add this to autoload. It checks for --autotest command line arg
/// and spawns the AutoPlaytest node if present.
/// </summary>
public partial class AutotestBootstrap : Node
{
    public override void _Ready()
    {
        var args = OS.GetCmdlineUserArgs();
        foreach (var arg in args)
        {
            if (arg == "--autotest")
            {
                var testScene = GD.Load<PackedScene>("res://scenes/AutoPlaytest.tscn");
                var testNode = testScene.Instantiate();
                GetTree().Root.CallDeferred("add_child", testNode);
                Core.GameLog.Event("Test", "AutoPlaytest enabled via --autotest flag");
                return;
            }

            if (arg == "--decryption-scenarios")
            {
                var runner = new DecryptionScenarioRunner();
                GetTree().Root.CallDeferred("add_child", runner);
                Core.GameLog.Event("Test", "Decryption scenario runner enabled");
                return;
            }
        }
    }
}
