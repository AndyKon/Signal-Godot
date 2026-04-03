using Godot;

namespace Signal.Tests;

/// <summary>
/// Add this to autoload. It checks for --autotest command line arg
/// and spawns the AutoPlaytest node if present.
/// </summary>
public partial class AutotestBootstrap : Node
{
    private bool _loadEvidence;
    private bool _evidenceLoaded;

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

            if (arg == "--evidence-test")
            {
                _loadEvidence = true;
                Core.GameLog.Event("Test", "Evidence test mode — data loads after scene ready");
                // Don't return — let other args process too
            }
        }
    }

    public override void _Process(double delta)
    {
        // Load evidence after a scene is active (NewGame resets state)
        if (_loadEvidence && !_evidenceLoaded && Evidence.EvidenceManager.Instance != null
            && Core.GameManager.Instance?.State != null
            && GetTree().CurrentScene?.Name != "MainMenu")
        {
            _evidenceLoaded = true;
            LoadEvidenceTestData();
        }
    }

    private void LoadEvidenceTestData()
    {
        var mgr = Evidence.EvidenceManager.Instance;
        if (mgr == null) return;

        // Section 1 — all
        mgr.Discover("seismic_report");
        mgr.Discover("exterior_view");
        mgr.Discover("nereus_boot_message");

        // Section 2 — all
        mgr.Discover("vasquez_fragments");
        mgr.Discover("vasquez_sedation");
        mgr.Discover("concussion_protocol");
        mgr.Discover("falsified_reports");
        mgr.Discover("supply_discrepancies");
        mgr.Discover("sudden_departure");

        // Section 3 — partial
        mgr.Discover("echo_origin");
        mgr.Discover("chen_final_messages");
        mgr.Discover("extraction_values");

        // Section 4 — partial
        mgr.Discover("okafor_dialogue");
        mgr.Discover("nereus_decision_log");
        mgr.Discover("chen_efficiency");
        mgr.Discover("nereus_false_warnings");
        mgr.Discover("preemptive_mods");

        // Section 5 — a few
        mgr.Discover("nereus_corporate");
        mgr.Discover("lock_sequence");
        mgr.Discover("deployment_truth");

        Core.GameLog.Event("Test", $"Pre-discovered 20 evidence entries — press J to open web");
    }
}
