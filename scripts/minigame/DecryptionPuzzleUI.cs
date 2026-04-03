using Godot;
using System;
using System.Collections.Generic;

namespace Signal.Minigame;

/// <summary>
/// Visual interface for the Mastermind-style decryption puzzle.
/// All UI is built programmatically — no scene editor required.
/// </summary>
public partial class DecryptionPuzzleUI : Control
{
    [Signal] public delegate void PuzzleCompletedEventHandler(int guessCount, float timeSpent);
    [Signal] public delegate void PuzzleCancelledEventHandler();

    // ── Layout constants ──────────────────────────────────────────────────────
    private const int SlotSize = 76;
    private const int SlotGap = 12;
    private const int FeedbackDotSize = 20;
    private const int TerminalMaxWidth = 1100;
    private const int TerminalPadding = 48;

    // ── Terminal colour palette ───────────────────────────────────────────────
    private static readonly Color ColorTermBg     = new Color(0.0f,  0.0f,  0.02f);   // near-black
    private static readonly Color ColorTermPanel  = new Color(0.02f, 0.04f, 0.03f);   // very dark green-black
    private static readonly Color ColorTermBorder = new Color(0.0f,  0.7f,  0.3f, 0.4f); // dim green border
    private static readonly Color ColorTermText   = new Color(0.0f,  0.9f,  0.4f);    // neon green
    private static readonly Color ColorTermDim    = new Color(0.0f,  0.45f, 0.2f);    // dim green

    // ── Hex value theme — medium tints, dark text readable on them ───────────
    private static readonly string[] HexLabels = { "0a", "3f", "b2", "e7", "1c", "d4", "8f", "5b" };
    private static readonly Color[] HexTints =
    {
        new Color(0.1f,  0.6f,  0.85f), // 0a — cyan
        new Color(0.1f,  0.75f, 0.55f), // 3f — teal
        new Color(0.2f,  0.7f,  0.2f),  // b2 — green
        new Color(0.75f, 0.7f,  0.1f),  // e7 — yellow
        new Color(0.85f, 0.5f,  0.1f),  // 1c — orange
        new Color(0.25f, 0.4f,  0.8f),  // d4 — blue
        new Color(0.55f, 0.3f,  0.75f), // 8f — purple
        new Color(0.8f,  0.25f, 0.55f), // 5b — magenta
    };
    // Text on hex tints uses black for legibility
    private static readonly Color ColorHexText = new Color(0.0f, 0.0f, 0.0f);

    // ── Feedback colours — vivid, unmistakable against terminal black ─────────
    private static readonly Color ColorCorrect    = new Color(0.0f,  1.0f,  0.4f);    // vivid green
    private static readonly Color ColorWrongPos   = new Color(1.0f,  0.8f,  0.0f);    // vivid yellow
    private static readonly Color ColorNotPresent = new Color(0.9f,  0.15f, 0.1f);    // vivid red
    private static readonly Color ColorPending    = new Color(0.06f, 0.08f, 0.06f);   // barely visible
    private static readonly Color ColorSlotEmpty  = new Color(0.04f, 0.06f, 0.04f);   // near-black green

    // ── Core state ────────────────────────────────────────────────────────────
    private DecryptionPuzzle _puzzle;
    private int _section;
    private bool _active;
    private float _elapsed;

    // ── Current input ─────────────────────────────────────────────────────────
    private int[] _currentInput; // -1 = empty
    private int _inputFilled;

    // ── UI refs ───────────────────────────────────────────────────────────────
    private MarginContainer _uiRoot;   // top-level UI container rebuilt on each StartPuzzle
    private Label _titleLabel;
    private Label _timerLabel;
    private VBoxContainer _historyVBox;
    private ScrollContainer _historyScroll;
    private Label _statusLabel;
    private Control[] _inputSlotContainers;   // the N input slots
    private ColorRect[] _inputSlotBgs;
    private Label[] _inputSlotLabels;
    private Button _submitButton;

    // ── History visuals ───────────────────────────────────────────────────────
    // Per history row: slot BGs + feedback dot BGs
    private List<ColorRect[]> _historySlotBgs   = new();
    private List<Label[]>     _historySlotLabels = new();
    private List<ColorRect[]> _historyDotBgs     = new();

    // ── Animation state machine ───────────────────────────────────────────────
    private enum AnimState { Idle, Replaying, ShowingNew }
    private AnimState _animState = AnimState.Idle;

    private ReplayResult[] _replayResults;   // set by PrepareReplay before animation
    private float _animTimer;
    private int _animRow;    // which history row is being revealed
    private int _animSlot;   // which slot within that row

    private const float PerSlotDelay    = 0.12f;
    private const float BetweenRowPause = 0.30f;
    private const float NewSlotDelay    = 0.15f;

    // ─────────────────────────────────────────────────────────────────────────
    // Godot lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        var bg = new ColorRect();
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bg.Color = ColorTermBg;
        AddChild(bg);
    }

    public override void _Process(double delta)
    {
        if (!_active) return;

        _elapsed += (float)delta;
        UpdateTimerLabel();
        TickAnimation((float)delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_active || _animState != AnimState.Idle) return;
        if (@event is not InputEventKey key || !key.Pressed) return;

        // 1–8 → values 0–7
        int valueIndex = key.Keycode switch
        {
            Key.Key1 => 0, Key.Key2 => 1, Key.Key3 => 2, Key.Key4 => 3,
            Key.Key5 => 4, Key.Key6 => 5, Key.Key7 => 6, Key.Key8 => 7,
            _ => -1
        };

        if (valueIndex >= 0 && valueIndex < _puzzle.ValueCount)
        {
            AppendInputValue(valueIndex);
            GetViewport().SetInputAsHandled();
            return;
        }

        switch (key.Keycode)
        {
            case Key.Backspace:
                RemoveLastInputValue();
                GetViewport().SetInputAsHandled();
                break;
            case Key.Enter:
            case Key.KpEnter:
                TrySubmit();
                GetViewport().SetInputAsHandled();
                break;
            case Key.Escape:
                OnCancel();
                GetViewport().SetInputAsHandled();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    public void StartPuzzle(int section, int seed = -1)
    {
        if (seed < 0) seed = (int)(Time.GetTicksMsec() % int.MaxValue);

        _puzzle = section switch
        {
            1 => DecryptionPuzzle.CreateSection1(seed),
            2 => DecryptionPuzzle.CreateSection2(seed),
            3 => DecryptionPuzzle.CreateSection3(seed),
            4 => DecryptionPuzzle.CreateSection4(seed),
            5 => DecryptionPuzzle.CreateSection5Hostile(seed),
            6 => DecryptionPuzzle.CreateSection5Cooperative(seed),
            _ => DecryptionPuzzle.CreateSection1(seed)
        };

        _section = section;
        _elapsed = 0f;
        _active = true;
        _animState = AnimState.Idle;
        _historySlotBgs.Clear();
        _historySlotLabels.Clear();
        _historyDotBgs.Clear();

        BuildUI(section);
        ResetCurrentInput();
        SetStatus("Enter your guess.");
    }

    /// <summary>Start a puzzle with fully custom parameters.</summary>
    public void StartCustomPuzzle(int slots, int values, bool repeats,
        int maxLies, bool feedbackLies, bool valueLies,
        float replayChance, int replayMax, int seed = -1)
    {
        if (seed < 0) seed = (int)(Time.GetTicksMsec() % int.MaxValue);

        _puzzle = new DecryptionPuzzle(slots, values, repeats, maxLies,
            feedbackLies, valueLies, replayChance, replayMax, seed);

        _section = 0; // custom
        _elapsed = 0f;
        _active = true;
        _animState = AnimState.Idle;
        _historySlotBgs.Clear();
        _historySlotLabels.Clear();
        _historyDotBgs.Clear();

        BuildUI(0);
        ResetCurrentInput();
        SetStatus("Enter your guess.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UI construction
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildUI(int section)
    {
        // Tear down previous UI root (if any) and rebuild from scratch
        _uiRoot?.QueueFree();

        // ── Centering wrapper — terminal panel centered on screen ─────────────
        var outerMargin = new MarginContainer();
        _uiRoot = outerMargin;
        outerMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        outerMargin.AddThemeConstantOverride("margin_top", 24);
        outerMargin.AddThemeConstantOverride("margin_bottom", 24);
        outerMargin.AddThemeConstantOverride("margin_left", 0);
        outerMargin.AddThemeConstantOverride("margin_right", 0);
        AddChild(outerMargin);

        // Horizontal centering: spacer | terminal | spacer
        var hCenter = new HBoxContainer();
        outerMargin.AddChild(hCenter);

        var leftSpacer = new Control();
        leftSpacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hCenter.AddChild(leftSpacer);

        // ── Terminal panel with green border ───────────────────────────────────
        var terminalPanel = new PanelContainer();
        terminalPanel.CustomMinimumSize = new Vector2(TerminalMaxWidth, 0);
        terminalPanel.SizeFlagsVertical = SizeFlags.ExpandFill;
        var termStyle = new StyleBoxFlat();
        termStyle.BgColor = ColorTermPanel;
        termStyle.BorderColor = ColorTermBorder;
        termStyle.SetBorderWidthAll(2);
        termStyle.SetCornerRadiusAll(2);
        termStyle.ContentMarginLeft   = TerminalPadding;
        termStyle.ContentMarginRight  = TerminalPadding;
        termStyle.ContentMarginTop    = 32;
        termStyle.ContentMarginBottom = 32;
        terminalPanel.AddThemeStyleboxOverride("panel", termStyle);
        hCenter.AddChild(terminalPanel);

        var rightSpacer = new Control();
        rightSpacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hCenter.AddChild(rightSpacer);

        // ── Content inside terminal ───────────────────────────────────────────
        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 12);
        terminalPanel.AddChild(root);

        // ── 1. Title bar ──────────────────────────────────────────────────────
        var titleBar = new HBoxContainer();
        titleBar.AddThemeConstantOverride("separation", 16);
        root.AddChild(titleBar);

        _titleLabel = new Label();
        _titleLabel.Text = section > 0
            ? $"> DECRYPTION TERMINAL // SECTION {section}"
            : $"> DECRYPTION TERMINAL // {_puzzle.SlotCount}s {_puzzle.ValueCount}v L{_puzzle.MaxLiesPerRound} fb={_puzzle.FeedbackLiesEnabled} val={_puzzle.ValueLiesEnabled}";
        _titleLabel.AddThemeFontSizeOverride("font_size", 22);
        _titleLabel.AddThemeColorOverride("font_color", ColorTermText);
        titleBar.AddChild(_titleLabel);

        var titleSpacer2 = new Control();
        titleSpacer2.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleBar.AddChild(titleSpacer2);

        _timerLabel = new Label();
        _timerLabel.Text = "0.0s";
        _timerLabel.AddThemeFontSizeOverride("font_size", 20);
        _timerLabel.AddThemeColorOverride("font_color", ColorTermDim);
        titleBar.AddChild(_timerLabel);

        // ── 2. Separator — terminal-style line ────────────────────────────────
        var sepLine = new ColorRect();
        sepLine.CustomMinimumSize = new Vector2(0, 1);
        sepLine.Color = ColorTermBorder;
        root.AddChild(sepLine);

        // ── 3. History scroll ─────────────────────────────────────────────────
        _historyScroll = new ScrollContainer();
        _historyScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _historyScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        root.AddChild(_historyScroll);

        _historyVBox = new VBoxContainer();
        _historyVBox.AddThemeConstantOverride("separation", 8);
        _historyVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _historyScroll.AddChild(_historyVBox);

        // ── 4. Status label ───────────────────────────────────────────────────
        _statusLabel = new Label();
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        _statusLabel.AddThemeFontSizeOverride("font_size", 18);
        _statusLabel.AddThemeColorOverride("font_color", ColorTermDim);
        root.AddChild(_statusLabel);

        // ── 5. Current guess input ────────────────────────────────────────────
        var inputCenter = new CenterContainer();
        root.AddChild(inputCenter);
        var inputRow = new HBoxContainer();
        inputRow.AddThemeConstantOverride("separation", SlotGap);
        inputCenter.AddChild(inputRow);

        _inputSlotContainers = new Control[_puzzle.SlotCount];
        _inputSlotBgs        = new ColorRect[_puzzle.SlotCount];
        _inputSlotLabels     = new Label[_puzzle.SlotCount];

        for (int i = 0; i < _puzzle.SlotCount; i++)
        {
            int slotIndex = i;
            var container = new Control();
            container.CustomMinimumSize = new Vector2(SlotSize, SlotSize);

            // Border (terminal green, dim)
            var slotBorder = new ColorRect();
            slotBorder.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            slotBorder.Color = ColorTermBorder;
            container.AddChild(slotBorder);

            // Inner fill
            var slotInner = new ColorRect();
            slotInner.SetAnchorsPreset(LayoutPreset.FullRect);
            slotInner.OffsetLeft   =  2;
            slotInner.OffsetTop    =  2;
            slotInner.OffsetRight  = -2;
            slotInner.OffsetBottom = -2;
            slotInner.Color = ColorSlotEmpty;
            container.AddChild(slotInner);

            var lbl = new Label();
            lbl.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            lbl.VerticalAlignment   = VerticalAlignment.Center;
            lbl.AddThemeFontSizeOverride("font_size", 22);
            lbl.AddThemeColorOverride("font_color", ColorHexText);
            container.AddChild(lbl);

            var btn = new Button();
            btn.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            btn.Flat = true;
            btn.Modulate = Colors.Transparent;
            btn.Pressed += () => ClearInputSlot(slotIndex);
            container.AddChild(btn);

            _inputSlotContainers[i] = container;
            _inputSlotBgs[i]        = slotInner;
            _inputSlotLabels[i]     = lbl;
            inputRow.AddChild(container);
        }

        var inputSpacer2 = new Control();
        inputSpacer2.CustomMinimumSize = new Vector2(16, 0);
        inputRow.AddChild(inputSpacer2);

        _submitButton = new Button();
        _submitButton.Text = "[ SUBMIT ]";
        _submitButton.CustomMinimumSize = new Vector2(120, SlotSize);
        _submitButton.AddThemeFontSizeOverride("font_size", 18);
        _submitButton.AddThemeColorOverride("font_color", ColorTermText);
        var submitStyle = new StyleBoxFlat();
        submitStyle.BgColor = ColorTermPanel;
        submitStyle.BorderColor = ColorTermBorder;
        submitStyle.SetBorderWidthAll(1);
        submitStyle.SetCornerRadiusAll(0);
        _submitButton.AddThemeStyleboxOverride("normal", submitStyle);
        var submitHover = new StyleBoxFlat();
        submitHover.BgColor = new Color(0.0f, 0.15f, 0.05f);
        submitHover.BorderColor = ColorTermText;
        submitHover.SetBorderWidthAll(1);
        _submitButton.AddThemeStyleboxOverride("hover", submitHover);
        _submitButton.Pressed += TrySubmit;
        inputRow.AddChild(_submitButton);

        // ── 6. Value picker ───────────────────────────────────────────────────
        var pickerSep = new ColorRect();
        pickerSep.CustomMinimumSize = new Vector2(0, 1);
        pickerSep.Color = ColorTermBorder;
        root.AddChild(pickerSep);

        var pickerCenter = new CenterContainer();
        root.AddChild(pickerCenter);
        var pickerRow = new HBoxContainer();
        pickerRow.AddThemeConstantOverride("separation", 8);
        pickerCenter.AddChild(pickerRow);

        for (int v = 0; v < _puzzle.ValueCount; v++)
        {
            int valueIndex = v;
            var btn = new Button();
            btn.Text = HexLabels[v];
            btn.CustomMinimumSize = new Vector2(SlotSize, SlotSize);
            btn.AddThemeFontSizeOverride("font_size", 20);

            var style = new StyleBoxFlat();
            style.BgColor = HexTints[v].Darkened(0.7f);
            style.BorderColor = HexTints[v].Darkened(0.2f);
            style.SetBorderWidthAll(2);
            style.SetCornerRadiusAll(0);
            btn.AddThemeStyleboxOverride("normal", style);
            btn.AddThemeColorOverride("font_color", HexTints[v].Lightened(0.4f));

            var styleHover = (StyleBoxFlat)style.Duplicate();
            styleHover.BgColor = HexTints[v].Darkened(0.5f);
            styleHover.BorderColor = HexTints[v];
            btn.AddThemeStyleboxOverride("hover", styleHover);

            var stylePressed = (StyleBoxFlat)style.Duplicate();
            stylePressed.BgColor = HexTints[v].Darkened(0.3f);
            btn.AddThemeStyleboxOverride("pressed", stylePressed);

            btn.Pressed += () => AppendInputValue(valueIndex);
            pickerRow.AddChild(btn);
        }

        var bksp = new Button();
        bksp.Text = "⌫";
        bksp.CustomMinimumSize = new Vector2(SlotSize, SlotSize);
        bksp.AddThemeFontSizeOverride("font_size", 26);
        bksp.AddThemeColorOverride("font_color", ColorTermDim);
        var bkspStyle = new StyleBoxFlat();
        bkspStyle.BgColor = ColorTermPanel;
        bkspStyle.BorderColor = ColorTermBorder;
        bkspStyle.SetBorderWidthAll(1);
        bksp.AddThemeStyleboxOverride("normal", bkspStyle);
        bksp.Pressed += RemoveLastInputValue;
        pickerRow.AddChild(bksp);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Input helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void ResetCurrentInput()
    {
        _currentInput = new int[_puzzle.SlotCount];
        for (int i = 0; i < _puzzle.SlotCount; i++) _currentInput[i] = -1;
        _inputFilled = 0;
        RefreshInputDisplay();
    }

    private void AppendInputValue(int value)
    {
        if (_inputFilled >= _puzzle.SlotCount) return;
        _currentInput[_inputFilled] = value;
        _inputFilled++;
        RefreshInputDisplay();
    }

    private void RemoveLastInputValue()
    {
        if (_inputFilled == 0) return;
        _inputFilled--;
        _currentInput[_inputFilled] = -1;
        RefreshInputDisplay();
    }

    private void ClearInputSlot(int slotIndex)
    {
        if (_currentInput[slotIndex] < 0) return;
        // Shift remaining values left
        for (int i = slotIndex; i < _inputFilled - 1; i++)
            _currentInput[i] = _currentInput[i + 1];
        _inputFilled--;
        _currentInput[_inputFilled] = -1;
        RefreshInputDisplay();
    }

    private void RefreshInputDisplay()
    {
        for (int i = 0; i < _puzzle.SlotCount; i++)
        {
            int v = _currentInput[i];
            if (v < 0)
            {
                _inputSlotBgs[i].Color    = ColorSlotEmpty;
                _inputSlotLabels[i].Text  = "";
            }
            else
            {
                _inputSlotBgs[i].Color   = HexTints[v];
                _inputSlotLabels[i].Text = HexLabels[v];
            }
        }

        bool ready = _inputFilled == _puzzle.SlotCount;
        _submitButton.Disabled = !ready;
        SetStatus(ready ? "Press SUBMIT or Enter to guess." : "Fill all slots to submit.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Submission
    // ─────────────────────────────────────────────────────────────────────────

    private void TrySubmit()
    {
        if (!_active || _animState != AnimState.Idle) return;
        if (_inputFilled < _puzzle.SlotCount) return;

        bool isFirstGuess = _puzzle.GuessesMade == 0;

        // Collect replay data before submitting (uses history as it is now)
        ReplayResult[] replayResults = null;
        if (!isFirstGuess)
            replayResults = _puzzle.PrepareReplay();

        var result = _puzzle.SubmitGuess((int[])_currentInput.Clone());
        if (result == null) return;

        // Add new history row (all pending)
        AddHistoryRow(_puzzle.GuessesMade - 1, result.DisplayGuess);

        // Reset input
        ResetCurrentInput();

        if (isFirstGuess)
        {
            // Skip replay, go straight to ShowingNew
            _animState = AnimState.ShowingNew;
            _animRow   = _puzzle.GuessesMade - 1;
            _animSlot  = 0;
            _animTimer = NewSlotDelay;
        }
        else
        {
            // Reset all existing history rows (except the newly added one) to pending
            int rowCount = _historySlotBgs.Count;
            for (int r = 0; r < rowCount - 1; r++)
                ResetRowToPending(r);

            _replayResults = replayResults;
            _animState     = AnimState.Replaying;
            _animRow       = 0;
            _animSlot      = 0;
            _animTimer     = PerSlotDelay;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // History row construction
    // ─────────────────────────────────────────────────────────────────────────

    private void AddHistoryRow(int rowIndex, int[] guess)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", SlotGap);
        _historyVBox.AddChild(row);

        // Guess number
        var numLabel = new Label();
        numLabel.Text = $"{rowIndex + 1:D2}";
        numLabel.CustomMinimumSize = new Vector2(40, SlotSize);
        numLabel.VerticalAlignment = VerticalAlignment.Center;
        numLabel.AddThemeFontSizeOverride("font_size", 18);
        numLabel.AddThemeColorOverride("font_color", ColorTermDim);
        row.AddChild(numLabel);

        // Guess slots
        var slotBgs    = new ColorRect[_puzzle.SlotCount];
        var slotLabels = new Label[_puzzle.SlotCount];

        for (int i = 0; i < _puzzle.SlotCount; i++)
        {
            var container = new Control();
            container.CustomMinimumSize = new Vector2(SlotSize, SlotSize);

            var bg = new ColorRect();
            bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            bg.Color = ColorPending;
            container.AddChild(bg);

            var inner = new ColorRect();
            inner.SetAnchorsPreset(LayoutPreset.FullRect);
            inner.OffsetLeft   =  6;
            inner.OffsetTop    =  6;
            inner.OffsetRight  = -6;
            inner.OffsetBottom = -6;
            inner.Color = HexTints[guess[i]];
            container.AddChild(inner);

            var lbl = new Label();
            lbl.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            lbl.VerticalAlignment   = VerticalAlignment.Center;
            lbl.Text = HexLabels[guess[i]];
            lbl.AddThemeFontSizeOverride("font_size", 20);
            lbl.AddThemeColorOverride("font_color", ColorHexText);
            container.AddChild(lbl);

            slotBgs[i]    = bg;
            slotLabels[i] = lbl;
            row.AddChild(container);
        }

        // Gap between guess and feedback
        var midGap = new Control();
        midGap.CustomMinimumSize = new Vector2(8, 0);
        row.AddChild(midGap);

        // Feedback dots (pending)
        var dotBgs = new ColorRect[_puzzle.SlotCount];
        int cols = (_puzzle.SlotCount + 1) / 2;

        var dotGrid = new GridContainer();
        dotGrid.Columns = cols;
        dotGrid.AddThemeConstantOverride("h_separation", 4);
        dotGrid.AddThemeConstantOverride("v_separation", 4);
        row.AddChild(dotGrid);

        for (int i = 0; i < _puzzle.SlotCount; i++)
        {
            var dot = new ColorRect();
            dot.CustomMinimumSize = new Vector2(FeedbackDotSize, FeedbackDotSize);
            dot.Color = ColorPending;
            dotBgs[i] = dot;
            dotGrid.AddChild(dot);
        }

        _historySlotBgs.Add(slotBgs);
        _historySlotLabels.Add(slotLabels);
        _historyDotBgs.Add(dotBgs);

        // Auto-scroll to bottom
        CallDeferred(MethodName.ScrollHistoryToBottom);
    }

    private void ScrollHistoryToBottom()
    {
        if (_historyScroll != null)
            _historyScroll.ScrollVertical = (int)_historyScroll.GetVScrollBar().MaxValue;
    }

    private void ScrollToRow(int rowIndex)
    {
        if (_historyScroll == null || _historyVBox == null) return;
        if (rowIndex >= _historyVBox.GetChildCount()) return;
        var row = _historyVBox.GetChild(rowIndex) as Control;
        if (row == null) return;
        // Scroll so this row is visible near the top of the scroll area
        _historyScroll.ScrollVertical = (int)Mathf.Max(0, row.Position.Y - 8);
    }

    private void ResetRowToPending(int rowIndex)
    {
        if (rowIndex >= _historySlotBgs.Count) return;
        var slotBgs = _historySlotBgs[rowIndex];
        var dotBgs  = _historyDotBgs[rowIndex];

        foreach (var bg in slotBgs) bg.Color = ColorPending;
        foreach (var dot in dotBgs) dot.Color = ColorPending;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Slot reveal helper
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reveal a history slot. For honest slots: just set the color.
    /// For lied slots: show TRUTH first, pause, then glitch-overwrite to the lie.
    /// </summary>
    private void RevealHistorySlot(int rowIndex, int slotIndex, SlotFeedback feedback, bool isTell)
    {
        if (rowIndex >= _historySlotBgs.Count) return;

        var slotBg = _historySlotBgs[rowIndex][slotIndex];
        var dotBg  = _historyDotBgs[rowIndex][slotIndex];
        var feedColor = FeedbackColor(feedback);

        if (!isTell)
        {
            // Honest slot — just show the feedback
            slotBg.Color = feedColor;
            dotBg.Color  = feedColor;
            return;
        }

        // Lied slot — first show TRUTH, then glitch-overwrite to lie
        // We need the true feedback. Get it from the result.
        int histIndex = rowIndex;
        if (histIndex < _puzzle.History.Count)
        {
            var result = _puzzle.History[histIndex];
            var trueColor = FeedbackColor(result.TrueFeedback[slotIndex]);

            // Step 1: show truth (looks normal at first)
            slotBg.Color = trueColor;
            dotBg.Color  = trueColor;

            // Step 2: after pause, glitch-overwrite to lie
            PlayGlitchOverwrite(slotBg, dotBg, slotBg.GetParent<Control>(), trueColor, feedColor);
        }
        else
        {
            slotBg.Color = feedColor;
            dotBg.Color  = feedColor;
        }
    }

    /// <summary>
    /// Glitch overwrite: truth sits for 0.3s, then rapid flicker + jitter,
    /// then settles on the lie. Looks like NEREUS forcibly corrupting data.
    /// </summary>
    private void PlayGlitchOverwrite(ColorRect slotBg, ColorRect dotBg, Control container,
                                      Color truthColor, Color lieColor)
    {
        var glitchWhite = new Color(0.9f, 0.95f, 1.0f);
        var glitchDark  = new Color(0.02f, 0.02f, 0.04f);
        var originalPos = container.Position;

        var tween = CreateTween();

        // Hold truth for a moment — looks normal
        tween.TweenInterval(0.3f);

        // Glitch burst: rapid flicker between truth, static, lie
        tween.TweenProperty(slotBg, "color", glitchWhite, 0.02f);
        tween.TweenProperty(slotBg, "color", glitchDark, 0.02f);
        tween.TweenProperty(slotBg, "color", truthColor, 0.03f);
        tween.TweenProperty(slotBg, "color", glitchWhite, 0.02f);
        tween.TweenProperty(slotBg, "color", lieColor, 0.02f);
        tween.TweenProperty(slotBg, "color", glitchDark, 0.02f);
        tween.TweenProperty(slotBg, "color", lieColor, 0.03f);

        // Sync the feedback dot
        var dotTween = CreateTween();
        dotTween.TweenInterval(0.3f);
        dotTween.TweenProperty(dotBg, "color", glitchWhite, 0.02f);
        dotTween.TweenProperty(dotBg, "color", lieColor, 0.06f);

        // Position jitter (parallel)
        var shakeTween = CreateTween();
        shakeTween.TweenInterval(0.3f);
        shakeTween.TweenProperty(container, "position", originalPos + new Vector2(4, -1), 0.02f);
        shakeTween.TweenProperty(container, "position", originalPos + new Vector2(-3, 2), 0.02f);
        shakeTween.TweenProperty(container, "position", originalPos + new Vector2(2, -1), 0.02f);
        shakeTween.TweenProperty(container, "position", originalPos + new Vector2(-2, 1), 0.02f);
        shakeTween.TweenProperty(container, "position", originalPos, 0.03f);
    }

    private static Color FeedbackColor(SlotFeedback f) => f switch
    {
        SlotFeedback.Correct      => ColorCorrect,
        SlotFeedback.WrongPosition => ColorWrongPos,
        SlotFeedback.NotPresent   => ColorNotPresent,
        _                         => ColorPending
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Animation state machine
    // ─────────────────────────────────────────────────────────────────────────

    private void TickAnimation(float delta)
    {
        if (_animState == AnimState.Idle) return;

        _animTimer -= delta;
        if (_animTimer > 0f) return;

        if (_animState == AnimState.Replaying)
            TickReplaying();
        else if (_animState == AnimState.ShowingNew)
            TickShowingNew();
    }

    private void TickReplaying()
    {
        int historyCount = _puzzle.GuessesMade; // includes the new guess
        int replayCount  = historyCount - 1;    // rows with replayResults

        if (_animRow >= replayCount)
        {
            // All history replayed → move to ShowingNew
            _animState = AnimState.ShowingNew;
            _animRow   = historyCount - 1;
            _animSlot  = 0;
            _animTimer = NewSlotDelay;
            CallDeferred(nameof(ScrollHistoryToBottom));
            return;
        }

        // Scroll to keep active row visible
        if (_animSlot == 0)
            CallDeferred(nameof(ScrollToRow), _animRow);

        // Reveal one slot in the current replay row
        var replayRow = _replayResults[_animRow];
        bool isTell   = replayRow.AlteredSlots[_animSlot];
        RevealHistorySlot(_animRow, _animSlot, replayRow.DisplayFeedback[_animSlot], isTell);

        _animSlot++;
        if (_animSlot >= _puzzle.SlotCount)
        {
            // Row complete — pause before next row
            _animSlot  = 0;
            _animRow++;
            _animTimer = BetweenRowPause;
        }
        else
        {
            _animTimer = PerSlotDelay;
        }
    }

    private void TickShowingNew()
    {
        int newRowIndex = _puzzle.GuessesMade - 1;
        var lastResult  = _puzzle.History[newRowIndex];

        bool isFeedbackTell = lastResult.LiedSlots[_animSlot];
        bool isValueTell = lastResult.ValueLiedSlots[_animSlot];

        // For value lies: show real value first, then glitch to swapped value
        if (isValueTell && newRowIndex < _historySlotLabels.Count)
        {
            var lbl = _historySlotLabels[newRowIndex][_animSlot];
            var slotBg = _historySlotBgs[newRowIndex][_animSlot];
            int realValue = lastResult.Guess[_animSlot];
            int fakeValue = lastResult.DisplayGuess[_animSlot];

            // Show real value first
            lbl.Text = HexLabels[realValue];
            slotBg.GetParent<Control>().GetChild<ColorRect>(1).Color = HexTints[realValue]; // inner rect

            // After pause, glitch the label and inner color to fake
            var labelTween = CreateTween();
            labelTween.TweenInterval(0.3f);
            labelTween.TweenCallback(Callable.From(() => {
                lbl.Text = "##";
            }));
            labelTween.TweenInterval(0.04f);
            labelTween.TweenCallback(Callable.From(() => {
                lbl.Text = HexLabels[realValue];
            }));
            labelTween.TweenInterval(0.03f);
            labelTween.TweenCallback(Callable.From(() => {
                lbl.Text = HexLabels[fakeValue];
                // Also update inner rect color to match fake value
                var inner = slotBg.GetParent<Control>().GetChild<ColorRect>(1);
                inner.Color = HexTints[fakeValue];
            }));
        }

        RevealHistorySlot(newRowIndex, _animSlot, lastResult.DisplayFeedback[_animSlot], isFeedbackTell);

        _animSlot++;
        if (_animSlot >= _puzzle.SlotCount)
        {
            // Done revealing new guess
            _animState = AnimState.Idle;
            _animTimer = 0f;
            OnGuessRevealed(lastResult);
        }
        else
        {
            _animTimer = NewSlotDelay;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Post-reveal handling
    // ─────────────────────────────────────────────────────────────────────────

    private void OnGuessRevealed(GuessResult result)
    {
        if (result.IsSolution)
        {
            _active = false;
            int guesses = _puzzle.GuessesMade;
            SetStatus($"DECRYPTED in {guesses} guess{(guesses == 1 ? "" : "es")}!");
            _statusLabel.AddThemeColorOverride("font_color", ColorTermText);
            EmitSignal(SignalName.PuzzleCompleted, guesses, _elapsed);
        }
        else
        {
            int guesses = _puzzle.GuessesMade;
            SetStatus($"Guess {guesses} recorded. Keep going.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Misc helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void SetStatus(string text)
    {
        if (_statusLabel != null)
            _statusLabel.Text = text;
    }

    private void UpdateTimerLabel()
    {
        if (_timerLabel != null)
            _timerLabel.Text = $"{_elapsed:F1}s";
    }

    private void OnCancel()
    {
        _active = false;
        EmitSignal(SignalName.PuzzleCancelled);
    }
}
