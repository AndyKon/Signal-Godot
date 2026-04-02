using Godot;
using Signal.Core;

namespace Signal.Minigame;

/// <summary>
/// Standalone test harness for the signal reconstruction minigame.
/// Run directly to playtest the minigame in isolation.
/// Controls: 1-4 = signal type, Q/W/E/R = difficulty, Space = random puzzle
/// </summary>
public partial class MinigameTestHarness : Control
{
    private SignalPuzzle _puzzle;
    private Label _infoLabel;
    private Label _resultsLabel;
    private int _completedCount;
    private float _totalTime;
    private SignalType _currentType = SignalType.SensorData;
    private int _currentDifficulty = 0;

    public override void _Ready()
    {
        // Background
        var bg = new ColorRect();
        bg.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        bg.Color = new Color(0.02f, 0.03f, 0.05f);
        AddChild(bg);

        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 8);
        var rootMargin = new MarginContainer();
        rootMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        rootMargin.AddThemeConstantOverride("margin_left", 20);
        rootMargin.AddThemeConstantOverride("margin_right", 20);
        rootMargin.AddThemeConstantOverride("margin_top", 10);
        rootMargin.AddThemeConstantOverride("margin_bottom", 10);
        rootMargin.AddChild(root);
        AddChild(rootMargin);

        // Info bar
        _infoLabel = new Label();
        _infoLabel.AddThemeFontSizeOverride("font_size", 14);
        _infoLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.4f, 0.5f));
        UpdateInfoLabel();
        root.AddChild(_infoLabel);

        // Puzzle
        _puzzle = new SignalPuzzle();
        _puzzle.SizeFlagsVertical = SizeFlags.ExpandFill;
        _puzzle.PuzzleCompleted += OnPuzzleCompleted;
        _puzzle.PuzzleCancelled += OnPuzzleCancelled;
        root.AddChild(_puzzle);

        // Results
        _resultsLabel = new Label();
        _resultsLabel.AddThemeFontSizeOverride("font_size", 14);
        _resultsLabel.AddThemeColorOverride("font_color", new Color(0.4f, 0.6f, 0.4f));
        _resultsLabel.Text = "Completed: 0 | Avg time: --";
        root.AddChild(_resultsLabel);

        // Key hints
        var hints = new Label();
        hints.AddThemeFontSizeOverride("font_size", 12);
        hints.AddThemeColorOverride("font_color", new Color(0.3f, 0.3f, 0.4f));
        hints.Text = "Keys: 1=Crew 2=Sensor 3=System 4=Encrypted | Q=Easy W=Medium E=Hard R=NEREUS | Space=Start | Esc=Quit";
        root.AddChild(hints);

        // Start with an easy sensor data puzzle
        CallDeferred(MethodName.StartInitialPuzzle);

        GameLog.Event("Test", "Minigame test harness loaded");
    }

    private void StartInitialPuzzle()
    {
        _puzzle.StartPuzzle(_currentType, _currentDifficulty);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed) return;

        switch (key.Keycode)
        {
            // Signal type selection
            case Key.Key1: _currentType = SignalType.CrewLog; UpdateInfoLabel(); _puzzle.StartPuzzle(_currentType, _currentDifficulty); break;
            case Key.Key2: _currentType = SignalType.SensorData; UpdateInfoLabel(); _puzzle.StartPuzzle(_currentType, _currentDifficulty); break;
            case Key.Key3: _currentType = SignalType.SystemMessage; UpdateInfoLabel(); _puzzle.StartPuzzle(_currentType, _currentDifficulty); break;
            case Key.Key4: _currentType = SignalType.Encrypted; UpdateInfoLabel(); _puzzle.StartPuzzle(_currentType, _currentDifficulty); break;

            // Difficulty selection
            case Key.Q: _currentDifficulty = 0; UpdateInfoLabel(); _puzzle.StartPuzzle(_currentType, _currentDifficulty); break;
            case Key.W: _currentDifficulty = 1; UpdateInfoLabel(); _puzzle.StartPuzzle(_currentType, _currentDifficulty); break;
            case Key.E: _currentDifficulty = 2; UpdateInfoLabel(); _puzzle.StartPuzzle(_currentType, _currentDifficulty); break;
            case Key.R: _currentDifficulty = 3; UpdateInfoLabel(); _puzzle.StartPuzzle(_currentType, _currentDifficulty); break;

            // Random
            case Key.Space: _puzzle.StartRandom(_currentDifficulty); break;

            // Quit
            case Key.Escape: GetTree().Quit(); break;
        }

        GetViewport().SetInputAsHandled();
    }

    private void UpdateInfoLabel()
    {
        string[] types = { "Crew Log", "Sensor Data", "System Message", "Encrypted" };
        string[] diffs = { "Easy", "Medium", "Hard", "NEREUS" };
        _infoLabel.Text = $"Type: {types[(int)_currentType]} | Difficulty: {diffs[_currentDifficulty]}";
    }

    private void OnPuzzleCompleted(float timeSpent)
    {
        _completedCount++;
        _totalTime += timeSpent;
        float avg = _totalTime / _completedCount;
        _resultsLabel.Text = $"Completed: {_completedCount} | Last: {timeSpent:F1}s | Avg: {avg:F1}s";
        GameLog.Event("Test", $"Puzzle completed: {timeSpent:F1}s (avg: {avg:F1}s over {_completedCount})");

        // Auto-start next puzzle after a brief delay
        GetTree().CreateTimer(2.0).Timeout += () => _puzzle.StartPuzzle(_currentType, _currentDifficulty);
    }

    private void OnPuzzleCancelled()
    {
        _puzzle.StartPuzzle(_currentType, _currentDifficulty);
    }
}
