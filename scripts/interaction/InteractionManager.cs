using Godot;
using Signal.Core;
using Signal.Narrative;
using Signal.Inventory;
using Signal.Minigame;

namespace Signal.Interaction;

public partial class InteractionManager : Node
{
    public static InteractionManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        GameLog.ManagerReady("InteractionManager");
    }

    public void ConnectHotspot(Hotspot hotspot)
    {
        hotspot.Clicked += OnHotspotClicked;
    }

    public void DisconnectHotspot(Hotspot hotspot)
    {
        hotspot.Clicked -= OnHotspotClicked;
    }

    private void OnHotspotClicked(Hotspot hotspot)
    {
        // Block clicks when narrative is displaying
        if (Narrative.NarrativeManager.Instance?.IsDisplaying == true) return;

        var action = hotspot.GetAction();
        if (action == null) return;
        GameLog.HotspotClicked(hotspot.Name, action.Type.ToString());
        ExecuteAction(action);
    }

    private void ExecuteAction(HotspotData action)
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;

        // Puzzle gate: if required and not yet solved, launch puzzle first
        if (action.RequiresPuzzle)
        {
            string solvedFlag = $"solved_{action.FlagToSet}";
            if (!string.IsNullOrEmpty(solvedFlag) && !state.HasFlag(solvedFlag))
            {
                LaunchPuzzleGate(action, solvedFlag);
                return;
            }
        }

        ExecuteActionDirect(action);
    }

    private void ExecuteActionDirect(HotspotData action)
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;

        if (!string.IsNullOrEmpty(action.ItemToConsume))
            InventoryManager.Instance?.RemoveItem(action.ItemToConsume);

        if (!string.IsNullOrEmpty(action.FlagToSet))
        {
            state.SetFlag(action.FlagToSet);
            GameLog.FlagSet(action.FlagToSet);
        }

        if (!string.IsNullOrEmpty(action.EvidenceToDiscover))
            Evidence.EvidenceManager.Instance?.Discover(action.EvidenceToDiscover);

        if (!string.IsNullOrEmpty(action.ItemToGrant))
            InventoryManager.Instance?.AddItem(action.ItemToGrant);

        switch (action.Type)
        {
            case HotspotType.Examine:
            case HotspotType.PickUp:
                NarrativeManager.Instance?.ShowText(action.ExamineText);
                break;

            case HotspotType.Door:
                SceneLoader.Instance?.LoadScene(action.TargetScene, action.IsNewSection);
                break;

            case HotspotType.Terminal:
            case HotspotType.Narration:
                if (!string.IsNullOrEmpty(action.NarrativeEntryId))
                    NarrativeManager.Instance?.PlayEntry(action.NarrativeEntryId);
                else
                    NarrativeManager.Instance?.ShowText(action.ExamineText);
                break;
        }
    }

    private DecryptionPuzzleUI _activePuzzle;
    private HotspotData _pendingAction;

    private void LaunchPuzzleGate(HotspotData action, string solvedFlag)
    {
        _pendingAction = action;

        // Determine puzzle difficulty from game state
        int section = GameManager.Instance?.State != null ? 1 : 1; // TODO: get from current section
        // For now, use section 1 defaults. PuzzleOverride support added later.

        var puzzleLayer = new CanvasLayer();
        puzzleLayer.Layer = 15;
        puzzleLayer.Name = "PuzzleGateLayer";
        GetTree().Root.AddChild(puzzleLayer);

        _activePuzzle = new DecryptionPuzzleUI();
        _activePuzzle.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        puzzleLayer.AddChild(_activePuzzle);

        _activePuzzle.PuzzleCompleted += (guesses, time) =>
        {
            GameLog.Event("Puzzle", $"Gate solved: {solvedFlag} in {guesses} guesses, {time:F1}s");
            GameManager.Instance?.State?.SetFlag(solvedFlag);

            // Clean up puzzle
            puzzleLayer.QueueFree();
            _activePuzzle = null;

            // Resume the gated action
            if (_pendingAction != null)
            {
                var resumed = _pendingAction;
                _pendingAction = null;
                ExecuteActionDirect(resumed);
            }
        };

        _activePuzzle.PuzzleCancelled += () =>
        {
            puzzleLayer.QueueFree();
            _activePuzzle = null;
            _pendingAction = null;
        };

        _activePuzzle.StartPuzzle(section);
        GameLog.Event("Puzzle", $"Puzzle gate launched for: {action.FlagToSet}");
    }
}
