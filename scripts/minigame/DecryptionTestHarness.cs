using Godot;
using Signal.Core;

namespace Signal.Minigame;

/// <summary>
/// Standalone test harness for the Mastermind decryption puzzle.
/// Controls: F1-F6 = section, Space = new puzzle (same section), Esc = quit
/// </summary>
public partial class DecryptionTestHarness : Control
{
    private DecryptionPuzzleUI _puzzleUI;
    private Label _infoLabel;
    private Label _resultsLabel;
    private int _completedCount;
    private float _totalTime;
    private int _totalGuesses;
    private int _currentSection = 1;

    public override void _Ready()
    {
        var bg = new ColorRect();
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bg.Color = new Color(0.02f, 0.03f, 0.05f);
        AddChild(bg);

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 4);
        var rootMargin = new MarginContainer();
        rootMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        rootMargin.AddThemeConstantOverride("margin_left", 12);
        rootMargin.AddThemeConstantOverride("margin_right", 12);
        rootMargin.AddThemeConstantOverride("margin_top", 8);
        rootMargin.AddThemeConstantOverride("margin_bottom", 8);
        rootMargin.AddChild(root);
        AddChild(rootMargin);

        _infoLabel = new Label();
        _infoLabel.AddThemeFontSizeOverride("font_size", 14);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
        UpdateInfoLabel();
        root.AddChild(_infoLabel);

        _puzzleUI = new DecryptionPuzzleUI();
        _puzzleUI.SizeFlagsVertical = SizeFlags.ExpandFill;
        _puzzleUI.PuzzleCompleted += OnPuzzleCompleted;
        _puzzleUI.PuzzleCancelled += OnPuzzleCancelled;
        root.AddChild(_puzzleUI);

        _resultsLabel = new Label();
        _resultsLabel.AddThemeFontSizeOverride("font_size", 14);
        _resultsLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.6f, 0.4f));
        _resultsLabel.Text = "Completed: 0 | Avg guesses: -- | Avg time: --";
        root.AddChild(_resultsLabel);

        var hints = new Label();
        hints.AddThemeFontSizeOverride("font_size", 12);
        hints.AddThemeColorOverride("font_color", new Color(0.3f, 0.3f, 0.4f));
        hints.Text = "F1-F6=Sections | F7=ValLie F8=FbLie F9=Both F10=Both5s F11=Fb+Replay F12=Everything | Space=New | Esc=Quit";
        root.AddChild(hints);

        // Check for --section N command line arg
        var args = OS.GetCmdlineUserArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--section" && int.TryParse(args[i + 1], out int sec) && sec >= 1 && sec <= 6)
            {
                _currentSection = sec;
                UpdateInfoLabel();
            }
        }

        CallDeferred(MethodName.StartInitialPuzzle);
        GameLog.Event("Test", $"Decryption test harness loaded (section {_currentSection})");
    }

    private void StartInitialPuzzle()
    {
        _puzzleUI.StartPuzzle(_currentSection);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed) return;

        switch (key.Keycode)
        {
            case Key.F1: _currentSection = 1; UpdateInfoLabel(); _puzzleUI.StartPuzzle(1); break;
            case Key.F2: _currentSection = 2; UpdateInfoLabel(); _puzzleUI.StartPuzzle(2); break;
            case Key.F3: _currentSection = 3; UpdateInfoLabel(); _puzzleUI.StartPuzzle(3); break;
            case Key.F4: _currentSection = 4; UpdateInfoLabel(); _puzzleUI.StartPuzzle(4); break;
            case Key.F5: _currentSection = 5; UpdateInfoLabel(); _puzzleUI.StartPuzzle(5); break;
            case Key.F6: _currentSection = 6; UpdateInfoLabel(); _puzzleUI.StartPuzzle(6); break;
            case Key.F7: _currentSection = 7; UpdateInfoLabel(); _puzzleUI.StartPuzzle(7); break;
            case Key.F8: _currentSection = 8; UpdateInfoLabel(); _puzzleUI.StartPuzzle(8); break;
            case Key.F9: _currentSection = 9; UpdateInfoLabel(); _puzzleUI.StartPuzzle(9); break;
            case Key.F10: _currentSection = 10; UpdateInfoLabel(); _puzzleUI.StartPuzzle(10); break;
            case Key.F11: _currentSection = 11; UpdateInfoLabel(); _puzzleUI.StartPuzzle(11); break;
            case Key.F12: _currentSection = 12; UpdateInfoLabel(); _puzzleUI.StartPuzzle(12); break;
            case Key.Space: _puzzleUI.StartPuzzle(_currentSection); break;
            case Key.Escape: GetTree().Quit(); break;
            default: return;
        }

        GetViewport().SetInputAsHandled();
    }

    private void UpdateInfoLabel()
    {
        string[] sections =
        {
            "",
            "S1: 4s/6v no lies",
            "S2: 4s/6v repeats",
            "S3: 4s/6v 1 value-swap",
            "S4: 4s/6v 1 feedback lie",
            "S5H: 6s/8v both lies + replay",
            "S5C: 4s/6v cooperative",
            "TEST: 4s/6v value-lie only",
            "TEST: 4s/6v feedback-lie only",
            "TEST: 4s/6v both lies (1 each)",
            "TEST: 5s/6v both lies",
            "TEST: 5s/6v feedback + replay",
            "TEST: 6s/8v everything"
        };
        _infoLabel.Text = _currentSection < sections.Length ? sections[_currentSection] : "Unknown";
    }

    private void OnPuzzleCompleted(int guessCount, float timeSpent)
    {
        _completedCount++;
        _totalTime += timeSpent;
        _totalGuesses += guessCount;
        float avgTime = _totalTime / _completedCount;
        float avgGuesses = (float)_totalGuesses / _completedCount;
        _resultsLabel.Text = $"Completed: {_completedCount} | Last: {guessCount} guesses in {timeSpent:F1}s | Avg: {avgGuesses:F1} guesses, {avgTime:F1}s";
        GameLog.Event("Test", $"Decryption completed: {guessCount} guesses, {timeSpent:F1}s (avg: {avgGuesses:F1} guesses, {avgTime:F1}s over {_completedCount})");

        GetTree().CreateTimer(2.0).Timeout += () => _puzzleUI.StartPuzzle(_currentSection);
    }

    private void OnPuzzleCancelled()
    {
        _puzzleUI.StartPuzzle(_currentSection);
    }
}
