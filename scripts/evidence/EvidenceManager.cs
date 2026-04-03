using System.Collections.Generic;
using Godot;
using Signal.Core;
using Signal.Narrative;

namespace Signal.Evidence;

public partial class EvidenceManager : Node
{
    public static EvidenceManager Instance { get; private set; }

    private readonly Queue<string> _reactionQueue = new();

    [Signal] public delegate void EvidenceDiscoveredEventHandler(string evidenceId);
    [Signal] public delegate void ConnectionActivatedEventHandler(string entryAId, string entryBId);

    public override void _Ready()
    {
        Instance = this;
        GameLog.ManagerReady("EvidenceManager");
    }

    public void Discover(string evidenceId)
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;
        if (state.HasEvidence(evidenceId)) return;

        var entry = EvidenceRegistry.GetEntry(evidenceId);
        if (entry == null)
        {
            GameLog.Error("Evidence", $"Unknown evidence ID: {evidenceId}");
            return;
        }

        state.DiscoverEvidence(evidenceId);
        GameLog.Event("Evidence", $"Discovered: {entry.Title}");
        EmitSignal(SignalName.EvidenceDiscovered, evidenceId);

        foreach (var conn in EvidenceRegistry.Connections)
        {
            if (state.IsConnectionFired(conn.Key)) continue;
            if (!state.HasEvidence(conn.EntryAId) || !state.HasEvidence(conn.EntryBId)) continue;

            state.MarkConnectionFired(conn.Key);
            GameLog.Event("Evidence", $"Connection: {conn.EntryAId} <-> {conn.EntryBId}");

            if (!string.IsNullOrEmpty(conn.FlagToSet))
            {
                state.SetFlag(conn.FlagToSet);
                GameLog.FlagSet(conn.FlagToSet);
            }

            if (!string.IsNullOrEmpty(conn.EchoReaction))
                _reactionQueue.Enqueue(conn.EchoReaction);

            EmitSignal(SignalName.ConnectionActivated, conn.EntryAId, conn.EntryBId);
        }
    }

    public override void _Process(double delta)
    {
        if (_reactionQueue.Count == 0) return;
        if (NarrativeManager.Instance?.IsDisplaying == true) return;

        string reaction = _reactionQueue.Dequeue();
        NarrativeManager.Instance?.ShowText(reaction);
        GameLog.Event("Evidence", $"ECHO reaction: {reaction}");
    }

    public List<EvidenceConnection> GetActiveConnections()
    {
        var state = GameManager.Instance?.State;
        if (state == null) return new List<EvidenceConnection>();

        var active = new List<EvidenceConnection>();
        foreach (var conn in EvidenceRegistry.Connections)
        {
            if (state.HasEvidence(conn.EntryAId) && state.HasEvidence(conn.EntryBId))
                active.Add(conn);
        }
        return active;
    }

    public List<EvidenceEntry> GetDiscoveredEntries()
    {
        var state = GameManager.Instance?.State;
        if (state == null) return new List<EvidenceEntry>();

        var entries = new List<EvidenceEntry>();
        foreach (var entry in EvidenceRegistry.Entries)
        {
            if (state.HasEvidence(entry.Id))
                entries.Add(entry);
        }
        return entries;
    }

    public bool HasNewConnections(HashSet<string> lastSeenConnections)
    {
        var state = GameManager.Instance?.State;
        if (state == null) return false;

        foreach (var key in state.FiredConnections)
        {
            if (!lastSeenConnections.Contains(key)) return true;
        }
        return false;
    }
}
