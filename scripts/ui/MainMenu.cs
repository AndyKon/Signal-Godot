using Godot;
using Signal.Core;
using Signal.Interaction;

namespace Signal.UI;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GetNode<Button>("VBox/NewGameButton").Pressed += OnNewGame;
        GetNode<Button>("VBox/LoadGameButton").Pressed += OnLoadGame;
        GetNode<Button>("VBox/QuitButton").Pressed += OnQuit;
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
        if (GameManager.Instance?.LoadFromSlot(1) == true)
        {
            string scene = GameManager.Instance.State.CurrentScene;
            SceneLoader.Instance?.LoadScene(scene);
        }
    }

    private void OnQuit()
    {
        GetTree().Quit();
    }
}
