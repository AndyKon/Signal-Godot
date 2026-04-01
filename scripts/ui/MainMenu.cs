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
    }

    private void OnNewGame()
    {
        GameManager.Instance?.NewGame();
        SceneLoader.Instance?.LoadScene("Section1_Hub_Room1", isNewSection: true);
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
