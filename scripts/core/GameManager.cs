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
        Instance = this;
        State = new GameState();
        string saveDir = System.IO.Path.Combine(
            OS.GetUserDataDir(), "saves");
        SaveSystem = new SaveSystem(saveDir, MaxSaveSlots);
        GameLog.ManagerReady("GameManager");
    }

    public void SaveToSlot(int slot)
    {
        SaveData data = State.ToSaveData();
        SaveSystem.Save(slot, data);
        GameLog.SavedToSlot(slot);
    }

    public bool LoadFromSlot(int slot)
    {
        SaveData data = SaveSystem.Load(slot);
        if (data == null) return false;
        State.LoadFromSaveData(data);
        GameLog.LoadedFromSlot(slot);
        return true;
    }

    public void NewGame()
    {
        State.Reset();
        GameLog.NewGame();
    }
}
