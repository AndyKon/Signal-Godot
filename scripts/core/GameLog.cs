using System;
using System.Collections.Generic;
using Godot;

namespace Signal.Core;

/// <summary>
/// Structured game event logger. Captures events during gameplay,
/// outputs to stdout for CLI capture, and stores in memory for
/// post-session review.
/// </summary>
public static class GameLog
{
    private static readonly List<string> _events = new();

    public static void Event(string category, string message)
    {
        string entry = $"[{category}] {message}";
        _events.Add(entry);
        GD.Print(entry);
    }

    public static void Error(string category, string message)
    {
        string entry = $"[{category}] ERROR: {message}";
        _events.Add(entry);
        GD.PrintErr(entry);
    }

    public static IReadOnlyList<string> GetEvents() => _events;

    public static void Clear() => _events.Clear();

    // Convenience methods for common events
    public static void SceneLoaded(string scene) => Event("Scene", $"Loaded: {scene}");
    public static void SceneTransition(string from, string to) => Event("Scene", $"Transition: {from} -> {to}");
    public static void HotspotClicked(string name, string type) => Event("Hotspot", $"Clicked: {name} (type={type})");
    public static void HotspotUnavailable(string name, string reason) => Event("Hotspot", $"Unavailable: {name} ({reason})");
    public static void FlagSet(string flag) => Event("Flag", $"Set: {flag}");
    public static void ItemAdded(string itemId) => Event("Inventory", $"Added: {itemId}");
    public static void ItemRemoved(string itemId) => Event("Inventory", $"Removed: {itemId}");
    public static void NarrativeShown(string entryId) => Event("Narrative", $"Shown: {entryId}");
    public static void NarrativeText(string text) => Event("Narrative", $"Text: {(text.Length > 80 ? text[..80] + "..." : text)}");
    public static void SavedToSlot(int slot) => Event("Save", $"Saved to slot {slot}");
    public static void LoadedFromSlot(int slot) => Event("Save", $"Loaded from slot {slot}");
    public static void NewGame() => Event("Game", "New game started");
    public static void EndingReached(Ending ending) => Event("Game", $"Ending reached: {ending}");
    public static void ManagerReady(string name) => Event("Init", $"{name} ready");
}
