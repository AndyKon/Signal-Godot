using Godot;
using Signal.Core;

namespace Signal.Minigame;

/// <summary>
/// Standalone test harness with debug config panel.
/// Tab toggles the config panel. Adjust parameters and hit Apply.
/// Presets available via F1-F6. Space = new puzzle with same config.
/// </summary>
public partial class DecryptionTestHarness : Control
{
    private DecryptionPuzzleUI _puzzleUI;
    private Label _resultsLabel;
    private int _completedCount;
    private float _totalTime;
    private int _totalGuesses;

    // ── Debug config ──────────────────────────────────────────────────────────
    private PanelContainer _configPanel;
    private bool _configVisible = true;
    private SpinBox _spSlots, _spValues, _spMaxLies;
    private SpinBox _spReplayChance, _spReplayMax, _spTellDelay, _spHistoryLimit;
    private CheckButton _cbRepeats, _cbFeedbackLies, _cbValueLies;
    private Label _configLabel;

    // Current config
    private int _cfgSlots = 4, _cfgValues = 6, _cfgMaxLies = 1;
    private int _cfgReplayMax = 0;
    private float _cfgReplayChance = 0f;
    private bool _cfgRepeats = true, _cfgFeedbackLies = true, _cfgValueLies = true;

    public override void _Ready()
    {
        var bg = new ColorRect();
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bg.Color = new Color(0.0f, 0.0f, 0.02f);
        AddChild(bg);

        // Main layout: config panel on left, puzzle on right
        var hbox = new HBoxContainer();
        hbox.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        hbox.AddThemeConstantOverride("separation", 0);
        AddChild(hbox);

        // ── Config panel ──────────────────────────────────────────────────────
        BuildConfigPanel(hbox);

        // ── Puzzle area ───────────────────────────────────────────────────────
        var puzzleArea = new VBoxContainer();
        puzzleArea.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        puzzleArea.AddThemeConstantOverride("separation", 4);
        hbox.AddChild(puzzleArea);

        _puzzleUI = new DecryptionPuzzleUI();
        _puzzleUI.SizeFlagsVertical = SizeFlags.ExpandFill;
        _puzzleUI.PuzzleCompleted += OnPuzzleCompleted;
        _puzzleUI.PuzzleCancelled += OnPuzzleCancelled;
        puzzleArea.AddChild(_puzzleUI);

        _resultsLabel = new Label();
        _resultsLabel.AddThemeFontSizeOverride("font_size", 14);
        _resultsLabel.AddThemeColorOverride("font_color", new Color(0.0f, 0.6f, 0.3f));
        _resultsLabel.Text = "  Completed: 0 | Avg guesses: -- | Avg time: --";
        puzzleArea.AddChild(_resultsLabel);

        // Check for --section N command line arg
        var args = OS.GetCmdlineUserArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--section" && int.TryParse(args[i + 1], out int sec) && sec >= 1 && sec <= 6)
                ApplyPreset(sec);
        }

        CallDeferred(MethodName.StartPuzzle);
        GameLog.Event("Test", "Decryption test harness loaded");
    }

    private void BuildConfigPanel(HBoxContainer parent)
    {
        _configPanel = new PanelContainer();
        _configPanel.CustomMinimumSize = new Vector2(280, 0);
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.02f, 0.04f, 0.03f);
        panelStyle.BorderColor = new Color(0.0f, 0.5f, 0.25f, 0.5f);
        panelStyle.BorderWidthRight = 1;
        panelStyle.ContentMarginLeft = 16;
        panelStyle.ContentMarginRight = 16;
        panelStyle.ContentMarginTop = 16;
        panelStyle.ContentMarginBottom = 16;
        _configPanel.AddThemeStyleboxOverride("panel", panelStyle);
        parent.AddChild(_configPanel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 10);
        _configPanel.AddChild(vbox);

        // Title
        var title = new Label { Text = "PUZZLE CONFIG" };
        title.AddThemeFontSizeOverride("font_size", 16);
        title.AddThemeColorOverride("font_color", new Color(0.0f, 0.9f, 0.4f));
        vbox.AddChild(title);

        // Config summary
        _configLabel = new Label();
        _configLabel.AddThemeFontSizeOverride("font_size", 12);
        _configLabel.AddThemeColorOverride("font_color", new Color(0.0f, 0.5f, 0.25f));
        _configLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        vbox.AddChild(_configLabel);

        // Spinboxes
        _spSlots = AddSpinRow(vbox, "Slots", 2, 8, _cfgSlots);
        _spValues = AddSpinRow(vbox, "Values", 2, 8, _cfgValues);
        _cbRepeats = AddCheckRow(vbox, "Allow Repeats", _cfgRepeats);
        _spMaxLies = AddSpinRow(vbox, "Max Lies/Round", 0, 4, _cfgMaxLies);
        _cbFeedbackLies = AddCheckRow(vbox, "Feedback Lies", _cfgFeedbackLies);
        _cbValueLies = AddCheckRow(vbox, "Value Swap Lies", _cfgValueLies);
        _spTellDelay = AddSpinRow(vbox, "Tell Delay (ms)", 100, 2000, 800, 100);
        _spHistoryLimit = AddSpinRow(vbox, "History Limit", 0, 20, 0);
        _spReplayChance = AddSpinRow(vbox, "Replay Chance %", 0, 100, (int)(_cfgReplayChance * 100), 10);
        _spReplayMax = AddSpinRow(vbox, "Replay Max/Cycle", 0, 4, _cfgReplayMax);

        // Apply button
        var applyBtn = new Button { Text = "[ APPLY & RESET ]" };
        applyBtn.CustomMinimumSize = new Vector2(0, 40);
        applyBtn.AddThemeFontSizeOverride("font_size", 14);
        applyBtn.AddThemeColorOverride("font_color", new Color(0.0f, 0.9f, 0.4f));
        var applyStyle = new StyleBoxFlat();
        applyStyle.BgColor = new Color(0.0f, 0.1f, 0.05f);
        applyStyle.BorderColor = new Color(0.0f, 0.7f, 0.3f);
        applyStyle.SetBorderWidthAll(1);
        applyBtn.AddThemeStyleboxOverride("normal", applyStyle);
        var applyHover = new StyleBoxFlat();
        applyHover.BgColor = new Color(0.0f, 0.2f, 0.1f);
        applyHover.BorderColor = new Color(0.0f, 1.0f, 0.4f);
        applyHover.SetBorderWidthAll(1);
        applyBtn.AddThemeStyleboxOverride("hover", applyHover);
        applyBtn.Pressed += OnApply;
        vbox.AddChild(applyBtn);

        // Separator
        var sep = new ColorRect { CustomMinimumSize = new Vector2(0, 1) };
        sep.Color = new Color(0.0f, 0.5f, 0.25f, 0.3f);
        vbox.AddChild(sep);

        // Presets
        var presetLabel = new Label { Text = "PRESETS" };
        presetLabel.AddThemeFontSizeOverride("font_size", 14);
        presetLabel.AddThemeColorOverride("font_color", new Color(0.0f, 0.7f, 0.35f));
        vbox.AddChild(presetLabel);

        AddPresetButton(vbox, "S1: Baseline", 1);
        AddPresetButton(vbox, "S2: + Repeats", 2);
        AddPresetButton(vbox, "S3: + Value Swaps", 3);
        AddPresetButton(vbox, "S4: + Feedback Lies", 4);
        AddPresetButton(vbox, "S5H: Full Hostile", 5);
        AddPresetButton(vbox, "S5C: Cooperative", 6);

        // Hints
        var hints = new Label { Text = "Tab = toggle panel\nSpace = new puzzle\nEsc = quit" };
        hints.AddThemeFontSizeOverride("font_size", 11);
        hints.AddThemeColorOverride("font_color", new Color(0.0f, 0.35f, 0.18f));
        vbox.AddChild(hints);

        UpdateConfigLabel();
    }

    private SpinBox AddSpinRow(VBoxContainer parent, string label, int min, int max, int value, int step = 1)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        parent.AddChild(row);

        var lbl = new Label { Text = label };
        lbl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        lbl.AddThemeFontSizeOverride("font_size", 13);
        lbl.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
        row.AddChild(lbl);

        var spin = new SpinBox();
        spin.MinValue = min;
        spin.MaxValue = max;
        spin.Value = value;
        spin.Step = step;
        spin.CustomMinimumSize = new Vector2(70, 0);
        spin.AddThemeFontSizeOverride("font_size", 13);
        // Prevent SpinBox from stealing keyboard focus (Tab/Space/Enter)
        spin.FocusMode = FocusModeEnum.Click;
        spin.GetLineEdit().FocusMode = FocusModeEnum.Click;
        row.AddChild(spin);

        return spin;
    }

    private CheckButton AddCheckRow(VBoxContainer parent, string label, bool value)
    {
        var cb = new CheckButton { Text = label, ButtonPressed = value };
        cb.AddThemeColorOverride("font_color", new Color(0.7f, 0.7f, 0.8f));
        cb.AddThemeFontSizeOverride("font_size", 13);
        cb.FocusMode = FocusModeEnum.Click;
        parent.AddChild(cb);
        return cb;
    }

    private void AddPresetButton(VBoxContainer parent, string text, int section)
    {
        var btn = new Button { Text = text };
        btn.AddThemeFontSizeOverride("font_size", 12);
        btn.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 0.7f));
        btn.Alignment = HorizontalAlignment.Left;
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.02f, 0.04f, 0.03f);
        style.BorderColor = new Color(0.0f, 0.4f, 0.2f, 0.3f);
        style.SetBorderWidthAll(1);
        btn.AddThemeStyleboxOverride("normal", style);
        var hover = new StyleBoxFlat();
        hover.BgColor = new Color(0.0f, 0.12f, 0.06f);
        hover.BorderColor = new Color(0.0f, 0.7f, 0.3f);
        hover.SetBorderWidthAll(1);
        btn.AddThemeStyleboxOverride("hover", hover);
        btn.Pressed += () => ApplyPreset(section);
        parent.AddChild(btn);
    }

    private void ApplyPreset(int section)
    {
        // (slots, values, repeats, maxLies, fbEnabled, valEnabled, replayChance, replayMax)
        var p = section switch
        {
            1 => (4, 6, false, 0, false, false, 0f, 0),
            2 => (4, 6, true,  0, false, false, 0f, 0),
            3 => (4, 6, true,  1, false, true,  0f, 0),   // value-swap only
            4 => (4, 6, true,  1, true,  false, 0f, 0),   // feedback only
            5 => (6, 8, true,  2, true,  true,  0.8f, 2), // everything
            6 => (4, 6, false, 0, false, false, 0f, 0),   // cooperative
            _ => (4, 6, false, 0, false, false, 0f, 0)
        };

        _spSlots.Value = p.Item1;
        _spValues.Value = p.Item2;
        _cbRepeats.ButtonPressed = p.Item3;
        _spMaxLies.Value = p.Item4;
        _cbFeedbackLies.ButtonPressed = p.Item5;
        _cbValueLies.ButtonPressed = p.Item6;
        _spReplayChance.Value = (int)(p.Item7 * 100);
        _spReplayMax.Value = p.Item8;

        OnApply();
    }

    private void OnApply()
    {
        _cfgSlots = (int)_spSlots.Value;
        _cfgValues = (int)_spValues.Value;
        _cfgRepeats = _cbRepeats.ButtonPressed;
        _cfgMaxLies = (int)_spMaxLies.Value;
        _cfgFeedbackLies = _cbFeedbackLies.ButtonPressed;
        _cfgValueLies = _cbValueLies.ButtonPressed;
        _cfgReplayChance = (float)_spReplayChance.Value / 100f;
        _cfgReplayMax = (int)_spReplayMax.Value;

        _completedCount = 0;
        _totalTime = 0;
        _totalGuesses = 0;
        _resultsLabel.Text = "  Completed: 0 | Avg guesses: -- | Avg time: --";

        _puzzleUI.SetTellDelay((float)_spTellDelay.Value / 1000f);
        _puzzleUI.SetMaxVisibleHistory((int)_spHistoryLimit.Value);

        UpdateConfigLabel();
        StartPuzzle();

        string config = $"{_cfgSlots}s/{_cfgValues}v rep={_cfgRepeats} lies={_cfgMaxLies} fb={_cfgFeedbackLies} val={_cfgValueLies} tell={_spTellDelay.Value}ms RC={_cfgReplayChance:F0} RM={_cfgReplayMax}";
        GameLog.Event("Test", $"Config applied: {config}");
    }

    private void UpdateConfigLabel()
    {
        var types = new System.Collections.Generic.List<string>();
        if (_cfgFeedbackLies) types.Add("feedback");
        if (_cfgValueLies) types.Add("value-swap");
        string lieTypes = types.Count > 0 ? string.Join(" + ", types) : "none";

        _configLabel.Text = $"{_cfgSlots} slots, {_cfgValues} values" +
            $"\nRepeats: {(_cfgRepeats ? "yes" : "no")}" +
            $"\nMax lies/round: {_cfgMaxLies} ({lieTypes})" +
            $"\nReplay: {_cfgReplayChance * 100:F0}% chance, max {_cfgReplayMax}";
    }

    private void StartPuzzle()
    {
        _puzzleUI.StartCustomPuzzle(_cfgSlots, _cfgValues, _cfgRepeats,
            _cfgMaxLies, _cfgFeedbackLies, _cfgValueLies, _cfgReplayChance, _cfgReplayMax);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed) return;

        switch (key.Keycode)
        {
            case Key.Tab:
                _configVisible = !_configVisible;
                _configPanel.Visible = _configVisible;
                GetViewport().SetInputAsHandled();
                break;
            case Key.Space:
                StartPuzzle();
                GetViewport().SetInputAsHandled();
                break;
            case Key.Escape:
                GetTree().Quit();
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    private void OnPuzzleCompleted(int guessCount, float timeSpent)
    {
        _completedCount++;
        _totalTime += timeSpent;
        _totalGuesses += guessCount;
        float avgTime = _totalTime / _completedCount;
        float avgGuesses = (float)_totalGuesses / _completedCount;
        _resultsLabel.Text = $"  Completed: {_completedCount} | Last: {guessCount} guesses in {timeSpent:F1}s | Avg: {avgGuesses:F1} guesses, {avgTime:F1}s";

        string config = $"{_cfgSlots}s/{_cfgValues}v FL={_cfgFeedbackLies} VL={_cfgValueLies}";
        GameLog.Event("Test", $"[{config}] Solved: {guessCount} guesses, {timeSpent:F1}s (avg: {avgGuesses:F1} guesses, {avgTime:F1}s over {_completedCount})");

        GetTree().CreateTimer(2.0).Timeout += () => StartPuzzle();
    }

    private void OnPuzzleCancelled()
    {
        StartPuzzle();
    }
}
