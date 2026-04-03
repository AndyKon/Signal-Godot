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
