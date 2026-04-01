using Godot;
using Signal.Inventory;

namespace Signal.UI;

public partial class InventoryUI : CanvasLayer
{
    private HBoxContainer _slotContainer;
    private PanelContainer _bar;
    private bool _isVisible;

    public override void _Ready()
    {
        Layer = 5;
        BuildUI();
        _bar.Visible = false;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.InventoryChanged += Refresh;
    }

    private void BuildUI()
    {
        var root = new Control();
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);

        _bar = new PanelContainer();
        _bar.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _bar.OffsetBottom = 60;
        _bar.CustomMinimumSize = new Vector2(0, 60);

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0, 0, 0, 0.7f);
        style.ContentMarginLeft = 12;
        style.ContentMarginRight = 12;
        style.ContentMarginTop = 8;
        style.ContentMarginBottom = 8;
        _bar.AddThemeStyleboxOverride("panel", style);
        _bar.MouseFilter = Control.MouseFilterEnum.Ignore;
        root.AddChild(_bar);

        _slotContainer = new HBoxContainer();
        _slotContainer.AddThemeConstantOverride("separation", 8);
        _slotContainer.Alignment = BoxContainer.AlignmentMode.Begin;
        _bar.AddChild(_slotContainer);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Tab)
        {
            _isVisible = !_isVisible;
            _bar.Visible = _isVisible;
            if (_isVisible) Refresh();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Refresh()
    {
        foreach (var child in _slotContainer.GetChildren())
            child.QueueFree();

        if (InventoryManager.Instance == null) return;

        var items = InventoryManager.Instance.GetHeldItemDefinitions();
        foreach (var item in items)
        {
            var slot = new PanelContainer();
            slot.CustomMinimumSize = new Vector2(44, 44);
            var slotStyle = new StyleBoxFlat();
            slotStyle.BgColor = new Color(0.2f, 0.2f, 0.25f, 0.9f);
            slot.AddThemeStyleboxOverride("panel", slotStyle);

            var label = new Label();
            label.Text = item.DisplayName;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.AddThemeFontSizeOverride("font_size", 11);
            slot.AddChild(label);

            _slotContainer.AddChild(slot);
        }

        Core.GameLog.Event("UI", $"Inventory refreshed: {items.Count} items");
    }
}
