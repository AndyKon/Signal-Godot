using Godot;
using Signal.Core;
using Signal.Narrative;
using Signal.Inventory;

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
}
