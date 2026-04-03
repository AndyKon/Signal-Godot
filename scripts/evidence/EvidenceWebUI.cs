using System.Collections.Generic;
using System.Linq;
using Godot;
using Signal.Core;

namespace Signal.Evidence;

/// <summary>
/// Fullscreen evidence web overlay. ECHO's internal memory correlation system.
/// Press J to toggle. Pauses game and timer while open.
/// </summary>
public partial class EvidenceWebUI : CanvasLayer
{
    public static EvidenceWebUI Instance { get; private set; }

    // ── Visual constants ──────────────────────────────────────────────────
    private static readonly Color BgColor = new(0.0f, 0.0f, 0.02f, 0.95f);
    private static readonly Color PanelBg = new(0.02f, 0.04f, 0.03f);
    private static readonly Color BorderColor = new(0.0f, 0.7f, 0.3f, 0.4f);
    private static readonly Color TextColor = new(0.0f, 0.9f, 0.4f);
    private static readonly Color DimColor = new(0.0f, 0.45f, 0.2f);
    private static readonly Color LineColor = new(0.0f, 0.5f, 0.25f, 0.2f);
    private static readonly Color LineActiveColor = new(0.0f, 0.8f, 0.4f, 0.6f);

    private static readonly Color[] TypeColors =
    {
        new(0.0f, 0.9f, 0.4f),  // TerminalLog — green
        new(0.0f, 0.7f, 1.0f),  // Environmental — cyan
        new(1.0f, 0.7f, 0.0f),  // SensorData — amber
        new(1.0f, 0.3f, 0.6f),  // CrewDialogue — pink
    };

    private const int NodeWidth = 240;
    private const int NodeHeight = 36;
    private const int NodeGap = 6;
    private const int GroupGap = 40;
    private const int DetailPanelWidth = 320;

    // ── Group layout positions (fixed) ────────────────────────────────────
    private static readonly Dictionary<EvidenceGroup, Vector2> GroupPositions = new()
    {
        { EvidenceGroup.PlatformSystems, new Vector2(60, 80) },
        { EvidenceGroup.Personnel,       new Vector2(60, 300) },
        { EvidenceGroup.Operations,      new Vector2(400, 80) },
        { EvidenceGroup.NEREUS,          new Vector2(400, 360) },
        { EvidenceGroup.Timeline,        new Vector2(740, 80) },
        { EvidenceGroup.ECHO,            new Vector2(740, 280) },
        { EvidenceGroup.External,        new Vector2(740, 460) },
    };

    // ── State ─────────────────────────────────────────────────────────────
    private bool _isOpen;
    private string _selectedId;
    private HashSet<string> _lastSeenConnections = new();

    // ── UI refs ───────────────────────────────────────────────────────────
    private Control _root;
    private Control _webCanvas;
    private PanelContainer _detailPanel;
    private Label _detailType;
    private Label _detailTitle;
    private Label _detailSource;
    private RichTextLabel _detailBody;
    private Label _headerLabel;

    // Node tracking: evidence ID → visual node Control
    private readonly Dictionary<string, Control> _nodeControls = new();
    // Node centers for connection line drawing
    private readonly Dictionary<string, Vector2> _nodeCenters = new();

    public override void _Ready()
    {
        Instance = this;
        Layer = 30;
        ProcessMode = ProcessModeEnum.Always; // process even when paused

        _root = new Control();
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.ProcessMode = ProcessModeEnum.Always;
        _root.Visible = false;
        AddChild(_root);

        GameLog.ManagerReady("EvidenceWebUI");
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed) return;

        if (key.Keycode == Key.J)
        {
            if (_isOpen) Close();
            else Open();
            GetViewport().SetInputAsHandled();
        }
        else if (_isOpen && key.Keycode == Key.Escape)
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    private void Open()
    {
        _isOpen = true;
        GetTree().Paused = true;
        Rebuild();
        _root.Visible = true;
        var entries = EvidenceManager.Instance?.GetDiscoveredEntries();
        var conns = EvidenceManager.Instance?.GetActiveConnections();
        GameLog.Event("Evidence", $"Web opened: {entries?.Count ?? 0} entries, {conns?.Count ?? 0} connections, {_nodeControls.Count} nodes rendered");
    }

    private void Close()
    {
        _isOpen = false;
        _selectedId = null;
        _root.Visible = false;
        GetTree().Paused = false;

        // Track what connections the player has seen
        var state = GameManager.Instance?.State;
        if (state != null)
            _lastSeenConnections = new HashSet<string>(state.FiredConnections);
    }

    private void Rebuild()
    {
        // Clear and rebuild
        foreach (var child in _root.GetChildren()) child.QueueFree();
        _nodeControls.Clear();
        _nodeCenters.Clear();
        _selectedId = null;

        // Background
        var bg = new ColorRect();
        bg.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        bg.Color = BgColor;
        // Click background to deselect
        var bgBtn = new Button();
        bgBtn.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        bgBtn.Flat = true;
        bgBtn.Modulate = Colors.Transparent;
        bgBtn.Pressed += () => SelectNode(null);
        _root.AddChild(bg);
        _root.AddChild(bgBtn);

        // Scrollable canvas for the web
        var scroll = new ScrollContainer();
        scroll.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        scroll.CustomMinimumSize = new Vector2(0, 0);
        _root.AddChild(scroll);

        _webCanvas = new Control();
        _webCanvas.CustomMinimumSize = new Vector2(1100, 700);
        scroll.AddChild(_webCanvas);

        // Header
        var entries = EvidenceManager.Instance?.GetDiscoveredEntries() ?? new List<EvidenceEntry>();
        var connections = EvidenceManager.Instance?.GetActiveConnections() ?? new List<EvidenceConnection>();

        _headerLabel = new Label();
        _headerLabel.Text = $"> ECHO MEMORY // DATA CORRELATION [ {entries.Count} RECORDS / {connections.Count} CONNECTIONS ]";
        _headerLabel.AddThemeFontSizeOverride("font_size", 16);
        _headerLabel.AddThemeColorOverride("font_color", TextColor);
        _headerLabel.Position = new Vector2(24, 20);
        _webCanvas.AddChild(_headerLabel);

        // Separator
        var sep = new ColorRect();
        sep.Position = new Vector2(24, 50);
        sep.Size = new Vector2(1050, 1);
        sep.Color = BorderColor;
        _webCanvas.AddChild(sep);

        // Group entries by group
        var grouped = new Dictionary<EvidenceGroup, List<EvidenceEntry>>();
        foreach (var entry in entries)
        {
            if (!grouped.ContainsKey(entry.Group))
                grouped[entry.Group] = new List<EvidenceEntry>();
            grouped[entry.Group].Add(entry);
        }

        // Render groups and nodes
        foreach (var (group, pos) in GroupPositions)
        {
            // Group label (always visible if any entries discovered in it)
            if (!grouped.ContainsKey(group)) continue;

            var groupLabel = new Label();
            groupLabel.Text = group.ToString().ToUpper();
            groupLabel.AddThemeFontSizeOverride("font_size", 11);
            groupLabel.AddThemeColorOverride("font_color", DimColor);
            groupLabel.Position = pos;
            _webCanvas.AddChild(groupLabel);

            // Nodes
            float y = pos.Y + 22;
            foreach (var entry in grouped[group])
            {
                var node = CreateNode(entry, new Vector2(pos.X, y));
                _webCanvas.AddChild(node);
                _nodeControls[entry.Id] = node;
                _nodeCenters[entry.Id] = new Vector2(pos.X + NodeWidth / 2f, y + NodeHeight / 2f);
                y += NodeHeight + NodeGap;
            }
        }

        // Connection lines (drawn as Line2D children of canvas)
        foreach (var conn in connections)
        {
            if (!_nodeCenters.ContainsKey(conn.EntryAId) || !_nodeCenters.ContainsKey(conn.EntryBId))
                continue;
            DrawConnectionLine(conn);
        }

        // Detail panel (right side, hidden until selection)
        BuildDetailPanel();
    }

    private Control CreateNode(EvidenceEntry entry, Vector2 position)
    {
        var container = new Control();
        container.Position = position;
        container.CustomMinimumSize = new Vector2(NodeWidth, NodeHeight);
        container.Size = new Vector2(NodeWidth, NodeHeight);

        var panel = new PanelContainer();
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.0f, 0.04f, 0.02f, 0.8f);
        style.BorderColor = BorderColor;
        style.SetBorderWidthAll(1);
        style.ContentMarginLeft = 8;
        style.ContentMarginRight = 8;
        style.ContentMarginTop = 4;
        style.ContentMarginBottom = 4;
        panel.AddThemeStyleboxOverride("panel", style);
        container.AddChild(panel);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 6);
        panel.AddChild(hbox);

        // Type pip
        var pip = new ColorRect();
        pip.CustomMinimumSize = new Vector2(6, 6);
        pip.Color = TypeColors[(int)entry.Type];
        var pipCenter = new CenterContainer();
        pipCenter.AddChild(pip);
        hbox.AddChild(pipCenter);

        // New connection pulse
        bool isNew = false;
        var state = GameManager.Instance?.State;
        if (state != null)
        {
            foreach (var conn in EvidenceRegistry.Connections)
            {
                if (conn.EntryAId != entry.Id && conn.EntryBId != entry.Id) continue;
                if (!state.IsConnectionFired(conn.Key)) continue;
                if (!_lastSeenConnections.Contains(conn.Key)) { isNew = true; break; }
            }
        }

        if (isNew)
        {
            var pulse = new ColorRect();
            pulse.CustomMinimumSize = new Vector2(6, 6);
            pulse.Color = TextColor;
            // Simple blink via modulate animation
            var tween = pulse.CreateTween().SetLoops();
            tween.TweenProperty(pulse, "modulate:a", 0.3f, 0.8f);
            tween.TweenProperty(pulse, "modulate:a", 1.0f, 0.8f);
            var pulseCenter = new CenterContainer();
            pulseCenter.AddChild(pulse);
            hbox.AddChild(pulseCenter);
        }

        // Title
        var title = new Label();
        title.Text = entry.Title;
        title.AddThemeFontSizeOverride("font_size", 12);
        title.AddThemeColorOverride("font_color", TextColor);
        title.ClipText = true;
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddChild(title);

        // Click handler
        var btn = new Button();
        btn.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        btn.Flat = true;
        btn.Modulate = Colors.Transparent;
        string id = entry.Id;
        btn.Pressed += () => SelectNode(id);
        container.AddChild(btn);

        return container;
    }

    private void DrawConnectionLine(EvidenceConnection conn)
    {
        var from = _nodeCenters[conn.EntryAId];
        var to = _nodeCenters[conn.EntryBId];

        var line = new Line2D();
        line.Width = 1.0f;
        line.DefaultColor = LineColor;
        line.AddPoint(from);

        // Simple curve via midpoint offset
        var mid = (from + to) / 2f;
        float offset = (to.Y - from.Y) * 0.2f;
        line.AddPoint(new Vector2(mid.X + offset, mid.Y));
        line.AddPoint(to);

        line.Name = $"conn_{conn.Key}";
        _webCanvas.AddChild(line);
    }

    private void SelectNode(string id)
    {
        _selectedId = id;

        // Get connected IDs
        var connectedIds = new HashSet<string>();
        if (id != null)
        {
            connectedIds.Add(id);
            var connections = EvidenceManager.Instance?.GetActiveConnections() ?? new List<EvidenceConnection>();
            foreach (var conn in connections)
            {
                if (conn.EntryAId == id) connectedIds.Add(conn.EntryBId);
                if (conn.EntryBId == id) connectedIds.Add(conn.EntryAId);
            }
        }

        // Update node visuals
        foreach (var (nodeId, control) in _nodeControls)
        {
            if (id == null)
            {
                control.Modulate = Colors.White;
            }
            else if (connectedIds.Contains(nodeId))
            {
                control.Modulate = Colors.White;
            }
            else
            {
                control.Modulate = new Color(1, 1, 1, 0.25f);
            }
        }

        // Update connection lines
        foreach (var child in _webCanvas.GetChildren())
        {
            if (child is Line2D line && line.Name.ToString().StartsWith("conn_"))
            {
                if (id == null)
                {
                    line.DefaultColor = LineColor;
                    line.Width = 1.0f;
                }
                else
                {
                    string connKey = line.Name.ToString().Substring(5);
                    var parts = connKey.Split(':');
                    bool active = parts.Length == 2 && (parts[0] == id || parts[1] == id);
                    line.DefaultColor = active ? LineActiveColor : LineColor;
                    line.Width = active ? 2.0f : 1.0f;
                }
            }
        }

        // Detail panel
        if (id != null)
        {
            var entry = EvidenceRegistry.GetEntry(id);
            if (entry != null) ShowDetail(entry);
        }
        else
        {
            _detailPanel.Visible = false;
        }
    }

    private void BuildDetailPanel()
    {
        _detailPanel = new PanelContainer();
        _detailPanel.SetAnchorsPreset(Control.LayoutPreset.RightWide);
        _detailPanel.OffsetLeft = -DetailPanelWidth;
        _detailPanel.Visible = false;

        var style = new StyleBoxFlat();
        style.BgColor = new Color(0.01f, 0.03f, 0.02f, 0.95f);
        style.BorderColor = BorderColor;
        style.BorderWidthLeft = 1;
        style.ContentMarginLeft = 20;
        style.ContentMarginRight = 20;
        style.ContentMarginTop = 20;
        style.ContentMarginBottom = 20;
        _detailPanel.AddThemeStyleboxOverride("panel", style);
        _root.AddChild(_detailPanel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 8);
        _detailPanel.AddChild(vbox);

        _detailType = new Label();
        _detailType.AddThemeFontSizeOverride("font_size", 10);
        _detailType.AddThemeColorOverride("font_color", DimColor);
        vbox.AddChild(_detailType);

        _detailTitle = new Label();
        _detailTitle.AddThemeFontSizeOverride("font_size", 16);
        _detailTitle.AddThemeColorOverride("font_color", TextColor);
        _detailTitle.AutowrapMode = TextServer.AutowrapMode.Word;
        vbox.AddChild(_detailTitle);

        _detailSource = new Label();
        _detailSource.AddThemeFontSizeOverride("font_size", 11);
        _detailSource.AddThemeColorOverride("font_color", new Color(0.0f, 0.55f, 0.3f));
        vbox.AddChild(_detailSource);

        var sep = new ColorRect();
        sep.CustomMinimumSize = new Vector2(0, 1);
        sep.Color = BorderColor;
        vbox.AddChild(sep);

        _detailBody = new RichTextLabel();
        _detailBody.BbcodeEnabled = true;
        _detailBody.FitContent = true;
        _detailBody.ScrollActive = false;
        _detailBody.AddThemeFontSizeOverride("normal_font_size", 13);
        _detailBody.AddThemeColorOverride("default_color", new Color(0.0f, 0.75f, 0.35f));
        vbox.AddChild(_detailBody);
    }

    private void ShowDetail(EvidenceEntry entry)
    {
        string[] typeNames = { "TERMINAL LOG", "ENVIRONMENTAL", "SENSOR DATA", "CREW DIALOGUE" };
        _detailType.Text = typeNames[(int)entry.Type];
        _detailTitle.Text = entry.Title;
        _detailSource.Text = entry.Source;
        _detailBody.Text = entry.Body;
        _detailPanel.Visible = true;
    }
}
