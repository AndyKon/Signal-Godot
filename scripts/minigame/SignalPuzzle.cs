using Godot;
using Signal.Core;

namespace Signal.Minigame;

/// <summary>
/// Main controller for the signal reconstruction minigame.
/// Ties together WaveformData (math), WaveformDisplay (visuals), and FilterUI (controls).
/// Can be used standalone for testing or embedded in terminal interactions.
/// </summary>
public partial class SignalPuzzle : Control
{
    [Signal] public delegate void PuzzleCompletedEventHandler(float timeSpent);
    [Signal] public delegate void PuzzleCancelledEventHandler();

    private WaveformData _data;
    private WaveformDisplay _display;
    private FilterUI _filterUI;
    private Label _statusLabel;
    private Label _timerLabel;
    private Label _typeLabel;
    private Button _cancelButton;

    private float _timeSpent;
    private bool _isActive;
    private bool _isComplete;
    private float _completionFlashTimer;

    // Preset frequency/bandwidth mappings per signal type
    private static readonly (float freq, float bandwidth)[] PresetValues = new[]
    {
        (0.3f, 0.15f),  // CrewLog — lower frequency, wider band (organic)
        (0.5f, 0.08f),  // SensorData — mid frequency, narrow band (clean)
        (0.7f, 0.10f),  // SystemMessage — higher frequency, moderate band
        (0.6f, 0.12f),  // Encrypted — mid-high frequency, moderate band
    };

    public override void _Ready()
    {
        BuildUI();
    }

    private void BuildUI()
    {
        // Root layout
        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        root.AddThemeConstantOverride("separation", 8);
        AddChild(root);

        // Title bar
        var titleBar = new HBoxContainer();
        titleBar.AddThemeConstantOverride("separation", 16);
        root.AddChild(titleBar);

        _typeLabel = new Label();
        _typeLabel.Text = "SIGNAL RECONSTRUCTION";
        _typeLabel.AddThemeFontSizeOverride("font_size", 18);
        _typeLabel.AddThemeColorOverride("font_color", new Color(0.6f, 0.8f, 0.6f));
        titleBar.AddChild(_typeLabel);

        var spacer = new Control();
        spacer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        titleBar.AddChild(spacer);

        _timerLabel = new Label();
        _timerLabel.Text = "0.0s";
        _timerLabel.AddThemeFontSizeOverride("font_size", 16);
        _timerLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        titleBar.AddChild(_timerLabel);

        _cancelButton = new Button();
        _cancelButton.Text = "Cancel";
        _cancelButton.Pressed += OnCancel;
        titleBar.AddChild(_cancelButton);

        // Waveform display
        _display = new WaveformDisplay();
        _display.SizeFlagsVertical = SizeFlags.ExpandFill;
        root.AddChild(_display);

        // Status label
        _statusLabel = new Label();
        _statusLabel.Text = "Identify the signal type, select the correct preset, then tune the filters.";
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.AddThemeFontSizeOverride("font_size", 14);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        root.AddChild(_statusLabel);

        // Filter controls
        _filterUI = new FilterUI();
        _filterUI.CustomMinimumSize = new Vector2(0, 200);
        _filterUI.PresetSelected += OnPresetSelected;
        _filterUI.FilterChanged += OnFilterChanged;
        root.AddChild(_filterUI);

        // Background style
        var bg = new StyleBoxFlat();
        bg.BgColor = new Color(0.03f, 0.05f, 0.08f);
        bg.ContentMarginLeft = 16;
        bg.ContentMarginRight = 16;
        bg.ContentMarginTop = 12;
        bg.ContentMarginBottom = 12;
        AddThemeStyleboxOverride("panel", bg);
    }

    /// <summary>
    /// Start a new puzzle with the given parameters.
    /// </summary>
    public void StartPuzzle(SignalType type, int difficulty, int seed = -1)
    {
        if (seed < 0) seed = (int)(Time.GetTicksMsec() % int.MaxValue);

        _data = difficulty switch
        {
            0 => WaveformData.CreateEasy(type, seed),
            1 => WaveformData.CreateMedium(type, seed),
            2 => WaveformData.CreateHard(type, seed),
            3 => WaveformData.CreateNereus(type, seed),
            _ => WaveformData.CreateMedium(type, seed)
        };

        _filterUI.Reset();
        _timeSpent = 0;
        _isActive = true;
        _isComplete = false;
        _completionFlashTimer = 0;

        // Set available presets based on difficulty
        if (difficulty == 0)
        {
            // Tutorial: only correct type available
            _filterUI.SetAvailablePresets(
                type == SignalType.CrewLog,
                type == SignalType.SensorData,
                type == SignalType.SystemMessage,
                type == SignalType.Encrypted
            );
        }
        else
        {
            _filterUI.SetAvailablePresets(true, true, true, true);
        }

        UpdateDisplay();

        _statusLabel.Text = "Identify the signal type, select the correct preset, then tune the filters.";
        GameLog.Event("Minigame", $"Puzzle started: type={type}, difficulty={difficulty}, seed={seed}");
    }

    /// <summary>
    /// Start a random puzzle for quick testing.
    /// </summary>
    public void StartRandom(int difficulty = 1)
    {
        var types = new[] { SignalType.CrewLog, SignalType.SensorData, SignalType.SystemMessage, SignalType.Encrypted };
        var type = types[GD.RandRange(0, types.Length - 1)];
        StartPuzzle(type, difficulty);
    }

    public override void _Process(double delta)
    {
        if (!_isActive) return;

        if (!_isComplete)
        {
            _timeSpent += (float)delta;
            _timerLabel.Text = $"{_timeSpent:F1}s";
        }

        if (_isComplete)
        {
            _completionFlashTimer += (float)delta;
            if (_completionFlashTimer > 1.5f)
            {
                _isActive = false;
                EmitSignal(SignalName.PuzzleCompleted, _timeSpent);
            }
        }
    }

    private void OnPresetSelected(int presetIndex)
    {
        if (!_isActive || _isComplete) return;

        // Apply preset values
        var (freq, bandwidth) = PresetValues[presetIndex];
        _data.FilterFrequency = freq;
        _data.FilterBandwidth = bandwidth;

        // Check if correct type was selected
        bool correct = presetIndex == (int)_data.CorrectType;
        if (correct)
            _statusLabel.Text = "Correct type! Fine-tune the filters to isolate the signal.";
        else
            _statusLabel.Text = "Signal type may not match. Try adjusting or selecting a different preset.";

        UpdateDisplay();
        GameLog.Event("Minigame", $"Preset selected: {(SignalType)presetIndex} (correct={correct})");
    }

    private void OnFilterChanged(float frequency, float amplitude, float phase)
    {
        if (!_isActive || _isComplete) return;

        // Sliders adjust relative to the preset base values
        // Frequency: ±0.2 around preset center
        // Amplitude: 0.0 to 0.5 noise floor
        // Phase: 0.0 to 1.0 (full rotation)
        _data.FilterFrequency += (frequency - 0.5f) * 0.4f;
        _data.FilterAmplitude = amplitude * 0.5f;
        _data.FilterPhase = phase;

        UpdateDisplay();
        CheckCompletion();
    }

    private void UpdateDisplay()
    {
        if (_data == null) return;

        var raw = _data.GetRawSamples();
        var filtered = _data.GetFilteredSamples();
        var clarity = _data.GetClarity();

        _display.SetSamples(raw, filtered, clarity);
    }

    private void CheckCompletion()
    {
        if (_data == null || _isComplete) return;

        if (_data.IsComplete())
        {
            _isComplete = true;
            _completionFlashTimer = 0;
            _statusLabel.Text = $"Signal isolated! ({_timeSpent:F1}s)";
            _statusLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.9f, 0.3f));
            GameLog.Event("Minigame", $"Puzzle completed in {_timeSpent:F1}s");
        }
    }

    private void OnCancel()
    {
        _isActive = false;
        EmitSignal(SignalName.PuzzleCancelled);
        GameLog.Event("Minigame", "Puzzle cancelled");
    }
}
