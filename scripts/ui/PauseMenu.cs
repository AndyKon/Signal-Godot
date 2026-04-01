using Godot;
using Signal.Core;

namespace Signal.UI;

public partial class PauseMenu : CanvasLayer
{
    private Control _root;
    private PanelContainer _panel;
    private bool _isPaused;
    private SaveSlotUI _saveSlotUI;

    public override void _Ready()
    {
        Layer = 20;
        BuildUI();
        _panel.Visible = false;
        ProcessMode = ProcessModeEnum.Always;

        // Find sibling SaveSlotUI in autoload
        CallDeferred(MethodName.WireSaveSlotUI);
    }

    private void WireSaveSlotUI()
    {
        var parent = GetParent();
        if (parent != null)
            _saveSlotUI = parent.GetNodeOrNull<SaveSlotUI>("SaveSlotUI");
        GameLog.ManagerReady("PauseMenu");
    }

    private void BuildUI()
    {
        _root = new Control();
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(_root);

        _panel = new PanelContainer();
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _panel.CustomMinimumSize = new Vector2(300, 250);
        _panel.Position = new Vector2(-150, -125);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.1f, 0.1f, 0.15f, 0.95f);
        style.ContentMarginLeft = 20;
        style.ContentMarginRight = 20;
        style.ContentMarginTop = 30;
        style.ContentMarginBottom = 30;
        _panel.AddThemeStyleboxOverride("panel", style);
        _root.AddChild(_panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        vbox.Alignment = BoxContainer.AlignmentMode.Center;
        _panel.AddChild(vbox);

        var title = new Label();
        title.Text = "PAUSED";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 24);
        vbox.AddChild(title);

        var resumeBtn = CreateButton("Resume");
        resumeBtn.Pressed += Resume;
        vbox.AddChild(resumeBtn);

        var saveBtn = CreateButton("Save");
        saveBtn.Pressed += OnSave;
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
        GameLog.Event("UI", "Game paused");
    }

    private void Resume()
    {
        _isPaused = false;
        _panel.Visible = false;
        GetTree().Paused = false;
        GameLog.Event("UI", "Game resumed");
    }

    private void OnSave()
    {
        if (_saveSlotUI != null)
        {
            _saveSlotUI.Show(slot =>
            {
                GameManager.Instance.SaveToSlot(slot);
                GameLog.SavedToSlot(slot);
            }, showEmpty: true);
        }
        else
        {
            // Fallback to slot 1 if SaveSlotUI not wired
            GameManager.Instance.SaveToSlot(1);
            GameLog.SavedToSlot(1);
        }
    }

    private void QuitToMenu()
    {
        GetTree().Paused = false;
        // Dismiss any open UI before going to menu
        Narrative.NarrativeManager.Instance?.Hide();
        _panel.Visible = false;
        _isPaused = false;
        GetTree().ChangeSceneToFile("res://scenes/MainMenu.tscn");
        GameLog.Event("UI", "Quit to menu");
    }
}
