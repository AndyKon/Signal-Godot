# Parallax Room System Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a 2D parallax room system with data-driven content, ECHO monologue, scan mechanic, puzzle launch integration, and playable Section 1 (3 rooms) as a tutorial.

**Architecture:** `RoomDefinition` data class defines room layouts. `RoomRegistry` holds all room data statically. `ParallaxRoom` base class builds three parallax layers, places hotspots, handles panning/scan/atmosphere. `InteractionManager` gains puzzle-gate flow. `NarrativeManager` gains ECHO monologue styling. Old placeholder rooms are deleted. Section 1 validates the full gameplay loop.

**Tech Stack:** Godot 4.6 C# (.NET 8.0), programmatic UI, existing singleton/autoload pattern.

---

## File Structure

### Create
- `scripts/room/RoomDefinition.cs` — Data classes for room layout + hotspot definitions
- `scripts/room/RoomRegistry.cs` — Static data: Section 1 room definitions (3 rooms)
- `scripts/room/ParallaxRoom.cs` — Base class: 2D parallax layers, panning, hotspots, scan, atmosphere, entry narrative

### Modify
- `scripts/narrative/NarrativeManager.cs` — Add `ShowEchoMonologue()` with distinct cyan/blue styling
- `scripts/interaction/HotspotData.cs` — Add `RequiresPuzzle` and `PuzzleOverride` fields
- `scripts/interaction/InteractionManager.cs` — Add puzzle gate flow in `ExecuteAction`
- `scripts/tests/AutoPlaytest.cs` — Strip room-navigation tests, keep system tests
- `scenes/Autoload.tscn` — No changes needed (room system uses existing managers)
- `project.godot` — Update main scene to new Section 1 Room 1

### Delete
- `scripts/rooms/RoomBuilder.cs`
- `scripts/rooms/HubRoom1.cs`
- `scripts/rooms/HubRoom2.cs`
- `scripts/rooms/HubRoom3.cs`
- `scenes/Section1_Hub_Room1.tscn`
- `scenes/Section1_Hub_Room2.tscn`
- `scenes/Section1_Hub_Room3.tscn`

### New Scenes
- `scenes/Section1_PressureLockControl.tscn`
- `scenes/Section1_EquipmentStorage.tscn`
- `scenes/Section1_PowerJunction.tscn`

---

## Task 1: Room Data Classes

**Files:**
- Create: `scripts/room/RoomDefinition.cs`

- [ ] **Step 1: Create RoomDefinition.cs**

```csharp
using System.Collections.Generic;
using Godot;
using Signal.Interaction;

namespace Signal.Room;

public enum RoomLayer { Bg, Mid, Fg }

public class RoomHotspotDef
{
    public string Id;
    public Vector2 Position;      // relative to room canvas
    public Vector2 Size;
    public RoomLayer Layer;
    public bool IsCriticalPath;
    public HotspotData Action;
    public HotspotCondition Condition;
    public bool RequiresPuzzle;
    public string PuzzleOverride;  // empty = game-state difficulty

    public RoomHotspotDef(string id, Vector2 position, Vector2 size, RoomLayer layer,
                           bool criticalPath, HotspotData action,
                           HotspotCondition condition = null,
                           bool requiresPuzzle = false, string puzzleOverride = "")
    {
        Id = id;
        Position = position;
        Size = size;
        Layer = layer;
        IsCriticalPath = criticalPath;
        Action = action;
        Condition = condition;
        RequiresPuzzle = requiresPuzzle;
        PuzzleOverride = puzzleOverride;
    }
}

public class RoomDefinition
{
    public string Id;
    public string DisplayName;
    public int Section;

    // Layer colors (programmatic — replaceable with art at 2880×1404)
    public Color BgColor;
    public Color MidColor;
    public Color FgColor;

    // Audio
    public string AmbientClip = "";
    public string MusicClip = "";

    // Entry narrative (ECHO monologue, first visit only)
    // Tracked via auto-generated flag: "visited_" + Id
    public string EntryNarrative = "";

    // Atmosphere
    public int DustCount;
    public bool HasEmergencyLights;
    public bool HasViewport;

    // Hotspots
    public List<RoomHotspotDef> Hotspots = new();

    public string VisitedFlag => $"visited_{Id}";
}
```

- [ ] **Step 2: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add scripts/room/RoomDefinition.cs
git commit -m "feat: add RoomDefinition and RoomHotspotDef data classes"
```

---

## Task 2: ECHO Monologue in NarrativeManager

**Files:**
- Modify: `scripts/narrative/NarrativeManager.cs`

- [ ] **Step 1: Add ShowEchoMonologue method and distinct panel styling**

Add a second panel (`_echoPanel`) built in `BuildUI()` with cyan/blue styling. Add `ShowEchoMonologue(string text)` that uses this panel instead of the standard one.

In the `BuildUI()` method, after the existing `_panel` setup, add:

```csharp
        // ECHO monologue panel — distinct styling
        _echoPanel = new PanelContainer();
        _echoPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomWide);
        _echoPanel.OffsetTop = -120;
        _echoPanel.CustomMinimumSize = new Vector2(0, 120);

        var echoStyle = new StyleBoxFlat();
        echoStyle.BgColor = new Color(0.02f, 0.04f, 0.10f, 0.75f);
        echoStyle.BorderColor = new Color(0.1f, 0.4f, 0.7f, 0.4f);
        echoStyle.BorderWidthTop = 1;
        echoStyle.ContentMarginLeft = 16;
        echoStyle.ContentMarginRight = 16;
        echoStyle.ContentMarginTop = 12;
        echoStyle.ContentMarginBottom = 12;
        _echoPanel.AddThemeStyleboxOverride("panel", echoStyle);
        root.AddChild(_echoPanel);

        _echoTextDisplay = new RichTextLabel();
        _echoTextDisplay.BbcodeEnabled = true;
        _echoTextDisplay.FitContent = true;
        _echoTextDisplay.ScrollActive = false;
        _echoTextDisplay.VisibleCharacters = 0;
        _echoTextDisplay.AddThemeColorOverride("default_color", new Color(0.4f, 0.75f, 0.95f));
        _echoTextDisplay.AddThemeFontSizeOverride("normal_font_size", 18);
        _echoPanel.AddChild(_echoTextDisplay);

        _echoPanel.Visible = false;
```

Add the fields at the top of the class:

```csharp
    private PanelContainer _echoPanel;
    private RichTextLabel _echoTextDisplay;
    private bool _isEchoMode;
```

Add the public method:

```csharp
    public void ShowEchoMonologue(string text)
    {
        _isEchoMode = true;
        _fullText = text;
        _echoTextDisplay.Text = $"[i]{text}[/i]";
        _echoTextDisplay.VisibleCharacters = 0;
        _visibleChars = 0;
        _charTimer = 0;
        _isDisplaying = true;
        _cooldownFrames = 3;
        _echoPanel.Visible = true;
        _panel.Visible = false;
        GameLog.Event("Narrative", $"ECHO monologue: {(text.Length > 60 ? text[..60] + "..." : text)}");
    }
```

Update `Hide()` to handle both panels:

```csharp
    public new void Hide()
    {
        _panel.Visible = false;
        _echoPanel.Visible = false;
        _isDisplaying = false;
        _isEchoMode = false;
        _textDisplay.Text = "";
        _textDisplay.VisibleCharacters = 0;
        _echoTextDisplay.Text = "";
        _echoTextDisplay.VisibleCharacters = 0;
    }
```

Update `_Process` typewriter to drive the correct text display:

In the typewriter section of `_Process`, replace references to `_textDisplay` with a local var:

```csharp
        var display = _isEchoMode ? _echoTextDisplay : _textDisplay;
```

Use `display` instead of `_textDisplay` for `VisibleCharacters` assignment in the typewriter loop.

- [ ] **Step 2: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add scripts/narrative/NarrativeManager.cs
git commit -m "feat: add ECHO monologue with distinct cyan/blue panel styling"
```

---

## Task 3: Puzzle Gate in HotspotData + InteractionManager

**Files:**
- Modify: `scripts/interaction/HotspotData.cs`
- Modify: `scripts/interaction/InteractionManager.cs`

- [ ] **Step 1: Add puzzle fields to HotspotData.cs**

Add after line 22 (`EvidenceToDiscover`):

```csharp
    [Export] public bool RequiresPuzzle { get; set; }
    [Export] public string PuzzleOverride { get; set; } = "";
```

- [ ] **Step 2: Add puzzle gate flow to InteractionManager.cs**

The flow: if hotspot requires puzzle and not yet solved, launch puzzle. On completion, resume the action. Add a using at the top:

```csharp
using Signal.Minigame;
```

Replace the `ExecuteAction` method with a version that checks for puzzle gates:

```csharp
    private void ExecuteAction(HotspotData action)
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;

        // Puzzle gate: if required and not yet solved, launch puzzle first
        if (action.RequiresPuzzle)
        {
            string solvedFlag = $"solved_{action.FlagToSet}";
            if (!string.IsNullOrEmpty(solvedFlag) && !state.HasFlag(solvedFlag))
            {
                LaunchPuzzleGate(action, solvedFlag);
                return;
            }
        }

        ExecuteActionDirect(action);
    }

    private void ExecuteActionDirect(HotspotData action)
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;

        if (!string.IsNullOrEmpty(action.ItemToConsume))
            InventoryManager.Instance?.RemoveItem(action.ItemToConsume);

        if (!string.IsNullOrEmpty(action.FlagToSet))
        {
            state.SetFlag(action.FlagToSet);
            GameLog.FlagSet(action.FlagToSet);
        }

        if (!string.IsNullOrEmpty(action.EvidenceToDiscover))
            Evidence.EvidenceManager.Instance?.Discover(action.EvidenceToDiscover);

        if (!string.IsNullOrEmpty(action.ItemToGrant))
            InventoryManager.Instance?.AddItem(action.ItemToGrant);

        switch (action.Type)
        {
            case HotspotType.Examine:
            case HotspotType.PickUp:
                NarrativeManager.Instance?.ShowText(action.ExamineText);
                break;

            case HotspotType.Door:
                SceneLoader.Instance?.LoadScene(action.TargetScene, action.IsNewSection);
                break;

            case HotspotType.Terminal:
            case HotspotType.Narration:
                if (!string.IsNullOrEmpty(action.NarrativeEntryId))
                    NarrativeManager.Instance?.PlayEntry(action.NarrativeEntryId);
                else
                    NarrativeManager.Instance?.ShowText(action.ExamineText);
                break;
        }
    }

    private DecryptionPuzzleUI _activePuzzle;
    private HotspotData _pendingAction;

    private void LaunchPuzzleGate(HotspotData action, string solvedFlag)
    {
        _pendingAction = action;

        // Determine puzzle difficulty from game state
        int section = GameManager.Instance?.State != null ? 1 : 1; // TODO: get from current section
        // For now, use section 1 defaults. PuzzleOverride support added later.

        var puzzleLayer = new CanvasLayer();
        puzzleLayer.Layer = 15;
        puzzleLayer.Name = "PuzzleGateLayer";
        GetTree().Root.AddChild(puzzleLayer);

        _activePuzzle = new DecryptionPuzzleUI();
        _activePuzzle.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        puzzleLayer.AddChild(_activePuzzle);

        _activePuzzle.PuzzleCompleted += (guesses, time) =>
        {
            GameLog.Event("Puzzle", $"Gate solved: {solvedFlag} in {guesses} guesses, {time:F1}s");
            GameManager.Instance?.State?.SetFlag(solvedFlag);

            // Clean up puzzle
            puzzleLayer.QueueFree();
            _activePuzzle = null;

            // Resume the gated action
            if (_pendingAction != null)
            {
                var resumed = _pendingAction;
                _pendingAction = null;
                ExecuteActionDirect(resumed);
            }
        };

        _activePuzzle.PuzzleCancelled += () =>
        {
            puzzleLayer.QueueFree();
            _activePuzzle = null;
            _pendingAction = null;
        };

        _activePuzzle.StartPuzzle(section);
        GameLog.Event("Puzzle", $"Puzzle gate launched for: {action.FlagToSet}");
    }
```

- [ ] **Step 3: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add scripts/interaction/HotspotData.cs scripts/interaction/InteractionManager.cs
git commit -m "feat: add puzzle gate flow — RequiresPuzzle launches decryption before content"
```

---

## Task 4: ParallaxRoom Base Class

**Files:**
- Create: `scripts/room/ParallaxRoom.cs`

This is the largest file. It handles: 2D parallax layers, cursor-driven panning (both axes), hotspot placement across layers, critical-path ambient cues, scan reveal (Shift), dust particles, emergency lights, bioluminescence, entry narrative on first visit.

- [ ] **Step 1: Create ParallaxRoom.cs**

```csharp
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
public partial class ParallaxRoom : Node2D
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

        _bgLayer = CreateLayer("BgLayer", _roomDef.BgColor, offset);
        _midLayer = CreateLayer("MidLayer", _roomDef.MidColor, offset);
        _fgLayer = CreateLayer("FgLayer", _roomDef.FgColor, offset);

        // Fg layer should not block mouse input to mid/bg hotspots
        _fgLayer.MouseFilter = Control.MouseFilterEnum.Ignore;
    }

    private Control CreateLayer(string name, Color color, Vector2 baseOffset)
    {
        var layer = new Control();
        layer.Name = name;
        layer.Position = new Vector2(baseOffset.X, baseOffset.Y);
        layer.Size = _canvasSize;
        layer.MouseFilter = Control.MouseFilterEnum.Ignore;

        var bg = new ColorRect();
        bg.Size = _canvasSize;
        bg.Color = color;
        bg.MouseFilter = Control.MouseFilterEnum.Ignore;
        layer.AddChild(bg);

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
        if (@event is InputEventKey key)
        {
            if (key.Keycode == Key.Shift)
            {
                _scanActive = key.Pressed;
                GetViewport().SetInputAsHandled();
            }
        }
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
```

- [ ] **Step 2: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add scripts/room/ParallaxRoom.cs
git commit -m "feat: add ParallaxRoom base class — 2D parallax, scan, hotspots, atmosphere"
```

---

## Task 5: Room Registry + Section 1 Content

**Files:**
- Create: `scripts/room/RoomRegistry.cs`

- [ ] **Step 1: Create RoomRegistry.cs with Section 1 rooms**

```csharp
using System.Collections.Generic;
using Godot;
using Signal.Interaction;

namespace Signal.Room;

public static class RoomRegistry
{
    public static readonly Dictionary<string, RoomDefinition> Rooms = new();

    static RoomRegistry()
    {
        Register(CreatePressureLockControl());
        Register(CreateEquipmentStorage());
        Register(CreatePowerJunction());
    }

    private static void Register(RoomDefinition room) => Rooms[room.Id] = room;

    public static RoomDefinition Get(string id) =>
        Rooms.TryGetValue(id, out var room) ? room : null;

    // ── Section 1: Pressure Lock Bay ──────────────────────────────────────

    private static RoomDefinition CreatePressureLockControl() => new()
    {
        Id = "section1_pressure_control",
        DisplayName = "Pressure Lock Control",
        Section = 1,
        BgColor = new Color(0.04f, 0.06f, 0.12f),
        MidColor = new Color(0.06f, 0.08f, 0.14f),
        FgColor = new Color(0.03f, 0.04f, 0.08f),
        EntryNarrative = "Systems initializing. Platform damage detected. Restoration directive active.",
        DustCount = 6,
        HasEmergencyLights = true,
        HasViewport = true,
        Hotspots = new List<RoomHotspotDef>
        {
            new("main_terminal",
                new Vector2(1400, 650), new Vector2(180, 120), RoomLayer.Mid,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.Narration,
                    ExamineText = "NEREUS PLATFORM OS v4.2.1\nBoot sequence complete.\nECHO unit sensor suite: active. Diagnostic scan [Shift].\nCrew status: evacuated. Platform status: seismic damage detected.\nDirective: restore critical subsystems.",
                    FlagToSet = "read_boot_message",
                    EvidenceToDiscover = "nereus_boot_message"
                }),

            new("viewport",
                new Vector2(1440, 280), new Vector2(260, 260), RoomLayer.Bg,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Examine,
                    ExamineText = "Deep ocean. Bioluminescent organisms drift past the viewport. Hydrothermal vents glow in the distance. Peaceful.",
                    EvidenceToDiscover = "exterior_view"
                }),

            new("pressure_gauge",
                new Vector2(700, 500), new Vector2(100, 80), RoomLayer.Mid,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Examine,
                    ExamineText = "Pressure readings. Sections 2 through 4 show fluctuations at 02:14 UTC. The pattern is irregular — not consistent with a single seismic event."
                }),

            new("door_to_storage",
                new Vector2(2500, 600), new Vector2(120, 220), RoomLayer.Mid,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.Door,
                    TargetScene = "Section1_EquipmentStorage"
                }),
        }
    };

    private static RoomDefinition CreateEquipmentStorage() => new()
    {
        Id = "section1_equipment_storage",
        DisplayName = "Equipment Storage",
        Section = 1,
        BgColor = new Color(0.05f, 0.06f, 0.10f),
        MidColor = new Color(0.07f, 0.08f, 0.12f),
        FgColor = new Color(0.03f, 0.04f, 0.07f),
        EntryNarrative = "Storage bay. Dim lighting. Equipment racks line the walls.",
        DustCount = 4,
        HasEmergencyLights = true,
        HasViewport = false,
        Hotspots = new List<RoomHotspotDef>
        {
            new("locked_terminal",
                new Vector2(800, 550), new Vector2(160, 120), RoomLayer.Mid,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Terminal,
                    ExamineText = "Platform seismic sensors recorded a magnitude 4.2 event at 02:14 UTC. Origin point consistent with subsurface geological activity. No structural damage detected. Emergency systems responded within normal parameters.",
                    FlagToSet = "read_seismic_report",
                    EvidenceToDiscover = "seismic_report"
                },
                requiresPuzzle: true),

            new("equipment_locker",
                new Vector2(1600, 450), new Vector2(120, 140), RoomLayer.Mid,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Examine,
                    ExamineText = "Standard maintenance equipment. Wrenches, sealant, diagnostic cables. Everything in its place. Nothing unusual."
                }),

            new("keycard",
                new Vector2(1200, 900), new Vector2(80, 60), RoomLayer.Fg,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.PickUp,
                    ExamineText = "A keycard on the floor, half under the shelf. Power Junction access.",
                    ItemToGrant = "keycard_power_junction",
                    FlagToSet = "picked_up_power_keycard"
                }),

            new("emergency_kit",
                new Vector2(400, 800), new Vector2(100, 70), RoomLayer.Fg,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Examine,
                    ExamineText = "Emergency kit. Sealed, unopened. Nobody used it."
                }),

            new("door_to_control",
                new Vector2(200, 600), new Vector2(120, 220), RoomLayer.Mid,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.Door,
                    TargetScene = "Section1_PressureLockControl"
                }),

            new("door_to_power",
                new Vector2(2600, 600), new Vector2(120, 220), RoomLayer.Mid,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.Door,
                    TargetScene = "Section1_PowerJunction"
                },
                condition: new HotspotCondition { RequiredItem = "keycard_power_junction" }),
        }
    };

    private static RoomDefinition CreatePowerJunction() => new()
    {
        Id = "section1_power_junction",
        DisplayName = "Power Junction",
        Section = 1,
        BgColor = new Color(0.10f, 0.06f, 0.05f),
        MidColor = new Color(0.12f, 0.07f, 0.06f),
        FgColor = new Color(0.06f, 0.04f, 0.03f),
        EntryNarrative = "Warning lights active. This room runs hot. Exposed wiring on every surface.",
        DustCount = 3,
        HasEmergencyLights = true,
        HasViewport = false,
        Hotspots = new List<RoomHotspotDef>
        {
            new("power_console",
                new Vector2(1400, 550), new Vector2(200, 140), RoomLayer.Mid,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.Terminal,
                    ExamineText = "Hub power restored. Life support systems coming online. Section 2 pressure locks releasing.",
                    FlagToSet = "hub_power_restored"
                }),

            new("exposed_wiring",
                new Vector2(900, 350), new Vector2(140, 100), RoomLayer.Mid,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Examine,
                    ExamineText = "Repair work, interrupted. The cuts are clean — deliberate, not damage."
                }),

            new("warning_sign",
                new Vector2(1800, 180), new Vector2(160, 60), RoomLayer.Bg,
                criticalPath: false,
                action: new HotspotData
                {
                    Type = HotspotType.Examine,
                    ExamineText = "CAUTION: Pressure differential — seal doors before maintenance."
                }),

            new("door_to_storage",
                new Vector2(200, 600), new Vector2(120, 220), RoomLayer.Mid,
                criticalPath: true,
                action: new HotspotData
                {
                    Type = HotspotType.Door,
                    TargetScene = "Section1_EquipmentStorage"
                }),
        }
    };
}
```

- [ ] **Step 2: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add scripts/room/RoomRegistry.cs
git commit -m "feat: add RoomRegistry with Section 1 content (3 rooms, tutorial flow)"
```

---

## Task 6: Scene Files + Cleanup

**Files:**
- Create: 3 new `.tscn` scene files
- Delete: old room scripts and scenes
- Modify: `scripts/tests/AutoPlaytest.cs`, `project.godot`, `scenes/MainMenu.tscn`

- [ ] **Step 1: Create a ParallaxRoomLoader script**

A small helper script that scenes reference. It reads the room ID from metadata and initializes ParallaxRoom.

Create `scripts/room/ParallaxRoomLoader.cs`:

```csharp
using Godot;

namespace Signal.Room;

/// <summary>
/// Scene script that loads a RoomDefinition by ID and initializes ParallaxRoom.
/// Set the RoomId export in the scene or via code.
/// </summary>
public partial class ParallaxRoomLoader : ParallaxRoom
{
    [Export] public string RoomId { get; set; } = "";

    public override void _Ready()
    {
        var roomDef = RoomRegistry.Get(RoomId);
        if (roomDef == null)
        {
            Core.GameLog.Error("Room", $"RoomId not found in registry: {RoomId}");
            return;
        }
        Initialize(roomDef);
        base._Ready();
    }
}
```

- [ ] **Step 2: Create scene files**

Create `scenes/Section1_PressureLockControl.tscn`:
```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/room/ParallaxRoomLoader.cs" id="1"]

[node name="PressureLockControl" type="Node2D"]
script = ExtResource("1")
RoomId = "section1_pressure_control"
```

Create `scenes/Section1_EquipmentStorage.tscn`:
```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/room/ParallaxRoomLoader.cs" id="1"]

[node name="EquipmentStorage" type="Node2D"]
script = ExtResource("1")
RoomId = "section1_equipment_storage"
```

Create `scenes/Section1_PowerJunction.tscn`:
```
[gd_scene load_steps=2 format=3]

[ext_resource type="Script" path="res://scripts/room/ParallaxRoomLoader.cs" id="1"]

[node name="PowerJunction" type="Node2D"]
script = ExtResource("1")
RoomId = "section1_power_junction"
```

- [ ] **Step 3: Delete old room files**

```bash
git rm scripts/rooms/RoomBuilder.cs scripts/rooms/HubRoom1.cs scripts/rooms/HubRoom2.cs scripts/rooms/HubRoom3.cs scenes/Section1_Hub_Room1.tscn scenes/Section1_Hub_Room2.tscn scenes/Section1_Hub_Room3.tscn
```

- [ ] **Step 4: Update MainMenu.cs to use new starting scene**

In `scripts/ui/MainMenu.cs`, find where it references `Section1_Hub_Room1` and change to `Section1_PressureLockControl`.

- [ ] **Step 5: Strip room-navigation tests from AutoPlaytest.cs**

Read `scripts/tests/AutoPlaytest.cs`. Remove all steps that reference `HubRoom`, `Room1`, `Room2`, `Room3`, or navigate between rooms. Keep the initialization checks, save/load tests, flag tests, and ending evaluator tests. The room-specific tests were testing placeholder content that no longer exists.

- [ ] **Step 6: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 7: Commit**

```bash
git add scripts/room/ParallaxRoomLoader.cs scenes/Section1_PressureLockControl.tscn scenes/Section1_EquipmentStorage.tscn scenes/Section1_PowerJunction.tscn scripts/ui/MainMenu.cs scripts/tests/AutoPlaytest.cs
git commit -m "feat: Section 1 scenes, delete old placeholder rooms, update main menu + autotest"
```

---

## Task 7: Integration Verification

- [ ] **Step 1: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 2: Verify files**

```bash
ls scripts/room/ && echo "---" && ls scenes/Section1_*
```

Expected: `ParallaxRoom.cs`, `ParallaxRoomLoader.cs`, `RoomDefinition.cs`, `RoomRegistry.cs` + three new scene files.

- [ ] **Step 3: Verify old files removed**

```bash
ls scripts/rooms/ 2>/dev/null && echo "OLD ROOMS STILL EXIST" || echo "Old rooms cleaned up"
ls scenes/Section1_Hub_* 2>/dev/null && echo "OLD SCENES STILL EXIST" || echo "Old scenes cleaned up"
```

Expected: both say cleaned up.

- [ ] **Step 4: Run autotest**

Run: `./playtest.sh autotest`
Expected: System tests pass. No room-navigation test failures (they were removed).

- [ ] **Step 5: Manual smoke test**

Launch the game and verify:
1. Main menu → New Game loads Section1_PressureLockControl
2. Parallax panning works (move cursor, layers shift)
3. Entry narrative appears in ECHO monologue style (cyan italic)
4. Shift scan reveals optional hotspots
5. Main terminal hotspot works (critical path glow, click shows NEREUS text)
6. Door to Equipment Storage works
7. Decryption puzzle launches on locked terminal
8. Keycard pickup works
9. Keycard-gated door to Power Junction works
10. Power console sets flag
11. J key opens evidence web (any discovered evidence appears)

---

## Summary

| Task | Files | Description |
|------|-------|-------------|
| 1 | RoomDefinition.cs | Data classes for rooms + hotspots |
| 2 | NarrativeManager.cs | ECHO monologue with distinct cyan styling |
| 3 | HotspotData.cs, InteractionManager.cs | Puzzle gate flow (RequiresPuzzle) |
| 4 | ParallaxRoom.cs | Base class: parallax, scan, atmosphere, entry narrative |
| 5 | RoomRegistry.cs | Section 1 content (3 rooms, tutorial) |
| 6 | Scenes, cleanup, MainMenu, AutoPlaytest | Wire everything, delete old rooms |
| 7 | (verification) | Build, file check, autotest, manual smoke test |

**Dependencies:** Tasks 1→4→5 (data→base class→content). Task 2 independent. Task 3 independent. Task 6 depends on all. Task 7 depends on 6.

**Parallelization:** Tasks 1, 2, 3 can run in parallel. Task 4 needs 1. Task 5 needs 4. Tasks 6-7 sequential after all.
