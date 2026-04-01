using Godot;

namespace Signal.Core;

public partial class GameManager : Node
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; }
    public SaveSystem SaveSystem { get; private set; }

    private const int MaxSaveSlots = 5;

    public override void _Ready()
    {
        GD.Print("[GameManager] _Ready called");
        Instance = this;
        State = new GameState();
        string saveDir = System.IO.Path.Combine(
            OS.GetUserDataDir(), "saves");
        SaveSystem = new SaveSystem(saveDir, MaxSaveSlots);
    }

    public void SaveToSlot(int slot)
    {
        SaveData data = State.ToSaveData();
        SaveSystem.Save(slot, data);
    }

    public bool LoadFromSlot(int slot)
    {
        SaveData data = SaveSystem.Load(slot);
        if (data == null) return false;
        State.LoadFromSaveData(data);
        return true;
    }

    public void NewGame()
    {
        State.Reset();
    }
}
