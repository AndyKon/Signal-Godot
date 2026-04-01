using System.Collections.Generic;

namespace Signal.Core;

public class GameState
{
    private readonly HashSet<string> _flags = new();
    private readonly HashSet<int> _poweredSections = new();
    private readonly List<string> _inventory = new();
    private int _totalOptionalFlags;

    public string CurrentScene { get; set; } = "";
    public int FlagCount => _flags.Count;
    public float FlagRatio => _totalOptionalFlags > 0 ? (float)_flags.Count / _totalOptionalFlags : 0f;
    public IReadOnlyList<string> Inventory => _inventory;

    public void SetFlag(string flag) => _flags.Add(flag);
    public bool HasFlag(string flag) => _flags.Contains(flag);
    public void RegisterTotalOptionalFlags(int total) => _totalOptionalFlags = total;

    public void SetSectionPowered(int section) => _poweredSections.Add(section);
    public bool IsSectionPowered(int section) => _poweredSections.Contains(section);

    public void AddItem(string itemId)
    {
        if (!_inventory.Contains(itemId))
            _inventory.Add(itemId);
    }

    public void RemoveItem(string itemId) => _inventory.Remove(itemId);
    public bool HasItem(string itemId) => _inventory.Contains(itemId);

    public SaveData ToSaveData()
    {
        return new SaveData
        {
            Flags = new List<string>(_flags),
            PoweredSections = new List<int>(_poweredSections),
            CurrentScene = CurrentScene,
            InventoryItems = new List<string>(_inventory),
            TotalOptionalFlags = _totalOptionalFlags
        };
    }

    public void LoadFromSaveData(SaveData data)
    {
        Reset();
        foreach (var flag in data.Flags) _flags.Add(flag);
        foreach (var section in data.PoweredSections) _poweredSections.Add(section);
        CurrentScene = data.CurrentScene;
        foreach (var item in data.InventoryItems) _inventory.Add(item);
        _totalOptionalFlags = data.TotalOptionalFlags;
    }

    public void Reset()
    {
        _flags.Clear();
        _poweredSections.Clear();
        _inventory.Clear();
        CurrentScene = "";
        _totalOptionalFlags = 0;
    }
}
