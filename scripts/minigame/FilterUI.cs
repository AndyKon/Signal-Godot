using Godot;

namespace Signal.Minigame;

public partial class FilterUI : Control
{
    // Signals emitted when the player changes values
    [Signal] public delegate void PresetSelectedEventHandler(int presetIndex);
    [Signal] public delegate void FilterChangedEventHandler(float frequency, float amplitude, float phase);

    // Preset colors: Crew Log (green), Sensor Data (blue), System Message (orange), Encrypted (purple)
    private static readonly Color[] PresetColors =
    {
        new(0.3f, 0.85f, 0.4f),   // Crew Log — green
        new(0.3f, 0.55f, 0.95f),   // Sensor Data — blue
        new(0.95f, 0.6f, 0.2f),    // System Message — orange
        new(0.7f, 0.35f, 0.9f),    // Encrypted — purple
    };

    private static readonly string[] PresetLabels = { "Crew Log", "Sensor", "System", "Encrypted" };

    private static readonly Color PanelBg = new(0.06f, 0.07f, 0.12f, 0.95f);
    private static readonly Color LabelDim = new(0.65f, 0.68f, 0.72f);
    private static readonly Color TrackDark = new(0.12f, 0.13f, 0.18f);
    private static readonly Color SliderThumbDefault = new(0.55f, 0.58f, 0.62f);
    private static readonly Color ButtonIdleBg = new(0.12f, 0.13f, 0.18f);
    private static readonly Color ButtonIdleBorder = new(0.25f, 0.27f, 0.32f);

    // Current values
    public int SelectedPreset { get; private set; } = -1; // -1 = none selected
    public float Frequency => (float)(_frequencySlider?.Value ?? 0.5);
    public float Amplitude => (float)(_amplitudeSlider?.Value ?? 0.5);
    public float Phase => (float)(_phaseSlider?.Value ?? 0.5);

    private HSlider _frequencySlider;
    private HSlider _amplitudeSlider;
    private HSlider _phaseSlider;
    private Label _frequencyValueLabel;
    private Label _amplitudeValueLabel;
    private Label _phaseValueLabel;
    private Button[] _presetButtons;

    public override void _Ready()
    {
        // Root panel with dark background
        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = PanelBg;
        panelStyle.ContentMarginLeft = 16;
        panelStyle.ContentMarginRight = 16;
        panelStyle.ContentMarginTop = 14;
        panelStyle.ContentMarginBottom = 14;
        panelStyle.CornerRadiusBottomLeft = 4;
        panelStyle.CornerRadiusBottomRight = 4;
        panelStyle.CornerRadiusTopLeft = 4;
        panelStyle.CornerRadiusTopRight = 4;
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 14);
        panel.AddChild(vbox);

        // --- Section: Preset buttons ---
        var presetLabel = MakeSectionLabel("SIGNAL TYPE");
        vbox.AddChild(presetLabel);

        var presetRow = new HBoxContainer();
        presetRow.AddThemeConstantOverride("separation", 8);
        vbox.AddChild(presetRow);

        _presetButtons = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            var btn = new Button();
            btn.Text = PresetLabels[i];
            btn.CustomMinimumSize = new Vector2(110, 36);
            btn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            btn.AddThemeFontSizeOverride("font_size", 13);

            ApplyButtonIdleStyle(btn);

            int index = i; // capture for closure
            btn.Pressed += () => OnPresetPressed(index);

            presetRow.AddChild(btn);
            _presetButtons[i] = btn;
        }

        // --- Separator ---
        var sep = new HSeparator();
        var sepStyle = new StyleBoxFlat();
        sepStyle.BgColor = new Color(0.2f, 0.22f, 0.28f, 0.6f);
        sepStyle.ContentMarginTop = 2;
        sepStyle.ContentMarginBottom = 2;
        sep.AddThemeStyleboxOverride("separator", sepStyle);
        vbox.AddChild(sep);

        // --- Section: Filter sliders ---
        var filterLabel = MakeSectionLabel("FILTER CONTROLS");
        vbox.AddChild(filterLabel);

        (_frequencySlider, _frequencyValueLabel) = CreateSliderRow(vbox, "Frequency");
        (_amplitudeSlider, _amplitudeValueLabel) = CreateSliderRow(vbox, "Amplitude");
        (_phaseSlider, _phaseValueLabel) = CreateSliderRow(vbox, "Phase");

        _frequencySlider.ValueChanged += _ => OnSliderChanged();
        _amplitudeSlider.ValueChanged += _ => OnSliderChanged();
        _phaseSlider.ValueChanged += _ => OnSliderChanged();
    }

    // ------------------------------------------------------------------ Preset buttons
    private void OnPresetPressed(int index)
    {
        // If clicking the already-selected preset, deselect it
        if (SelectedPreset == index)
        {
            SelectedPreset = -1;
            RefreshPresetStyles();
            EmitSignal(SignalName.PresetSelected, -1);
            return;
        }

        SelectedPreset = index;
        RefreshPresetStyles();
        RefreshSliderThumbColor();
        EmitSignal(SignalName.PresetSelected, index);
    }

    private void RefreshPresetStyles()
    {
        for (int i = 0; i < _presetButtons.Length; i++)
        {
            if (i == SelectedPreset)
                ApplyButtonSelectedStyle(_presetButtons[i], PresetColors[i]);
            else
                ApplyButtonIdleStyle(_presetButtons[i]);
        }
    }

    private void RefreshSliderThumbColor()
    {
        Color thumbColor = SelectedPreset >= 0 ? PresetColors[SelectedPreset] : SliderThumbDefault;
        ApplySliderThumbColor(_frequencySlider, thumbColor);
        ApplySliderThumbColor(_amplitudeSlider, thumbColor);
        ApplySliderThumbColor(_phaseSlider, thumbColor);
    }

    // ------------------------------------------------------------------ Sliders
    private void OnSliderChanged()
    {
        _frequencyValueLabel.Text = Frequency.ToString("F2");
        _amplitudeValueLabel.Text = Amplitude.ToString("F2");
        _phaseValueLabel.Text = Phase.ToString("F2");

        EmitSignal(SignalName.FilterChanged, Frequency, Amplitude, Phase);
    }

    private (HSlider slider, Label valueLabel) CreateSliderRow(VBoxContainer parent, string name)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        parent.AddChild(row);

        // Left label — parameter name
        var nameLabel = new Label();
        nameLabel.Text = name;
        nameLabel.CustomMinimumSize = new Vector2(90, 0);
        nameLabel.AddThemeFontSizeOverride("font_size", 13);
        nameLabel.AddThemeColorOverride("font_color", LabelDim);
        nameLabel.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(nameLabel);

        // Slider
        var slider = new HSlider();
        slider.MinValue = 0.0;
        slider.MaxValue = 1.0;
        slider.Step = 0.01;
        slider.Value = 0.5;
        slider.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        slider.CustomMinimumSize = new Vector2(0, 24);
        ApplySliderStyle(slider);
        row.AddChild(slider);

        // Right label — current value
        var valueLabel = new Label();
        valueLabel.Text = "0.50";
        valueLabel.CustomMinimumSize = new Vector2(42, 0);
        valueLabel.HorizontalAlignment = HorizontalAlignment.Right;
        valueLabel.AddThemeFontSizeOverride("font_size", 13);
        valueLabel.AddThemeColorOverride("font_color", LabelDim);
        valueLabel.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(valueLabel);

        return (slider, valueLabel);
    }

    // ------------------------------------------------------------------ Public API

    /// <summary>Reset to default state: no preset selected, sliders centered.</summary>
    public void Reset()
    {
        SelectedPreset = -1;
        RefreshPresetStyles();

        _frequencySlider.Value = 0.5;
        _amplitudeSlider.Value = 0.5;
        _phaseSlider.Value = 0.5;

        RefreshSliderThumbColor();
        OnSliderChanged();
    }

    /// <summary>
    /// Enable or disable each preset button.
    /// Section 1 might only show Sensor Data, for example.
    /// </summary>
    public void SetAvailablePresets(bool crew, bool sensor, bool system, bool encrypted)
    {
        bool[] available = { crew, sensor, system, encrypted };
        for (int i = 0; i < _presetButtons.Length; i++)
        {
            _presetButtons[i].Disabled = !available[i];
            _presetButtons[i].Visible = available[i];
        }

        // If the currently selected preset was disabled, clear selection
        if (SelectedPreset >= 0 && !available[SelectedPreset])
        {
            SelectedPreset = -1;
            RefreshPresetStyles();
            RefreshSliderThumbColor();
        }
    }

    // ------------------------------------------------------------------ Styling helpers

    private static Label MakeSectionLabel(string text)
    {
        var label = new Label();
        label.Text = text;
        label.AddThemeFontSizeOverride("font_size", 11);
        label.AddThemeColorOverride("font_color", new Color(0.45f, 0.48f, 0.55f));
        return label;
    }

    private static void ApplyButtonIdleStyle(Button btn)
    {
        var normal = new StyleBoxFlat();
        normal.BgColor = ButtonIdleBg;
        normal.BorderColor = ButtonIdleBorder;
        normal.BorderWidthBottom = 1;
        normal.BorderWidthTop = 1;
        normal.BorderWidthLeft = 1;
        normal.BorderWidthRight = 1;
        normal.CornerRadiusBottomLeft = 3;
        normal.CornerRadiusBottomRight = 3;
        normal.CornerRadiusTopLeft = 3;
        normal.CornerRadiusTopRight = 3;
        normal.ContentMarginLeft = 8;
        normal.ContentMarginRight = 8;
        normal.ContentMarginTop = 4;
        normal.ContentMarginBottom = 4;
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = new Color(0.16f, 0.17f, 0.24f);
        btn.AddThemeStyleboxOverride("hover", hover);

        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.BgColor = new Color(0.1f, 0.11f, 0.16f);
        btn.AddThemeStyleboxOverride("pressed", pressed);

        var disabled = (StyleBoxFlat)normal.Duplicate();
        disabled.BgColor = new Color(0.08f, 0.08f, 0.1f, 0.5f);
        btn.AddThemeStyleboxOverride("disabled", disabled);

        btn.AddThemeColorOverride("font_color", new Color(0.6f, 0.62f, 0.68f));
        btn.AddThemeColorOverride("font_hover_color", new Color(0.75f, 0.78f, 0.82f));
        btn.AddThemeColorOverride("font_disabled_color", new Color(0.3f, 0.32f, 0.36f));
    }

    private static void ApplyButtonSelectedStyle(Button btn, Color accentColor)
    {
        var normal = new StyleBoxFlat();
        normal.BgColor = new Color(accentColor.R * 0.2f, accentColor.G * 0.2f, accentColor.B * 0.2f, 0.9f);
        normal.BorderColor = accentColor;
        normal.BorderWidthBottom = 2;
        normal.BorderWidthTop = 2;
        normal.BorderWidthLeft = 2;
        normal.BorderWidthRight = 2;
        normal.CornerRadiusBottomLeft = 3;
        normal.CornerRadiusBottomRight = 3;
        normal.CornerRadiusTopLeft = 3;
        normal.CornerRadiusTopRight = 3;
        normal.ContentMarginLeft = 8;
        normal.ContentMarginRight = 8;
        normal.ContentMarginTop = 4;
        normal.ContentMarginBottom = 4;
        btn.AddThemeStyleboxOverride("normal", normal);

        var hover = (StyleBoxFlat)normal.Duplicate();
        hover.BgColor = new Color(accentColor.R * 0.25f, accentColor.G * 0.25f, accentColor.B * 0.25f, 0.95f);
        btn.AddThemeStyleboxOverride("hover", hover);

        var pressed = (StyleBoxFlat)normal.Duplicate();
        pressed.BgColor = new Color(accentColor.R * 0.15f, accentColor.G * 0.15f, accentColor.B * 0.15f, 0.9f);
        btn.AddThemeStyleboxOverride("pressed", pressed);

        btn.AddThemeColorOverride("font_color", accentColor);
        btn.AddThemeColorOverride("font_hover_color", accentColor.Lightened(0.2f));
    }

    private static void ApplySliderStyle(HSlider slider)
    {
        // Grabber (thumb)
        var grabber = new StyleBoxFlat();
        grabber.BgColor = SliderThumbDefault;
        grabber.CornerRadiusBottomLeft = 3;
        grabber.CornerRadiusBottomRight = 3;
        grabber.CornerRadiusTopLeft = 3;
        grabber.CornerRadiusTopRight = 3;
        grabber.ContentMarginLeft = 6;
        grabber.ContentMarginRight = 6;
        grabber.ContentMarginTop = 6;
        grabber.ContentMarginBottom = 6;
        slider.AddThemeStyleboxOverride("grabber_area", grabber);
        slider.AddThemeStyleboxOverride("grabber_area_highlight", grabber);

        // Track (slider_area)
        var track = new StyleBoxFlat();
        track.BgColor = TrackDark;
        track.CornerRadiusBottomLeft = 2;
        track.CornerRadiusBottomRight = 2;
        track.CornerRadiusTopLeft = 2;
        track.CornerRadiusTopRight = 2;
        track.ContentMarginTop = 3;
        track.ContentMarginBottom = 3;
        slider.AddThemeStyleboxOverride("slider", track);

        // Grabber icon — use a flat colored box via theme icon override is
        // not straightforward, so we rely on the grabber_area style above
        // and use the built-in grabber sizing via constants.
        slider.AddThemeConstantOverride("grabber_offset", 0);
        slider.AddThemeConstantOverride("center_grabber", 0);
    }

    private static void ApplySliderThumbColor(HSlider slider, Color color)
    {
        var grabber = new StyleBoxFlat();
        grabber.BgColor = color;
        grabber.CornerRadiusBottomLeft = 3;
        grabber.CornerRadiusBottomRight = 3;
        grabber.CornerRadiusTopLeft = 3;
        grabber.CornerRadiusTopRight = 3;
        grabber.ContentMarginLeft = 6;
        grabber.ContentMarginRight = 6;
        grabber.ContentMarginTop = 6;
        grabber.ContentMarginBottom = 6;
        slider.AddThemeStyleboxOverride("grabber_area", grabber);

        var grabberHl = (StyleBoxFlat)grabber.Duplicate();
        grabberHl.BgColor = color.Lightened(0.15f);
        slider.AddThemeStyleboxOverride("grabber_area_highlight", grabberHl);
    }
}
