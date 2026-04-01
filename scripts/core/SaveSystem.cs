using System.IO;
using System.Text.Json;

namespace Signal.Core;

public class SaveSystem
{
    private readonly string _saveDirectory;
    private readonly int _maxSlots;

    public SaveSystem(string saveDirectory, int maxSlots)
    {
        _saveDirectory = saveDirectory;
        _maxSlots = maxSlots;
    }

    public bool Save(int slot, SaveData data)
    {
        if (slot < 0 || slot >= _maxSlots) return false;

        Directory.CreateDirectory(_saveDirectory);
        string path = GetSlotPath(slot);
        string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
        return true;
    }

    public SaveData Load(int slot)
    {
        if (slot < 0 || slot >= _maxSlots) return null;

        string path = GetSlotPath(slot);
        if (!File.Exists(path)) return null;

        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SaveData>(json);
    }

    public bool SlotExists(int slot)
    {
        if (slot < 0 || slot >= _maxSlots) return false;
        return File.Exists(GetSlotPath(slot));
    }

    public void Delete(int slot)
    {
        string path = GetSlotPath(slot);
        if (File.Exists(path))
            File.Delete(path);
    }

    private string GetSlotPath(int slot) =>
        Path.Combine(_saveDirectory, $"save_{slot}.json");
}
