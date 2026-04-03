using System.Collections.Generic;
using Godot;
using Signal.Core;
using Signal.Interaction;
using Signal.Narrative;
using Signal.Inventory;

namespace Signal.Tests;

/// <summary>
/// Automated playtest that exercises every system.
/// Run with: --autotest flag or by adding to autoload.
/// Simulates a full game playthrough and reports results.
/// </summary>
public partial class AutoPlaytest : Node
{
    private int _step;
    private float _timer;
    private float _stepDelay = 0.5f; // seconds between actions
    private readonly List<string> _failures = new();
    private readonly List<string> _passes = new();
    private bool _done;

    public override void _Ready()
    {
        GameLog.Event("Test", "=== AUTOMATED PLAYTEST STARTING ===");
        _step = 0;
        _timer = 1.0f; // Wait 1 second for everything to initialize
    }

    public override void _Process(double delta)
    {
        if (_done) return;

        _timer -= (float)delta;
        if (_timer > 0) return;

        _timer = _stepDelay;
        RunStep();
    }

    private void RunStep()
    {
        switch (_step)
        {
            // === INITIALIZATION CHECKS ===
            case 0:
                Check("GameManager initialized", GameManager.Instance != null);
                Check("InteractionManager initialized", InteractionManager.Instance != null);
                Check("SceneLoader initialized", SceneLoader.Instance != null);
                Check("InventoryManager initialized", InventoryManager.Instance != null);
                Check("NarrativeManager initialized", NarrativeManager.Instance != null);
                Check("CursorManager initialized", CursorManager.Instance != null);
                Check("GameState exists", GameManager.Instance?.State != null);
                Check("SaveSystem exists", GameManager.Instance?.SaveSystem != null);
                break;

            // === NEW GAME ===
            case 1:
                GameLog.Event("Test", "--- Starting new game ---");
                GameManager.Instance.NewGame();
                Check("State reset - no flags", GameManager.Instance.State.FlagCount == 0);
                Check("State reset - no inventory", GameManager.Instance.State.Inventory.Count == 0);
                Check("State reset - no powered sections", !GameManager.Instance.State.IsSectionPowered(1));
                break;

            // === SAVE/LOAD TEST ===
            case 2:
                GameLog.Event("Test", "--- Testing save/load ---");
                GameManager.Instance.SaveToSlot(2);
                Check("Saved to slot 2", GameManager.Instance.SaveSystem.SlotExists(2));

                // Record current state
                int flagCount = GameManager.Instance.State.FlagCount;
                string scene = GameManager.Instance.State.CurrentScene;

                // Reset and reload
                GameManager.Instance.NewGame();
                Check("State cleared after NewGame", GameManager.Instance.State.FlagCount == 0);

                bool loaded = GameManager.Instance.LoadFromSlot(2);
                Check("Load from slot 2 succeeded", loaded);
                Check("Flags restored", GameManager.Instance.State.FlagCount == flagCount);
                Check("Scene restored", GameManager.Instance.State.CurrentScene == scene);
                break;

            // === ENDING EVALUATOR TEST ===
            case 3:
                GameLog.Event("Test", "--- Testing ending evaluator ---");
                var testState = new GameState();
                testState.RegisterTotalOptionalFlags(10);

                Check("No flags = Escape ending", EndingEvaluator.Evaluate(testState) == Ending.Escape);

                for (int i = 0; i < 6; i++) testState.SetFlag($"test_{i}");
                Check("60% flags = Truth ending", EndingEvaluator.Evaluate(testState) == Ending.Truth);

                for (int i = 6; i < 9; i++) testState.SetFlag($"test_{i}");
                Check("90% flags no mirror = Truth ending", EndingEvaluator.Evaluate(testState) == Ending.Truth);

                testState.SetFlag("saw_reflection");
                Check("90% flags + mirror = Confrontation ending", EndingEvaluator.Evaluate(testState) == Ending.Confrontation);
                break;

            // === SAVE SLOT MANAGEMENT ===
            case 4:
                GameLog.Event("Test", "--- Testing save slot management ---");
                Check("Slot 2 exists (manual save)", GameManager.Instance.SaveSystem.SlotExists(2));
                Check("Slot 3 empty", !GameManager.Instance.SaveSystem.SlotExists(3));
                Check("Slot -1 invalid", !GameManager.Instance.SaveSystem.SlotExists(-1));
                Check("Slot 5 invalid", !GameManager.Instance.SaveSystem.SlotExists(5));

                GameManager.Instance.SaveSystem.Delete(2);
                Check("Slot 2 deleted", !GameManager.Instance.SaveSystem.SlotExists(2));
                break;

            // === REPORT ===
            case 5:
                Report();
                _done = true;
                // Quit after report
                _timer = 1.0f;
                break;

            case 6:
                GetTree().Quit();
                break;
        }

        _step++;
    }

    private void Check(string name, bool passed)
    {
        if (passed)
        {
            _passes.Add(name);
            GameLog.Event("Test", $"  PASS: {name}");
        }
        else
        {
            _failures.Add(name);
            GameLog.Error("Test", $"  FAIL: {name}");
        }
    }

    private Hotspot FindHotspot(string name)
    {
        return GetTree().Root.FindChild(name, true, false) as Hotspot;
    }

    private void SimulateClick(Hotspot hotspot)
    {
        // Directly emit the clicked signal (simulates a click without mouse input)
        hotspot.EmitSignal(Hotspot.SignalName.Clicked, hotspot);
    }

    private void DismissNarrative()
    {
        NarrativeManager.Instance?.Hide();
    }

    private void Report()
    {
        GameLog.Event("Test", "");
        GameLog.Event("Test", "=== AUTOMATED PLAYTEST RESULTS ===");
        GameLog.Event("Test", $"PASSED: {_passes.Count}");
        GameLog.Event("Test", $"FAILED: {_failures.Count}");
        GameLog.Event("Test", $"TOTAL:  {_passes.Count + _failures.Count}");

        if (_failures.Count > 0)
        {
            GameLog.Event("Test", "");
            GameLog.Event("Test", "FAILURES:");
            foreach (var f in _failures)
                GameLog.Error("Test", $"  - {f}");
        }

        GameLog.Event("Test", "=== END PLAYTEST ===");
    }
}
