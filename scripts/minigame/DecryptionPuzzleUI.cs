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
    private const int SlotSize = 56;
    private const int SlotGap = 8;
    private const int FeedbackDotSize = 12;

    // ── Hex value theme ───────────────────────────────────────────────────────
    private static readonly string[] HexLabels = { "0a", "3f", "b2", "e7", "1c", "d4", "8f", "5b" };
    private static readonly Color[] HexTints =
    {
        new Color(0.10f, 0.20f, 0.55f), // deep blue
        new Color(0.10f, 0.55f, 0.50f), // teal
        new Color(0.15f, 0.60f, 0.20f), // green
        new Color(0.75f, 0.55f, 0.05f), // amber
        new Color(0.80f, 0.35f, 0.05f), // orange
        new Color(0.75f, 0.12f, 0.12f), // red
        new Color(0.45f, 0.15f, 0.70f), // purple
        new Color(0.75f, 0.10f, 0.55f), // magenta
    };

    // ── Feedback colours ──────────────────────────────────────────────────────
    private static readonly Color ColorCorrect      = new Color(0.2f,  0.8f,  0.3f);
    private static readonly Color ColorWrongPos     = new Color(0.9f,  0.75f, 0.1f);
    private static readonly Color ColorNotPresent   = new Color(0.8f,  0.2f,  0.2f);
    private static readonly Color ColorPending      = new Color(0.15f, 0.18f, 0.22f);
    private static readonly Color ColorSlotEmpty    = new Color(0.08f, 0.10f, 0.14f);

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
        // Dark terminal background
        var bg = new ColorRect();
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bg.Color = new Color(0.03f, 0.04f, 0.06f);
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

    // ─────────────────────────────────────────────────────────────────────────
    // UI construction
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildUI(int section)
    {
        // Tear down previous UI root (if any) and rebuild from scratch
        _uiRoot?.QueueFree();

        var margin = new MarginContainer();
        _uiRoot = margin;
        margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left",   20);
        margin.AddThemeConstantOverride("margin_right",  20);
        margin.AddThemeConstantOverride("margin_top",    12);
        margin.AddThemeConstantOverride("margin_bottom", 12);
        AddChild(margin);

        var root = new VBoxContainer();
        root.AddThemeConstantOverride("separation", 8);
        margin.AddChild(root);

        // ── 1. Title bar ──────────────────────────────────────────────────────
        var titleBar = new HBoxContainer();
        titleBar.AddThemeConstantOverride("separation", 16);
        root.AddChild(titleBar);

        _titleLabel = new Label();
        _titleLabel.Text = $"DECRYPTION TERMINAL — Section {section}";
        _titleLabel.AddThemeFontSizeOverride("font_size", 18);
        _titleLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.9f, 0.5f));
        titleBar.AddChild(_titleLabel);

        var titleSpacer = new Control();
        titleSpacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleBar.AddChild(titleSpacer);

        _timerLabel = new Label();
        _timerLabel.Text = "0.0s";
        _timerLabel.AddThemeFontSizeOverride("font_size", 16);
        _timerLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        titleBar.AddChild(_timerLabel);

        // ── 2. Separator ──────────────────────────────────────────────────────
        var sep = new HSeparator();
        root.AddChild(sep);

        // ── 3. History scroll ─────────────────────────────────────────────────
        _historyScroll = new ScrollContainer();
        _historyScroll.SizeFlagsVertical = SizeFlags.ExpandFill;
        _historyScroll.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;
        root.AddChild(_historyScroll);

        _historyVBox = new VBoxContainer();
        _historyVBox.AddThemeConstantOverride("separation", 6);
        _historyVBox.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _historyScroll.AddChild(_historyVBox);

        // ── 4. Status label ───────────────────────────────────────────────────
        _statusLabel = new Label();
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        _statusLabel.AddThemeFontSizeOverride("font_size", 14);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.55f, 0.65f));
        root.AddChild(_statusLabel);

        // ── 5. Current guess input ────────────────────────────────────────────
        var inputRow = new HBoxContainer();
        inputRow.AddThemeConstantOverride("separation", SlotGap);
        root.AddChild(inputRow);

        _inputSlotContainers = new Control[_puzzle.SlotCount];
        _inputSlotBgs        = new ColorRect[_puzzle.SlotCount];
        _inputSlotLabels     = new Label[_puzzle.SlotCount];

        for (int i = 0; i < _puzzle.SlotCount; i++)
        {
            int slotIndex = i; // capture for closure
            var container = new Control();
            container.CustomMinimumSize = new Vector2(SlotSize, SlotSize);

            var slotBg = new ColorRect();
            slotBg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            slotBg.Color = ColorSlotEmpty;
            container.AddChild(slotBg);

            var slotBorder = new ColorRect();
            slotBorder.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            slotBorder.Color = new Color(0.25f, 0.30f, 0.38f, 1f);
            // Use a thin panel effect — just set as bg; real border via StyleBoxFlat on a Panel
            container.AddChild(slotBorder);

            // Solid inner fill on top of border
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
            lbl.AddThemeFontSizeOverride("font_size", 16);
            lbl.AddThemeColorOverride("font_color", Colors.White);
            container.AddChild(lbl);

            // Invisible click button
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

        var inputSpacer = new Control();
        inputSpacer.CustomMinimumSize = new Vector2(8, 0);
        inputRow.AddChild(inputSpacer);

        _submitButton = new Button();
        _submitButton.Text = "SUBMIT";
        _submitButton.CustomMinimumSize = new Vector2(80, SlotSize);
        _submitButton.AddThemeFontSizeOverride("font_size", 13);
        _submitButton.Pressed += TrySubmit;
        inputRow.AddChild(_submitButton);

        var cancelBtn = new Button();
        cancelBtn.Text = "✕";
        cancelBtn.CustomMinimumSize = new Vector2(40, SlotSize);
        cancelBtn.AddThemeFontSizeOverride("font_size", 16);
        cancelBtn.TooltipText = "Cancel";
        cancelBtn.Pressed += OnCancel;
        inputRow.AddChild(cancelBtn);

        // ── 6. Value picker ───────────────────────────────────────────────────
        var pickerRow = new HBoxContainer();
        pickerRow.AddThemeConstantOverride("separation", 6);
        root.AddChild(pickerRow);

        for (int v = 0; v < _puzzle.ValueCount; v++)
        {
            int valueIndex = v;
            var btn = new Button();
            btn.Text = HexLabels[v];
            btn.CustomMinimumSize = new Vector2(SlotSize, SlotSize);
            btn.AddThemeFontSizeOverride("font_size", 15);
            btn.AddThemeColorOverride("font_color", Colors.White);

            // Tinted StyleBoxFlat background
            var style = new StyleBoxFlat();
            style.BgColor = HexTints[v];
            style.BorderColor = HexTints[v].Lightened(0.3f);
            style.SetBorderWidthAll(2);
            style.SetCornerRadiusAll(4);
            btn.AddThemeStyleboxOverride("normal", style);

            var styleHover = (StyleBoxFlat)style.Duplicate();
            styleHover.BgColor = HexTints[v].Lightened(0.15f);
            btn.AddThemeStyleboxOverride("hover", styleHover);

            var stylePressed = (StyleBoxFlat)style.Duplicate();
            stylePressed.BgColor = HexTints[v].Darkened(0.10f);
            btn.AddThemeStyleboxOverride("pressed", stylePressed);

            btn.Pressed += () => AppendInputValue(valueIndex);
            pickerRow.AddChild(btn);
        }

        // Backspace button
        var bksp = new Button();
        bksp.Text = "⌫";
        bksp.CustomMinimumSize = new Vector2(SlotSize, SlotSize);
        bksp.AddThemeFontSizeOverride("font_size", 20);
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
        AddHistoryRow(_puzzle.GuessesMade - 1, result.Guess);

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
        numLabel.CustomMinimumSize = new Vector2(28, SlotSize);
        numLabel.VerticalAlignment = VerticalAlignment.Center;
        numLabel.AddThemeFontSizeOverride("font_size", 13);
        numLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
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
            inner.OffsetLeft   =  2;
            inner.OffsetTop    =  2;
            inner.OffsetRight  = -2;
            inner.OffsetBottom = -2;
            inner.Color = HexTints[guess[i]];
            container.AddChild(inner);

            var lbl = new Label();
            lbl.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            lbl.HorizontalAlignment = HorizontalAlignment.Center;
            lbl.VerticalAlignment   = VerticalAlignment.Center;
            lbl.Text = HexLabels[guess[i]];
            lbl.AddThemeFontSizeOverride("font_size", 15);
            lbl.AddThemeColorOverride("font_color", Colors.White);
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

    private void RevealHistorySlot(int rowIndex, int slotIndex, SlotFeedback feedback, bool isTell)
    {
        if (rowIndex >= _historySlotBgs.Count) return;

        var slotBg = _historySlotBgs[rowIndex][slotIndex];
        var dotBg  = _historyDotBgs[rowIndex][slotIndex];

        var feedColor = FeedbackColor(feedback);

        slotBg.Color = feedColor;
        dotBg.Color  = feedColor;

        if (isTell)
            PlayTellEffect(slotBg, slotBg.GetParent<Control>(), feedback);
    }

    private static Color FeedbackColor(SlotFeedback f) => f switch
    {
        SlotFeedback.Correct      => ColorCorrect,
        SlotFeedback.WrongPosition => ColorWrongPos,
        SlotFeedback.NotPresent   => ColorNotPresent,
        _                         => ColorPending
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Visual tells for NEREUS lies — section-aware
    // ─────────────────────────────────────────────────────────────────────────

    private void PlayTellEffect(ColorRect bg, Control container, SlotFeedback displayFeedback)
    {
        switch (_section)
        {
            case 3: PlayFlickerTell(bg, displayFeedback); break;
            case 4: PlayBriefTruthTell(bg, displayFeedback); break;
            case 5: PlayCompositeTell(bg, container, displayFeedback); break;
        }
    }

    /// <summary>Section 3: rapid flicker — glimpse of wrong color before settling.</summary>
    private void PlayFlickerTell(ColorRect bg, SlotFeedback displayFeedback)
    {
        var lieColor = FeedbackColor(displayFeedback).Darkened(0.5f);
        var flashColor = Colors.White.Lerp(lieColor, 0.3f);
        var tween = CreateTween();
        tween.TweenProperty(bg, "color", flashColor, 0.04f);
        tween.TweenProperty(bg, "color", lieColor, 0.04f);
        tween.TweenProperty(bg, "color", flashColor, 0.04f);
        tween.TweenProperty(bg, "color", lieColor, 0.04f);
        tween.TweenProperty(bg, "color", flashColor, 0.04f);
        tween.TweenProperty(bg, "color", lieColor, 0.06f);
    }

    /// <summary>Section 4: briefly shows contrasting hint color then transitions to lie.</summary>
    private void PlayBriefTruthTell(ColorRect bg, SlotFeedback displayFeedback)
    {
        var lieColor = FeedbackColor(displayFeedback).Darkened(0.5f);
        Color hintColor = displayFeedback switch
        {
            SlotFeedback.Correct      => ColorNotPresent.Darkened(0.3f),
            SlotFeedback.WrongPosition => ColorCorrect.Darkened(0.3f),
            SlotFeedback.NotPresent   => ColorWrongPos.Darkened(0.3f),
            _ => Colors.White
        };
        var tween = CreateTween();
        tween.TweenProperty(bg, "color", hintColor, 0.05f);
        tween.TweenInterval(0.15f);
        tween.TweenProperty(bg, "color", lieColor, 0.12f);
    }

    /// <summary>Section 5: composite flicker + position shake.</summary>
    private void PlayCompositeTell(ColorRect bg, Control container, SlotFeedback displayFeedback)
    {
        var lieColor = FeedbackColor(displayFeedback).Darkened(0.5f);
        Color hintColor = displayFeedback switch
        {
            SlotFeedback.Correct      => ColorNotPresent.Darkened(0.3f),
            SlotFeedback.WrongPosition => ColorCorrect.Darkened(0.3f),
            SlotFeedback.NotPresent   => ColorWrongPos.Darkened(0.3f),
            _ => Colors.White
        };
        var colorTween = CreateTween();
        colorTween.TweenProperty(bg, "color", hintColor, 0.04f);
        colorTween.TweenProperty(bg, "color", lieColor, 0.03f);
        colorTween.TweenProperty(bg, "color", hintColor, 0.04f);
        colorTween.TweenProperty(bg, "color", lieColor, 0.1f);

        var originalPos = container.Position;
        var shakeTween = CreateTween();
        shakeTween.TweenProperty(container, "position", originalPos + new Vector2(3, 0), 0.03f);
        shakeTween.TweenProperty(container, "position", originalPos + new Vector2(-3, 0), 0.03f);
        shakeTween.TweenProperty(container, "position", originalPos + new Vector2(2, 0), 0.03f);
        shakeTween.TweenProperty(container, "position", originalPos, 0.03f);
    }

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
            return;
        }

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

        bool isTell = lastResult.LiedSlots[_animSlot];
        RevealHistorySlot(newRowIndex, _animSlot, lastResult.DisplayFeedback[_animSlot], isTell);

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
            _statusLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.9f, 0.3f));
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
