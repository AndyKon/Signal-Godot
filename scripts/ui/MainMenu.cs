using Godot;
using Signal.Core;
using Signal.Interaction;

namespace Signal.UI;

public partial class MainMenu : Control
{
    private SaveSlotUI _saveSlotUI;

    public override void _Ready()
    {
        GetNode<Button>("VBox/NewGameButton").Pressed += OnNewGame;
        GetNode<Button>("VBox/LoadGameButton").Pressed += OnLoadGame;
        GetNode<Button>("VBox/QuitButton").Pressed += OnQuit;

        // Find SaveSlotUI from autoload
        var managers = GetTree().Root.GetNodeOrNull("Managers");
        if (managers != null)
            _saveSlotUI = managers.GetNodeOrNull<SaveSlotUI>("SaveSlotUI");

        GameLog.Event("UI", "MainMenu ready");
    }

    private void OnNewGame()
    {
        if (GameManager.Instance == null)
        {
            GameLog.Error("UI", "GameManager.Instance is null");
            return;
        }
        if (SceneLoader.Instance == null)
        {
            GameLog.Error("UI", "SceneLoader.Instance is null");
            return;
        }

        GameManager.Instance.NewGame();
        SceneLoader.Instance.LoadScene("Section1_Hub_Room1", isNewSection: true);
    }

    private void OnLoadGame()
    {
        if (_saveSlotUI != null)
        {
            _saveSlotUI.Show(slot =>
            {
                if (GameManager.Instance?.LoadFromSlot(slot) == true)
                {
                    string scene = GameManager.Instance.State.CurrentScene;
                    SceneLoader.Instance?.LoadScene(scene);
                }
            }, showEmpty: false);
        }
    }

    private void OnQuit()
    {
        GetTree().Quit();
    }
}
