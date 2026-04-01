using Godot;
using Signal.Core;

namespace Signal.UI;

public partial class PauseMenu : CanvasLayer
{
    private PanelContainer _panel;
    private bool _isPaused;

    public override void _Ready()
    {
        Layer = 20;
        BuildUI();
        _panel.Visible = false;
        ProcessMode = ProcessModeEnum.Always;
    }

    private void BuildUI()
    {
        _panel = new PanelContainer();
        _panel.AnchorsPreset = (int)Control.LayoutPreset.Center;
        _panel.CustomMinimumSize = new Vector2(300, 250);
        _panel.Position = new Vector2(-150, -125);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        style.ContentMarginLeft = 20;
        style.ContentMarginRight = 20;
        style.ContentMarginTop = 30;
        style.ContentMarginBottom = 30;
        _panel.AddThemeStyleboxOverride("panel", style);
        AddChild(_panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        _panel.AddChild(vbox);

        var resumeBtn = CreateButton("Resume");
        resumeBtn.Pressed += Resume;
        vbox.AddChild(resumeBtn);

        var saveBtn = CreateButton("Save");
        saveBtn.Pressed += () => GameManager.Instance?.SaveToSlot(1);
        vbox.AddChild(saveBtn);

        var quitBtn = CreateButton("Quit to Menu");
        quitBtn.Pressed += QuitToMenu;
        vbox.AddChild(quitBtn);
    }

    private Button CreateButton(string text)
    {
        var btn = new Button();
        btn.Text = text;
        btn.CustomMinimumSize = new Vector2(200, 40);
        return btn;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel"))
        {
            if (_isPaused) Resume();
            else Pause();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Pause()
    {
        _isPaused = true;
        _panel.Visible = true;
        GetTree().Paused = true;
    }

    private void Resume()
    {
        _isPaused = false;
        _panel.Visible = false;
        GetTree().Paused = false;
    }

    private void QuitToMenu()
    {
        GetTree().Paused = false;
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
    }
}
