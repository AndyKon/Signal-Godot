using System.Collections.Generic;
using Godot;
using Signal.Core;
using Signal.Interaction;
using Signal.Narrative;

namespace Signal.Room;

/// <summary>
/// Base class for parallax rooms. Builds three depth layers from a RoomDefinition,
/// handles cursor-driven 2D panning, hotspot placement, scan reveal, and atmosphere.
/// </summary>
public partial class ParallaxRoom : Control
{
    // ── Parallax config ───────────────────────────────────────────────────
    private const float CanvasWidthRatio = 1.5f;   // 150% of viewport
    private const float CanvasHeightRatio = 1.3f;   // 130% of viewport
    private const float BgShiftX = 50f, BgShiftY = 30f;
    private const float MidShiftX = 25f, MidShiftY = 15f;
    private const float FgShiftX = 10f, FgShiftY = 6f;
    private const float PanSmooth = 0.15f;

    // ── Scan config ───────────────────────────────────────────────────────
    private const float ScanLineSpeed = 800f;
    private bool _scanActive;

    // ── State ─────────────────────────────────────────────────────────────
    private RoomDefinition _roomDef;
    private Vector2 _viewportSize;
    private Vector2 _canvasSize;
    private Vector2 _currentOffset;
    private Vector2 _targetOffset;

    // ── Layer nodes ───────────────────────────────────────────────────────
    private Control _bgLayer;
    private Control _midLayer;
    private Control _fgLayer;

    // ── Hotspot tracking ──────────────────────────────────────────────────
    private readonly List<Hotspot> _hotspots = new();
    private readonly List<(Hotspot hotspot, RoomHotspotDef def)> _optionalHotspots = new();

    public void Initialize(RoomDefinition roomDef)
    {
        _roomDef = roomDef;
    }

    public override void _Ready()
    {
        if (_roomDef == null)
        {
            GameLog.Error("Room", "ParallaxRoom: no RoomDefinition set");
            return;
        }

        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Ignore; // Don't eat clicks — let Area2D hotspots receive them
        _viewportSize = GetViewportRect().Size;
        _canvasSize = new Vector2(
            _viewportSize.X * CanvasWidthRatio,
            _viewportSize.Y * CanvasHeightRatio);

        BuildLayers();
        PlaceHotspots();
        SpawnAtmosphere();
        CheckEntryNarrative();

        GameLog.Event("Room", $"Loaded: {_roomDef.DisplayName} ({_roomDef.Id})");
    }

    // ─────────────────────────────────────────────────────────────────────
    // Layer construction
    // ─────────────────────────────────────────────────────────────────────

    private void BuildLayers()
    {
        // Offset so canvas is centered on viewport
        var offset = (_canvasSize - _viewportSize) / -2f;

        _bgLayer = CreateLayer("BgLayer", _roomDef.BgColor, offset, opaqueFill: true);
        _midLayer = CreateLayer("MidLayer", _roomDef.MidColor, offset, opaqueFill: false);
        _fgLayer = CreateLayer("FgLayer", _roomDef.FgColor, offset, opaqueFill: false);

        // All layers must ignore mouse so Area2D hotspots receive physics input
        _bgLayer.MouseFilter = MouseFilterEnum.Ignore;
        _midLayer.MouseFilter = MouseFilterEnum.Ignore;
        _fgLayer.MouseFilter = MouseFilterEnum.Ignore;
    }

    private Control CreateLayer(string name, Color color, Vector2 baseOffset, bool opaqueFill)
    {
        var layer = new Control();
        layer.Name = name;
        layer.Position = new Vector2(baseOffset.X, baseOffset.Y);
        layer.Size = _canvasSize;
        layer.MouseFilter = Control.MouseFilterEnum.Ignore;

        if (opaqueFill)
        {
            var bg = new ColorRect();
            bg.Size = _canvasSize;
            bg.Color = color;
            bg.MouseFilter = Control.MouseFilterEnum.Ignore;
            layer.AddChild(bg);
        }

        AddChild(layer);
        return layer;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Hotspot placement
    // ─────────────────────────────────────────────────────────────────────

    private void PlaceHotspots()
    {
        foreach (var def in _roomDef.Hotspots)
        {
            var parentLayer = def.Layer switch
            {
                RoomLayer.Bg => _bgLayer,
                RoomLayer.Mid => _midLayer,
                RoomLayer.Fg => _fgLayer,
                _ => _midLayer
            };

            var hotspot = new Hotspot();
            hotspot.Name = def.Id;
            hotspot.Position = def.Position;
            hotspot.Action = def.Action;
            hotspot.Condition = def.Condition;

            var shape = new CollisionShape2D();
            var rect = new RectangleShape2D();
            rect.Size = def.Size;
            shape.Shape = rect;
            hotspot.AddChild(shape);

            parentLayer.AddChild(hotspot);
            InteractionManager.Instance?.ConnectHotspot(hotspot);
            _hotspots.Add(hotspot);

            if (def.IsCriticalPath)
            {
                AddAmbientCue(hotspot, def.Size);
            }
            else
            {
                _optionalHotspots.Add((hotspot, def));
                // Optional hotspots start with highlight hidden
                hotspot.SetHighlight(false);
            }
        }
    }

    private void AddAmbientCue(Hotspot hotspot, Vector2 size)
    {
        var glow = new ColorRect();
        glow.Size = size;
        glow.Position = -size / 2f;
        glow.Color = new Color(0f, 0.9f, 0.4f, 0.08f);
        glow.MouseFilter = Control.MouseFilterEnum.Ignore;
        glow.Name = "AmbientCue";
        hotspot.AddChild(glow);

        // Pulse animation
        var tween = glow.CreateTween().SetLoops();
        tween.TweenProperty(glow, "color:a", 0.15f, 1.5f);
        tween.TweenProperty(glow, "color:a", 0.04f, 1.5f);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Parallax panning (both axes)
    // ─────────────────────────────────────────────────────────────────────

    public override void _Process(double delta)
    {
        UpdatePanning((float)delta);
        _scanActive = Input.IsKeyPressed(Key.Shift);
        UpdateScan();
    }

    private void UpdatePanning(float delta)
    {
        var mousePos = GetViewport().GetMousePosition();
        // Normalize to -1..1 range from viewport center
        float nx = (mousePos.X / _viewportSize.X - 0.5f) * 2f;
        float ny = (mousePos.Y / _viewportSize.Y - 0.5f) * 2f;

        _targetOffset = new Vector2(nx, ny);

        // Smooth interpolation
        _currentOffset = _currentOffset.Lerp(_targetOffset, 1f - Mathf.Exp(-delta / PanSmooth));

        // Apply per-layer shifts
        var baseOffset = (_canvasSize - _viewportSize) / -2f;
        _bgLayer.Position = baseOffset + new Vector2(
            _currentOffset.X * -BgShiftX,
            _currentOffset.Y * -BgShiftY);
        _midLayer.Position = baseOffset + new Vector2(
            _currentOffset.X * -MidShiftX,
            _currentOffset.Y * -MidShiftY);
        _fgLayer.Position = baseOffset + new Vector2(
            _currentOffset.X * -FgShiftX,
            _currentOffset.Y * -FgShiftY);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ECHO scan (Shift key)
    // ─────────────────────────────────────────────────────────────────────

    public override void _UnhandledInput(InputEvent @event)
    {
        // Scan handled in _Process via Input polling — modifier keys
        // don't reliably fire as InputEventKey in _UnhandledInput
    }

    private void UpdateScan()
    {
        foreach (var (hotspot, def) in _optionalHotspots)
        {
            // During scan, show a green tint on all optional hotspots
            var cue = hotspot.GetNodeOrNull<ColorRect>("ScanCue");
            if (_scanActive)
            {
                if (cue == null)
                {
                    cue = new ColorRect();
                    cue.Name = "ScanCue";
                    cue.Size = def.Size;
                    cue.Position = -def.Size / 2f;
                    cue.Color = new Color(0f, 0.9f, 0.4f, 0.12f);
                    cue.MouseFilter = Control.MouseFilterEnum.Ignore;
                    hotspot.AddChild(cue);
                }
                cue.Visible = true;
            }
            else if (cue != null)
            {
                cue.Visible = false;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Atmosphere
    // ─────────────────────────────────────────────────────────────────────

    private void SpawnAtmosphere()
    {
        var rng = new System.Random();

        // Dust particles
        for (int i = 0; i < _roomDef.DustCount; i++)
        {
            var dust = new ColorRect();
            dust.Size = new Vector2(2, 2);
            dust.Color = new Color(0.4f, 0.6f, 0.8f, 0.15f);
            dust.Position = new Vector2(rng.Next((int)_canvasSize.X), rng.Next((int)_canvasSize.Y));
            dust.MouseFilter = Control.MouseFilterEnum.Ignore;
            _fgLayer.AddChild(dust);

            // Float animation
            float duration = 5f + (float)rng.NextDouble() * 5f;
            float delay = (float)rng.NextDouble() * duration;
            var tween = dust.CreateTween().SetLoops();
            tween.TweenInterval(delay);
            tween.TweenProperty(dust, "position:y", dust.Position.Y - 80f, duration);
            tween.TweenProperty(dust, "position:y", dust.Position.Y, 0f);
        }

        // Emergency lights
        if (_roomDef.HasEmergencyLights)
        {
            var light = new ColorRect();
            light.Size = new Vector2(_canvasSize.X * 0.8f, 3f);
            light.Position = new Vector2(_canvasSize.X * 0.1f, _canvasSize.Y * 0.6f);
            light.Color = new Color(0.8f, 0.4f, 0f, 0.5f);
            light.MouseFilter = Control.MouseFilterEnum.Ignore;
            _midLayer.AddChild(light);

            var lightTween = light.CreateTween().SetLoops();
            lightTween.TweenProperty(light, "color:a", 0.8f, 2f);
            lightTween.TweenProperty(light, "color:a", 0.3f, 2f);
        }

        // Bioluminescence (viewport)
        if (_roomDef.HasViewport)
        {
            for (int i = 0; i < 5; i++)
            {
                var dot = new ColorRect();
                dot.Size = new Vector2(3 + rng.Next(4), 3 + rng.Next(4));
                dot.Color = new Color(0.1f, 0.4f + (float)rng.NextDouble() * 0.4f, 0.9f, 0.3f);
                dot.Position = new Vector2(
                    _canvasSize.X * 0.35f + rng.Next((int)(_canvasSize.X * 0.3f)),
                    _canvasSize.Y * 0.1f + rng.Next((int)(_canvasSize.Y * 0.3f)));
                dot.MouseFilter = Control.MouseFilterEnum.Ignore;
                _bgLayer.AddChild(dot);

                float delay2 = (float)rng.NextDouble() * 3f;
                var dotTween = dot.CreateTween().SetLoops();
                dotTween.TweenInterval(delay2);
                dotTween.TweenProperty(dot, "color:a", 0.8f, 1.5f);
                dotTween.TweenProperty(dot, "color:a", 0.15f, 1.5f);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Entry narrative
    // ─────────────────────────────────────────────────────────────────────

    private void CheckEntryNarrative()
    {
        if (string.IsNullOrEmpty(_roomDef.EntryNarrative)) return;
        var state = GameManager.Instance?.State;
        if (state == null) return;

        if (!state.HasFlag(_roomDef.VisitedFlag))
        {
            state.SetFlag(_roomDef.VisitedFlag);
            NarrativeManager.Instance?.ShowEchoMonologue(_roomDef.EntryNarrative);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Cleanup
    // ─────────────────────────────────────────────────────────────────────

    public override void _ExitTree()
    {
        foreach (var hotspot in _hotspots)
            InteractionManager.Instance?.DisconnectHotspot(hotspot);
    }
}
