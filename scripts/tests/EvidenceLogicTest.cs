using Godot;
using Signal.Core;
using Signal.Evidence;

namespace Signal.Tests;

public partial class EvidenceLogicTest : Node
{
    private int _passed;
    private int _failed;

    public override void _Ready()
    {
        GD.Print("=== EVIDENCE LOGIC TESTS ===");

        TestEntryLookup();
        TestConnectionKey();
        TestDiscovery();
        TestConnectionActivation();
        TestDuplicateDiscovery();
        TestConnectionFlagSetting();
        TestGetDiscoveredEntries();
        TestGetActiveConnections();
        TestRegistryCompleteness();

        GD.Print($"=== RESULTS: {_passed} passed, {_failed} failed ===");
    }

    private void Check(string name, bool condition)
    {
        if (condition) { _passed++; GD.Print($"  PASS: {name}"); }
        else { _failed++; GD.PrintErr($"  FAIL: {name}"); }
    }

    private void TestEntryLookup()
    {
        var entry = EvidenceRegistry.GetEntry("seismic_report");
        Check("EntryLookup: seismic_report exists", entry != null);
        Check("EntryLookup: correct title", entry?.Title == "Seismic Event: 02:14 UTC");
        Check("EntryLookup: correct group", entry?.Group == EvidenceGroup.PlatformSystems);
        Check("EntryLookup: unknown returns null", EvidenceRegistry.GetEntry("nonexistent") == null);
    }

    private void TestConnectionKey()
    {
        var conn = new EvidenceConnection("b_entry", "a_entry", "test");
        Check("ConnectionKey: alphabetically sorted", conn.Key == "a_entry:b_entry");
        var conn2 = new EvidenceConnection("a_entry", "b_entry", "test");
        Check("ConnectionKey: same regardless of order", conn.Key == conn2.Key);
    }

    private void TestDiscovery()
    {
        var state = new GameState();
        Check("Discovery: not discovered initially", !state.HasEvidence("seismic_report"));
        state.DiscoverEvidence("seismic_report");
        Check("Discovery: discovered after call", state.HasEvidence("seismic_report"));
    }

    private void TestConnectionActivation()
    {
        var state = new GameState();
        var conn = EvidenceRegistry.Connections[0];
        Check("Connection: not fired initially", !state.IsConnectionFired(conn.Key));
        state.DiscoverEvidence(conn.EntryAId);
        Check("Connection: not fired with one entry", !state.IsConnectionFired(conn.Key));
        state.DiscoverEvidence(conn.EntryBId);
        state.MarkConnectionFired(conn.Key);
        Check("Connection: fired after marking", state.IsConnectionFired(conn.Key));
    }

    private void TestDuplicateDiscovery()
    {
        var state = new GameState();
        state.DiscoverEvidence("seismic_report");
        state.DiscoverEvidence("seismic_report");
        int count = 0;
        foreach (var _ in state.DiscoveredEvidence) count++;
        Check("DuplicateDiscovery: only counted once", count == 1);
    }

    private void TestConnectionFlagSetting()
    {
        var conn = EvidenceRegistry.Connections[0];
        Check("ConnectionFlag: has flag to set", !string.IsNullOrEmpty(conn.FlagToSet));
        Check("ConnectionFlag: flag is seismic_contradiction", conn.FlagToSet == "seismic_contradiction");
    }

    private void TestGetDiscoveredEntries()
    {
        var state = new GameState();
        state.DiscoverEvidence("seismic_report");
        state.DiscoverEvidence("vasquez_fragments");
        int count = 0;
        foreach (var entry in EvidenceRegistry.Entries)
        {
            if (state.HasEvidence(entry.Id)) count++;
        }
        Check("GetDiscovered: finds 2 entries", count == 2);
    }

    private void TestGetActiveConnections()
    {
        var state = new GameState();
        state.DiscoverEvidence("seismic_report");
        state.DiscoverEvidence("lock_sequence");
        int activeCount = 0;
        foreach (var conn in EvidenceRegistry.Connections)
        {
            if (state.HasEvidence(conn.EntryAId) && state.HasEvidence(conn.EntryBId))
                activeCount++;
        }
        Check("ActiveConnections: 1 active after discovering both", activeCount == 1);
    }

    private void TestRegistryCompleteness()
    {
        bool allValid = true;
        foreach (var conn in EvidenceRegistry.Connections)
        {
            if (EvidenceRegistry.GetEntry(conn.EntryAId) == null)
            { allValid = false; GD.PrintErr($"  Missing entry: {conn.EntryAId}"); }
            if (EvidenceRegistry.GetEntry(conn.EntryBId) == null)
            { allValid = false; GD.PrintErr($"  Missing entry: {conn.EntryBId}"); }
        }
        Check("RegistryCompleteness: all connection IDs reference valid entries", allValid);
        Check("RegistryCompleteness: has entries", EvidenceRegistry.Entries.Count > 0);
        Check("RegistryCompleteness: has connections", EvidenceRegistry.Connections.Count > 0);
    }
}
