using System.Collections.Generic;
using Godot;
using Signal.Core;

namespace Signal.Inventory;

public partial class InventoryManager : Node
{
    public static InventoryManager Instance { get; private set; }

    [Signal] public delegate void InventoryChangedEventHandler();

    private readonly Dictionary<string, ItemDefinition> _itemLookup = new();

    public override void _Ready()
    {
        Instance = this;
        LoadItemDefinitions();
    }

    private void LoadItemDefinitions()
    {
        // Load all .tres files from data/items/
        string dir = "res://data/items/";
        if (!DirAccess.DirExistsAbsolute(dir)) return;

        var dirAccess = DirAccess.Open(dir);
        if (dirAccess == null) return;

        dirAccess.ListDirBegin();
        string file = dirAccess.GetNext();
        while (file != "")
        {
            if (file.EndsWith(".tres"))
            {
                var item = GD.Load<ItemDefinition>(dir + file);
                if (item != null)
                    _itemLookup[item.ItemId] = item;
            }
            file = dirAccess.GetNext();
        }
    }

    public void AddItem(string itemId)
    {
        GameManager.Instance.State.AddItem(itemId);
        EmitSignal(SignalName.InventoryChanged);
    }

    public void RemoveItem(string itemId)
    {
        GameManager.Instance.State.RemoveItem(itemId);
        EmitSignal(SignalName.InventoryChanged);
    }

    public bool HasItem(string itemId) => GameManager.Instance.State.HasItem(itemId);

    public ItemDefinition GetDefinition(string itemId) =>
        _itemLookup.TryGetValue(itemId, out var def) ? def : null;

    public List<ItemDefinition> GetHeldItemDefinitions()
    {
        var result = new List<ItemDefinition>();
        foreach (string id in GameManager.Instance.State.Inventory)
        {
            if (_itemLookup.TryGetValue(id, out var def))
                result.Add(def);
        }
        return result;
    }
}
