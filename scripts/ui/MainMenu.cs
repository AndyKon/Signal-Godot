using Godot;
using Signal.Core;
using Signal.Interaction;

namespace Signal.UI;

public partial class MainMenu : Control
{
    public override void _Ready()
    {
        GD.Print("[MainMenu] _Ready called");
        GD.Print($"[MainMenu] GameManager.Instance = {GameManager.Instance}");
        GD.Print($"[MainMenu] SceneLoader.Instance = {SceneLoader.Instance}");

        GetNode<Button>("VBox/NewGameButton").Pressed += OnNewGame;
        GetNode<Button>("VBox/LoadGameButton").Pressed += OnLoadGame;
        GetNode<Button>("VBox/QuitButton").Pressed += OnQuit;
    }

    private void OnNewGame()
    {
        GD.Print("[MainMenu] New Game clicked");
        GD.Print($"[MainMenu] GameManager.Instance = {GameManager.Instance}");
        GD.Print($"[MainMenu] SceneLoader.Instance = {SceneLoader.Instance}");

        if (GameManager.Instance == null)
        {
            GD.PrintErr("[MainMenu] GameManager.Instance is null!");
            return;
        }

        if (SceneLoader.Instance == null)
        {
            GD.PrintErr("[MainMenu] SceneLoader.Instance is null!");
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
