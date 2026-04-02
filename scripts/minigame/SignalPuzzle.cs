using Godot;
using Signal.Core;

namespace Signal.Minigame;

/// <summary>
/// Main controller for the signal reconstruction minigame.
/// Ties together WaveformData (math), WaveformDisplay (visuals), and FilterUI (controls).
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
    private Label _clarityLabel;
    private Label _typeLabel;
    private Button _cancelButton;

    private float _timeSpent;
    private bool _isActive;
    private bool _isComplete;
    private float _completionFlashTimer;
    private int _selectedPreset = -1;

    // Preset base frequency and bandwidth per signal type
    // These get the player in the right ballpark — sliders fine-tune from here
    private static readonly (float freq, float bandwidth)[] PresetValues = new[]
    {
        (0.3f, 0.15f),  // CrewLog
        (0.5f, 0.08f),  // SensorData
        (0.7f, 0.10f),  // SystemMessage
        (0.6f, 0.12f),  // Encrypted
    };

    public override void _Ready()
    {
        BuildUI();
    }

    private void BuildUI()
    {
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

        _clarityLabel = new Label();
        _clarityLabel.Text = "Clarity: 0%";
        _clarityLabel.AddThemeFontSizeOverride("font_size", 16);
        _clarityLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.3f, 0.3f));
        titleBar.AddChild(_clarityLabel);

        var spacer2 = new Control();
        spacer2.CustomMinimumSize = new Vector2(16, 0);
        titleBar.AddChild(spacer2);

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
        _statusLabel.Text = "Select a signal type preset, then tune the filters to isolate the signal.";
        _statusLabel.HorizontalAlignment = HorizontalAlignment.Center;
        _statusLabel.AutowrapMode = TextServer.AutowrapMode.Word;
        _statusLabel.AddThemeFontSizeOverride("font_size", 14);
        _statusLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        root.AddChild(_statusLabel);

        // Filter controls
        _filterUI = new FilterUI();
        _filterUI.CustomMinimumSize = new Vector2(0, 200);
        _filterUI.PresetSelected += OnPresetSelected;
        _filterUI.FilterChanged += OnFilterChanged;
        root.AddChild(_filterUI);
    }

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
        _selectedPreset = -1;
        _timeSpent = 0;
        _isActive = true;
        _isComplete = false;
        _completionFlashTimer = 0;

        // All presets available on all difficulties — the challenge is choosing correctly
        _filterUI.SetAvailablePresets(true, true, true, true);

        // Reset filter state
        _data.FilterFrequency = 0.5f;
        _data.FilterBandwidth = 0.1f;
        _data.FilterAmplitude = 0.0f;
        _data.FilterPhase = 0.5f;

        UpdateDisplay();
        UpdateClarityLabel(0);

        _statusLabel.AddThemeColorOverride("font_color", new Color(0.5f, 0.5f, 0.6f));
        _statusLabel.Text = "Step 1: Look at the waveform shape. Select the signal type you think it is.";

        GameLog.Event("Minigame", $"Puzzle started: type={type}, difficulty={difficulty}, seed={seed}");
    }

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
        else
        {
            _completionFlashTimer += (float)delta;
            if (_completionFlashTimer > 2.0f)
            {
                _isActive = false;
                EmitSignal(SignalName.PuzzleCompleted, _timeSpent);
            }
        }
    }

    private void OnPresetSelected(int presetIndex)
    {
        if (!_isActive || _isComplete) return;

        _selectedPreset = presetIndex;

        // Apply preset: SET the filter values (not add)
        var (freq, bandwidth) = PresetValues[presetIndex];
        _data.FilterFrequency = freq;
        _data.FilterBandwidth = bandwidth;
        _data.FilterAmplitude = 0.1f; // Light noise floor
        _data.FilterPhase = 0.5f;

        // Reset sliders to center (they adjust relative to preset)
        _filterUI.Reset();
        // Re-select the preset button after reset
        // (Reset clears selection, so we need to manually keep it)

        bool correct = presetIndex == (int)_data.CorrectType;
        if (correct)
            _statusLabel.Text = "Good match! Now adjust the sliders to fine-tune. Watch the clarity percentage.";
        else
            _statusLabel.Text = "That doesn't seem right — the waveform isn't responding well. Try a different type.";

        UpdateDisplay();
        CheckCompletion();

        GameLog.Event("Minigame", $"Preset selected: {(SignalType)presetIndex} (correct={correct}), clarity={_data.GetClarity():F2}");
    }

    private void OnFilterChanged(float frequency, float amplitude, float phase)
    {
        if (!_isActive || _isComplete || _data == null) return;

        if (_selectedPreset >= 0)
        {
            // Sliders adjust RELATIVE to the preset base values
            var (baseFreq, baseBandwidth) = PresetValues[_selectedPreset];

            // Frequency slider: ±0.2 offset from preset center
            _data.FilterFrequency = baseFreq + (frequency - 0.5f) * 0.4f;

            // Bandwidth stays at preset value (could add a slider later)
            _data.FilterBandwidth = baseBandwidth;

            // Amplitude threshold: 0.0 to 0.3 noise floor
            _data.FilterAmplitude = amplitude * 0.3f;

            // Phase: full 0-1 range
            _data.FilterPhase = phase;
        }
        else
        {
            // No preset selected — raw slider control (less effective)
            _data.FilterFrequency = frequency;
            _data.FilterBandwidth = 0.2f;
            _data.FilterAmplitude = amplitude * 0.3f;
            _data.FilterPhase = phase;

            _statusLabel.Text = "Select a signal type preset first for better results.";
        }

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
        UpdateClarityLabel(clarity);
    }

    private void UpdateClarityLabel(float clarity)
    {
        int pct = (int)(clarity * 100);
        _clarityLabel.Text = $"Clarity: {pct}%";

        // Color from red → yellow → green
        Color color;
        if (clarity < 0.5f)
            color = new Color(0.8f, 0.3f + clarity * 0.8f, 0.3f);
        else
            color = new Color(0.8f - (clarity - 0.5f) * 1.2f, 0.8f, 0.3f);

        _clarityLabel.AddThemeColorOverride("font_color", color);
    }

    private void CheckCompletion()
    {
        if (_data == null || _isComplete) return;

        var clarity = _data.GetClarity();
        if (_data.IsComplete())
        {
            _isComplete = true;
            _completionFlashTimer = 0;
            _statusLabel.Text = $"Signal isolated! Clarity: {(int)(clarity * 100)}% | Time: {_timeSpent:F1}s";
            _statusLabel.AddThemeColorOverride("font_color", new Color(0.2f, 0.9f, 0.3f));
            GameLog.Event("Minigame", $"Puzzle completed in {_timeSpent:F1}s, clarity={clarity:F2}");
        }
    }

    private void OnCancel()
    {
        _isActive = false;
        EmitSignal(SignalName.PuzzleCancelled);
        GameLog.Event("Minigame", "Puzzle cancelled");
    }
}
