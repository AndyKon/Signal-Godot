using System;
using Godot;
using Signal.Core;

namespace Signal.UI;

public partial class SaveSlotUI : CanvasLayer
{
    private PanelContainer _panel;
    private VBoxContainer _slotContainer;
    private Action<int> _onSlotSelected;
    private bool _showEmpty;

    public override void _Ready()
    {
        Layer = 25;
        BuildUI();
        _panel.Visible = false;
        ProcessMode = ProcessModeEnum.Always;
    }

    private void BuildUI()
    {
        var root = new Control();
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);

        // Dimmed background
        var dimmer = new ColorRect();
        dimmer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        dimmer.Color = new Color(0, 0, 0, 0.5f);
        dimmer.MouseFilter = Control.MouseFilterEnum.Ignore;

        _panel = new PanelContainer();
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        _panel.CustomMinimumSize = new Vector2(350, 320);
        _panel.Position = new Vector2(-175, -160);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
        style.ContentMarginLeft = 16;
        style.ContentMarginRight = 16;
        style.ContentMarginTop = 16;
        style.ContentMarginBottom = 16;
        _panel.AddThemeStyleboxOverride("panel", style);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);

        var title = new Label();
        title.Text = "Save Slots";
        title.HorizontalAlignment = HorizontalAlignment.Center;
        title.AddThemeFontSizeOverride("font_size", 20);
        vbox.AddChild(title);

        _slotContainer = new VBoxContainer();
        _slotContainer.AddThemeConstantOverride("separation", 6);
        vbox.AddChild(_slotContainer);

        var cancelBtn = new Button();
        cancelBtn.Text = "Cancel";
        cancelBtn.Pressed += Hide;
        vbox.AddChild(cancelBtn);

        _panel.AddChild(vbox);
        root.AddChild(dimmer);
        root.AddChild(_panel);
    }

    public void Show(Action<int> onSlotSelected, bool showEmpty = true)
    {
        _onSlotSelected = onSlotSelected;
        _showEmpty = showEmpty;
        PopulateSlots();
        _panel.Visible = true;
        _panel.GetParent<Control>().GetChild<ColorRect>(0).Visible = true;
        GameLog.Event("UI", $"SaveSlotUI shown (showEmpty={showEmpty})");
    }

    public new void Hide()
    {
        _panel.Visible = false;
        _panel.GetParent<Control>().GetChild<ColorRect>(0).Visible = false;
    }

    private void PopulateSlots()
    {
        foreach (var child in _slotContainer.GetChildren())
            child.QueueFree();

        var saveSystem = GameManager.Instance.SaveSystem;

        for (int i = 0; i < 5; i++)
        {
            bool exists = saveSystem.SlotExists(i);
            if (!exists && !_showEmpty) continue;

            int slotIndex = i;
            var btn = new Button();

            if (exists)
            {
                var data = saveSystem.Load(i);
                string sceneName = string.IsNullOrEmpty(data.CurrentScene) ? "Unknown" : data.CurrentScene;
                btn.Text = $"Slot {i + 1}: {sceneName} ({data.Flags.Count} flags)";
            }
            else
            {
                btn.Text = $"Slot {i + 1}: Empty";
            }

            btn.Pressed += () =>
            {
                _onSlotSelected?.Invoke(slotIndex);
                Hide();
            };

            _slotContainer.AddChild(btn);
        }
    }
}
