using System.Collections.Generic;
using Godot;
using Signal.Core;

namespace Signal.Narrative;

public partial class NarrativeManager : CanvasLayer
{
    public static NarrativeManager Instance { get; private set; }

    private PanelContainer _panel;
    private RichTextLabel _textDisplay;
    private AudioStreamPlayer _voicePlayer;
    private readonly Dictionary<string, NarrativeEntry> _entryLookup = new();
    private bool _isDisplaying;
    private string _fullText = "";
    private int _visibleChars;
    private float _charTimer;
    private const float CharDelay = 0.03f;
    private bool _skipRequested;

    public bool IsDisplaying => _isDisplaying;

    public override void _Ready()
    {
        Instance = this;
        Layer = 10;

        BuildUI();
        LoadNarrativeEntries();

        _panel.Visible = false;
    }

    private void BuildUI()
    {
        _panel = new PanelContainer();
        _panel.AnchorsPreset = (int)Control.LayoutPreset.BottomWide;
        _panel.OffsetTop = -120;
        _panel.OffsetBottom = 0;

        var stylebox = new StyleBoxFlat();
        stylebox.BgColor = new Color(0, 0, 0, 0.78f);
        stylebox.ContentMarginLeft = 16;
        stylebox.ContentMarginRight = 16;
        stylebox.ContentMarginTop = 12;
        stylebox.ContentMarginBottom = 12;
        _panel.AddThemeStyleboxOverride("panel", stylebox);
        AddChild(_panel);

        _textDisplay = new RichTextLabel();
        _textDisplay.BbcodeEnabled = true;
        _textDisplay.FitContent = true;
        _textDisplay.ScrollActive = false;
        _textDisplay.VisibleCharacters = 0;
        _panel.AddChild(_textDisplay);

        _voicePlayer = new AudioStreamPlayer();
        AddChild(_voicePlayer);
    }

    private void LoadNarrativeEntries()
    {
        string dir = "res://data/narrative/";
        if (!DirAccess.DirExistsAbsolute(dir)) return;

        var dirAccess = DirAccess.Open(dir);
        if (dirAccess == null) return;

        dirAccess.ListDirBegin();
        string file = dirAccess.GetNext();
        while (file != "")
        {
            if (file.EndsWith(".tres"))
            {
                var entry = GD.Load<NarrativeEntry>(dir + file);
                if (entry != null)
                    _entryLookup[entry.EntryId] = entry;
            }
            file = dirAccess.GetNext();
        }
    }

    public void ShowText(string text)
    {
        _fullText = text;
        _textDisplay.Text = text;
        _textDisplay.VisibleCharacters = 0;
        _visibleChars = 0;
        _charTimer = 0;
        _skipRequested = false;
        _isDisplaying = true;
        _panel.Visible = true;
    }

    public void PlayEntry(string entryId)
    {
        if (!_entryLookup.TryGetValue(entryId, out var entry))
        {
            GD.PushWarning($"Narrative entry not found: {entryId}");
            return;
        }

        var state = GameManager.Instance?.State;
        bool useAlt = state != null &&
            !string.IsNullOrEmpty(entry.AltConditionFlag) &&
            state.HasFlag(entry.AltConditionFlag);

        string text = useAlt ? entry.AltText : entry.Text;
        AudioStream clip = useAlt ? entry.AltVoiceClip : entry.VoiceClip;

        ShowText(text);

        if (clip != null)
        {
            _voicePlayer.Stream = clip;
            _voicePlayer.Play();
        }

        if (state != null && !string.IsNullOrEmpty(entry.FlagToSet))
            state.SetFlag(entry.FlagToSet);
    }

    public override void _Process(double delta)
    {
        if (!_isDisplaying) return;

        // Handle skip/dismiss
        if (Input.IsActionJustPressed("ui_accept") || Input.IsMouseButtonPressed(MouseButton.Left))
        {
            if (_visibleChars < _fullText.Length)
            {
                // Skip typewriter — show all text
                _textDisplay.VisibleCharacters = -1;
                _visibleChars = _fullText.Length;
                return;
            }
            else
            {
                // Dismiss
                Hide();
                return;
            }
        }

        // Typewriter effect
        if (_visibleChars < _fullText.Length)
        {
            _charTimer += (float)delta;
            while (_charTimer >= CharDelay && _visibleChars < _fullText.Length)
            {
                _charTimer -= CharDelay;
                _visibleChars++;
                _textDisplay.VisibleCharacters = _visibleChars;
            }
        }
    }

    public new void Hide()
    {
        _panel.Visible = false;
        _isDisplaying = false;
        _textDisplay.Text = "";
        _textDisplay.VisibleCharacters = 0;
    }
}
