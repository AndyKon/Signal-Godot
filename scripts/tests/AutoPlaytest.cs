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
                SceneLoader.Instance.LoadScene("Section1_Hub_Room1", isNewSection: true);
                _timer = 1.0f; // Wait for scene load
                break;

            // === ROOM 1 TESTS ===
            case 2:
                Check("Scene is Room 1", GameManager.Instance.State.CurrentScene == "Section1_Hub_Room1");
                Check("Auto-saved to slot 0", GameManager.Instance.SaveSystem.SlotExists(0));
                break;

            case 3:
                // Find and click optional terminal first (to set flag for alt text test later)
                var optTerminal = FindHotspot("OptionalTerminal");
                Check("OptionalTerminal exists", optTerminal != null);
                if (optTerminal != null)
                {
                    SimulateClick(optTerminal);
                    _timer = 0.8f;
                }
                break;

            case 4:
                Check("found_hub_log_01 flag set", GameManager.Instance.State.HasFlag("found_hub_log_01"));
                Check("Narrative is displaying", NarrativeManager.Instance.IsDisplaying);
                // Dismiss narrative
                DismissNarrative();
                _timer = 0.5f;
                break;

            case 5:
                Check("Narrative dismissed", !NarrativeManager.Instance.IsDisplaying);
                // Now click intro terminal — should show alt text since we have found_hub_log_01
                var introTerminal = FindHotspot("IntroTerminal");
                Check("IntroTerminal exists", introTerminal != null);
                if (introTerminal != null)
                    SimulateClick(introTerminal);
                _timer = 0.8f;
                break;

            case 6:
                Check("Narrative showing after intro terminal", NarrativeManager.Instance.IsDisplaying);
                // Can't easily check alt text content from here, but the log will show it
                DismissNarrative();
                _timer = 0.5f;
                break;

            // === TRANSITION TO ROOM 2 ===
            case 7:
                var door1 = FindHotspot("DoorToRoom2");
                Check("DoorToRoom2 exists", door1 != null);
                if (door1 != null)
                    SimulateClick(door1);
                _timer = 1.0f;
                break;

            case 8:
                Check("Scene is Room 2", GameManager.Instance.State.CurrentScene == "Section1_Hub_Room2");
                break;

            // === ROOM 2: KEYCARD + LOCKED DOOR ===
            case 9:
                // Door to Room 3 should NOT be available (no keycard)
                var lockedDoor = FindHotspot("DoorToRoom3");
                Check("DoorToRoom3 exists", lockedDoor != null);
                Check("DoorToRoom3 NOT available (no keycard)", lockedDoor != null && !lockedDoor.IsAvailable());
                break;

            case 10:
                // Pick up keycard
                var keycard = FindHotspot("KeycardPickup");
                Check("KeycardPickup exists", keycard != null);
                Check("KeycardPickup is available", keycard != null && keycard.IsAvailable());
                if (keycard != null)
                    SimulateClick(keycard);
                _timer = 0.8f;
                break;

            case 11:
                Check("keycard_hub in inventory", GameManager.Instance.State.HasItem("keycard_hub"));
                Check("picked_up_hub_keycard flag set", GameManager.Instance.State.HasFlag("picked_up_hub_keycard"));
                DismissNarrative();
                _timer = 0.5f;
                break;

            case 12:
                // Keycard should now be gone (blocked by flag)
                var keycardGone = FindHotspot("KeycardPickup");
                Check("KeycardPickup hidden after pickup", keycardGone == null || !keycardGone.IsAvailable());

                // Door should now be available
                var unlockedDoor = FindHotspot("DoorToRoom3");
                Check("DoorToRoom3 now available (has keycard)", unlockedDoor != null && unlockedDoor.IsAvailable());
                break;

            // === TRANSITION TO ROOM 3 ===
            case 13:
                var door3 = FindHotspot("DoorToRoom3");
                if (door3 != null)
                    SimulateClick(door3);
                _timer = 1.0f;
                break;

            case 14:
                Check("Scene is Room 3", GameManager.Instance.State.CurrentScene == "Section1_Hub_Room3");
                break;

            // === ROOM 3: POWER CONSOLE ===
            case 15:
                var console = FindHotspot("PowerConsole");
                Check("PowerConsole exists", console != null);
                if (console != null)
                    SimulateClick(console);
                _timer = 0.8f;
                break;

            case 16:
                Check("hub_power_restored flag set", GameManager.Instance.State.HasFlag("hub_power_restored"));
                DismissNarrative();
                _timer = 0.5f;
                break;

            // === SAVE/LOAD TEST ===
            case 17:
                GameLog.Event("Test", "--- Testing save/load ---");
                GameManager.Instance.SaveToSlot(2);
                Check("Saved to slot 2", GameManager.Instance.SaveSystem.SlotExists(2));

                // Record current state
                int flagCount = GameManager.Instance.State.FlagCount;
                bool hasKeycard = GameManager.Instance.State.HasItem("keycard_hub");
                string scene = GameManager.Instance.State.CurrentScene;

                // Reset and reload
                GameManager.Instance.NewGame();
                Check("State cleared after NewGame", GameManager.Instance.State.FlagCount == 0);

                bool loaded = GameManager.Instance.LoadFromSlot(2);
                Check("Load from slot 2 succeeded", loaded);
                Check("Flags restored", GameManager.Instance.State.FlagCount == flagCount);
                Check("Inventory restored", GameManager.Instance.State.HasItem("keycard_hub") == hasKeycard);
                Check("Scene restored", GameManager.Instance.State.CurrentScene == scene);
                break;

            // === ENDING EVALUATOR TEST ===
            case 18:
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
            case 19:
                GameLog.Event("Test", "--- Testing save slot management ---");
                Check("Slot 0 exists (auto-save)", GameManager.Instance.SaveSystem.SlotExists(0));
                Check("Slot 2 exists (manual save)", GameManager.Instance.SaveSystem.SlotExists(2));
                Check("Slot 3 empty", !GameManager.Instance.SaveSystem.SlotExists(3));
                Check("Slot -1 invalid", !GameManager.Instance.SaveSystem.SlotExists(-1));
                Check("Slot 5 invalid", !GameManager.Instance.SaveSystem.SlotExists(5));

                GameManager.Instance.SaveSystem.Delete(2);
                Check("Slot 2 deleted", !GameManager.Instance.SaveSystem.SlotExists(2));
                break;

            // === BACKTRACKING TEST ===
            case 20:
                GameLog.Event("Test", "--- Testing backtracking ---");
                // Reload state and go back to Room 2
                GameManager.Instance.LoadFromSlot(0);
                SceneLoader.Instance.LoadScene("Section1_Hub_Room2");
                _timer = 1.0f;
                break;

            case 21:
                Check("Backtrack to Room 2", GameManager.Instance.State.CurrentScene == "Section1_Hub_Room2");
                // Go back to Room 1
                var doorBack = FindHotspot("DoorToRoom1");
                Check("DoorToRoom1 exists in Room 2", doorBack != null);
                if (doorBack != null)
                    SimulateClick(doorBack);
                _timer = 1.0f;
                break;

            case 22:
                Check("Backtrack to Room 1", GameManager.Instance.State.CurrentScene == "Section1_Hub_Room1");
                break;

            // === REPORT ===
            case 23:
                Report();
                _done = true;
                // Quit after report
                _timer = 1.0f;
                break;

            case 24:
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
