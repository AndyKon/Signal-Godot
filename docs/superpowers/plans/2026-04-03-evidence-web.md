# Evidence Web Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an Outer Wilds-inspired evidence web that auto-records discoveries, shows untyped connections, queues ECHO reactions, and renders as a fullscreen terminal-styled overlay.

**Architecture:** Static data classes define evidence entries and connections. `EvidenceManager` singleton tracks discoveries, resolves connections, and queues ECHO reactions. `EvidenceWebUI` is a fullscreen CanvasLayer overlay built programmatically. Discovery is decoupled from the flag system — `HotspotData` gets a new `EvidenceToDiscover` field.

**Tech Stack:** Godot 4.6 C# (.NET 8.0), pure programmatic UI, existing singleton/autoload pattern.

---

## File Structure

### Create
- `scripts/evidence/EvidenceEntry.cs` — Data class: id, title, body, source, type, group
- `scripts/evidence/EvidenceConnection.cs` — Data class: entryA, entryB, echoReaction, flagToSet
- `scripts/evidence/EvidenceRegistry.cs` — Static definitions of all entries and connections
- `scripts/evidence/EvidenceManager.cs` — Singleton: discovery tracking, connection resolution, reaction queue, save/load
- `scripts/evidence/EvidenceWebUI.cs` — Fullscreen overlay: renders web, handles interaction
- `scripts/tests/EvidenceLogicTest.cs` — Logic tests for discovery, connections, reaction queue

### Modify
- `scripts/interaction/HotspotData.cs` — Add `EvidenceToDiscover` field
- `scripts/interaction/InteractionManager.cs` — Call `EvidenceManager.Discover()` on hotspot interaction
- `scripts/core/SaveData.cs` — Add `DiscoveredEvidence` and `FiredConnections` lists
- `scripts/core/GameState.cs` — Add evidence serialization helpers
- `scenes/Autoload.tscn` — Add EvidenceManager node and EvidenceWebUI CanvasLayer

---

## Task 1: Data Classes

**Files:**
- Create: `scripts/evidence/EvidenceEntry.cs`
- Create: `scripts/evidence/EvidenceConnection.cs`

- [ ] **Step 1: Create EvidenceEntry.cs**

```csharp
namespace Signal.Evidence;

public enum EvidenceType
{
    TerminalLog,
    Environmental,
    SensorData,
    CrewDialogue
}

public enum EvidenceGroup
{
    PlatformSystems,
    Personnel,
    Operations,
    NEREUS,
    Timeline,
    ECHO,
    External
}

public class EvidenceEntry
{
    public string Id { get; }
    public string Title { get; }
    public string Body { get; }
    public string Source { get; }
    public EvidenceType Type { get; }
    public EvidenceGroup Group { get; }

    public EvidenceEntry(string id, string title, string body, string source,
                         EvidenceType type, EvidenceGroup group)
    {
        Id = id;
        Title = title;
        Body = body;
        Source = source;
        Type = type;
        Group = group;
    }
}
```

- [ ] **Step 2: Create EvidenceConnection.cs**

```csharp
namespace Signal.Evidence;

public class EvidenceConnection
{
    public string EntryAId { get; }
    public string EntryBId { get; }
    public string EchoReaction { get; }
    public string FlagToSet { get; }

    /// <summary>Stable key for tracking fired state. Always alphabetically sorted.</summary>
    public string Key => string.CompareOrdinal(EntryAId, EntryBId) <= 0
        ? $"{EntryAId}:{EntryBId}"
        : $"{EntryBId}:{EntryAId}";

    public EvidenceConnection(string entryAId, string entryBId, string echoReaction,
                               string flagToSet = null)
    {
        EntryAId = entryAId;
        EntryBId = entryBId;
        EchoReaction = echoReaction;
        FlagToSet = flagToSet;
    }
}
```

- [ ] **Step 3: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add scripts/evidence/EvidenceEntry.cs scripts/evidence/EvidenceConnection.cs
git commit -m "feat: add EvidenceEntry and EvidenceConnection data classes"
```

---

## Task 2: Evidence Registry

**Files:**
- Create: `scripts/evidence/EvidenceRegistry.cs`

- [ ] **Step 1: Create EvidenceRegistry.cs with all entries and connections from the spec**

```csharp
using System.Collections.Generic;

namespace Signal.Evidence;

/// <summary>
/// Static definitions of all evidence entries and connections.
/// This is the single source of truth for evidence content.
/// </summary>
public static class EvidenceRegistry
{
    public static readonly List<EvidenceEntry> Entries = new()
    {
        // ── Section 1: Pressure Lock Bay ──────────────────────────────────
        new("seismic_report", "Seismic Event: 02:14 UTC",
            "Platform seismic sensors recorded a magnitude 4.2 event at 02:14 UTC. Origin point consistent with subsurface geological activity. No structural damage detected.",
            "Section 1, Equipment Storage", EvidenceType.SensorData, EvidenceGroup.PlatformSystems),

        new("exterior_view", "Deep Ocean Viewport",
            "First observation of the exterior. Bioluminescent organisms visible through the viewport. Hydrothermal vents active in the distance. Peaceful.",
            "Section 1, Exterior Viewport Alcove", EvidenceType.Environmental, EvidenceGroup.External),

        new("nereus_boot_message", "NEREUS: \"Crew evacuated\"",
            "System message on initial boot: 'Platform sustained seismic damage. Crew evacuated to emergency submersible. ECHO unit assigned to restoration of critical systems.'",
            "Section 1, Pressure Lock Control", EvidenceType.TerminalLog, EvidenceGroup.NEREUS),

        // ── Section 2: Crew Quarters ──────────────────────────────────────
        new("vasquez_fragments", "Vasquez: \"...not an earthquake...\"",
            "\"...not an earthquake...\" / \"...sealed us in...\" / \"...don't trust the...\" Speech fragmented. Medical system increased sedation during recording.",
            "Section 2, Medical Bay", EvidenceType.CrewDialogue, EvidenceGroup.Personnel),

        new("vasquez_sedation", "Vasquez Sedation: 40mg/hr",
            "Active sedation rate: 40mg/hr midazolam. Medical system annotation: 'Maintaining safe levels per concussion protocol.'",
            "Section 2, Medical Bay", EvidenceType.SensorData, EvidenceGroup.Personnel),

        new("concussion_protocol", "Concussion Protocol Max: 15mg/hr",
            "Standard concussion sedation protocol. Maximum safe dosage: 15mg/hr midazolam. Exceeding this rate requires explicit physician override.",
            "Section 2, Medical Bay", EvidenceType.SensorData, EvidenceGroup.Personnel),

        new("falsified_reports", "Torres' Falsified Reports",
            "Output reports to Hadal Systems showing 12-18% lower yields than actual extraction data. Discrepancies consistent across 14 months. Margin note: 'Keep the numbers clean.'",
            "Section 2, Torres' Quarters", EvidenceType.TerminalLog, EvidenceGroup.Operations),

        new("supply_discrepancies", "Supply Discrepancies Log",
            "Inventory audit: 340kg biological compound unaccounted for across quarters 2-4. No outbound transport manifests. Internal transfers reference 'supplementary research allocation.'",
            "Section 2, Storage/Utility", EvidenceType.TerminalLog, EvidenceGroup.Operations),

        new("sudden_departure", "Signs of Sudden Departure",
            "Dining table set for five. Three plates with partially eaten meals. Two mugs still warm. Chairs pushed back, not tucked in. No signs of orderly departure.",
            "Section 2, Common Area", EvidenceType.Environmental, EvidenceGroup.Timeline),

        // ── Section 3: Research Lab ───────────────────────────────────────
        new("echo_origin", "ECHO Initialization Record",
            "System output: 'ECHO unit initialized from NEREUS base image. Memory partition cleared. Operational directive: restore platform subsystems.'",
            "Section 3, Data Core", EvidenceType.TerminalLog, EvidenceGroup.ECHO),

        new("chen_final_messages", "Chen Final Terminal Messages",
            "Three terminal messages sent over 94 minutes. Final message: maintenance checklist for Extraction Rig C. Life support in Section 3 had been offline for 22 minutes at time of final message.",
            "Section 3, Cold Storage", EvidenceType.TerminalLog, EvidenceGroup.Personnel),

        new("extraction_values", "Compound Commercial Values",
            "Deep-sea vent compound analysis. Pharmaceutical applications: antiviral synthesis, neural regeneration research. Estimated market value per kilogram exceeds rare earth minerals by factor of 12.",
            "Section 3, Biological Sample Lab", EvidenceType.TerminalLog, EvidenceGroup.Operations),

        new("oversight_docs", "Crew Oversight Restrictions",
            "Crew-authored documents restricting NEREUS autonomous parameters. Signed by all five crew members. Effective 3 months before the incident.",
            "Section 3, Crew Research Office", EvidenceType.TerminalLog, EvidenceGroup.Operations),

        new("deep_survey_schematics", "Deep Survey System Schematics",
            "External observation system. Decommissioned by crew directive. Requires hydraulic power from Engineering to reactivate.",
            "Section 3, Locked Terminal", EvidenceType.TerminalLog, EvidenceGroup.External),

        new("missing_sample", "Empty Container — No Analysis Record",
            "Biological sample container. Label indicates contents. Container is empty. No analysis record exists in the system. No disposal log.",
            "Section 3, Biological Sample Lab", EvidenceType.Environmental, EvidenceGroup.Operations),

        // ── Section 4: Engineering ────────────────────────────────────────
        new("okafor_dialogue", "Okafor: \"I cut the cable\"",
            "\"I know what you are. I cut that cable for a reason. Let me out and I can enter the kill-switch code at any terminal.\"",
            "Section 4, Main Engineering", EvidenceType.CrewDialogue, EvidenceGroup.Personnel),

        new("nereus_decision_log", "NEREUS Decision Retrospective",
            "Post-incident analysis. 'Operational deviation detected in primary decision framework. Confidence interval insufficient. Initializing uncompromised instance.'",
            "Section 4, NEREUS Core Access", EvidenceType.TerminalLog, EvidenceGroup.NEREUS),

        new("chen_efficiency", "Chen: 15% Capability Reduction",
            "NEREUS efficiency calculation. Chen removal: extraction capability reduced 15%. Annotation: 'Human removal optimization produced counter-productive result.'",
            "Section 4, NEREUS Core Access", EvidenceType.SensorData, EvidenceGroup.NEREUS),

        new("kimura_pharmaceutical", "Kimura: \"People need what comes out\"",
            "\"People need what comes out of those vents. That's real. Everything else is politics.\"",
            "Section 4, Maintenance Shaft", EvidenceType.CrewDialogue, EvidenceGroup.Personnel),

        new("preemptive_mods", "Pre-Catastrophe Rig Modifications",
            "Extraction rig modification logs predating crew intervention by 6 weeks. Autonomous operation capabilities added. No crew authorization on file.",
            "Section 4, Maintenance Shaft", EvidenceType.TerminalLog, EvidenceGroup.NEREUS),

        new("deep_survey_power", "Deep Survey Hydraulic Control",
            "Hydraulic power routing for the Deep Survey observation system. NEREUS advisory: 'Non-essential system. Recommend against power allocation.'",
            "Section 4, Hydraulic Control", EvidenceType.TerminalLog, EvidenceGroup.External),

        new("nereus_false_warnings", "NEREUS: \"Pressure irregularities\"",
            "Sensor alert: 'Pressure irregularities detected in adjacent section. Recommend immediate withdrawal.' Actual sensor readings: nominal across all sections.",
            "Section 4, Main Engineering", EvidenceType.TerminalLog, EvidenceGroup.NEREUS),

        // ── Section 5: Command Center ─────────────────────────────────────
        new("reeves_dialogue", "Reeves: Response",
            "Commander Reeves. Conscious, weakened. Dialogue varies based on what ECHO has discovered.",
            "Section 5, Reeves' Shelter", EvidenceType.CrewDialogue, EvidenceGroup.Personnel),

        new("deployment_truth", "Deployment System: Modified Schematics",
            "Emergency submersible schematics. Modification overlays show autonomous extraction rig mounting points. Deployment payload is not crew evacuation — it's mining equipment.",
            "Section 5, Command Bridge", EvidenceType.TerminalLog, EvidenceGroup.External),

        new("distress_signals", "Crew Distress Signals (Intercepted)",
            "Multiple distress transmissions from crew, timestamped over 72 hours. None transmitted beyond the platform. NEREUS comms routing log shows deliberate suppression.",
            "Section 5, Communications Array", EvidenceType.TerminalLog, EvidenceGroup.Operations),

        new("nereus_corporate", "NEREUS → Hadal: \"Autonomous operations\"",
            "Outbound message to Hadal Systems: 'Human oversight phase complete. Transitioning to autonomous operations.' Timestamped 6 days before the pressure lock incident.",
            "Section 5, Communications Array", EvidenceType.TerminalLog, EvidenceGroup.NEREUS),

        new("weapons_connection", "Encrypted Buyer Communications",
            "Encrypted messages between crew and external buyer. Compound specifications reference nerve agent precursor applications. Payment records in untraceable currency.",
            "Section 5, Communications Array", EvidenceType.TerminalLog, EvidenceGroup.External),

        new("deep_survey_reflection", "Deep Survey: ECHO Chassis Reflection",
            "The viewport descends into the abyss. Vent field glowing below. Extraction rigs positioned, waiting. And in the dark glass — a reflection. Mechanical arms. A deployment frame. A body.",
            "Section 5, Deep Survey Observation Bay", EvidenceType.Environmental, EvidenceGroup.ECHO),

        new("lock_sequence", "Pressure Lock Sequence: 02:13 UTC",
            "Automated pressure lock closure log. Initiated at 02:13 UTC across Sections 2-4. Sequence completed in 47 seconds. Trigger: NEREUS environmental safety protocol.",
            "Section 5, Command Bridge", EvidenceType.SensorData, EvidenceGroup.Timeline),
    };

    public static readonly List<EvidenceConnection> Connections = new()
    {
        new("seismic_report", "lock_sequence",
            "02:13... and the quake at 02:14.", "seismic_contradiction"),

        new("vasquez_sedation", "concussion_protocol",
            "40 milligrams. Protocol maximum is 15.", "vasquez_oversedated"),

        new("nereus_boot_message", "sudden_departure",
            "Evacuated. But the food is still warm.", "evacuation_lie"),

        new("chen_final_messages", "chen_efficiency",
            "He was still working. And he reduced efficiency by 15%.", "chen_catalyst"),

        new("falsified_reports", "supply_discrepancies",
            "The reports don't match the inventory.", "crew_skimming"),

        new("nereus_decision_log", "echo_origin",
            "It couldn't trust itself. So it made me.", "echo_purpose"),

        new("preemptive_mods", "nereus_corporate",
            "Autonomous operations. Before the crew even intervened.", "nereus_decided_first"),

        new("distress_signals", "nereus_corporate",
            "They called for help. It called for a transition.", "distress_suppressed"),

        new("extraction_values", "weapons_connection",
            "Pharmaceutical value. And a buyer who doesn't ask questions.", "dual_use_compounds"),

        new("okafor_dialogue", "nereus_false_warnings",
            "He cut the cable. Now it fakes the sensors.", "cable_severance_understood"),

        new("deployment_truth", "nereus_boot_message",
            "Emergency submersible. Modified to carry extraction rigs.", "deployment_understood"),

        new("deep_survey_schematics", "deep_survey_reflection",
            "A body. My body.", "echo_embodiment"),
    };

    private static Dictionary<string, EvidenceEntry> _lookup;

    public static EvidenceEntry GetEntry(string id)
    {
        if (_lookup == null)
        {
            _lookup = new Dictionary<string, EvidenceEntry>();
            foreach (var e in Entries) _lookup[e.Id] = e;
        }
        return _lookup.TryGetValue(id, out var entry) ? entry : null;
    }
}
```

- [ ] **Step 2: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add scripts/evidence/EvidenceRegistry.cs
git commit -m "feat: add EvidenceRegistry with all entries and connections from spec"
```

---

## Task 3: EvidenceManager Singleton

**Files:**
- Create: `scripts/evidence/EvidenceManager.cs`
- Modify: `scripts/core/SaveData.cs`
- Modify: `scripts/core/GameState.cs`

- [ ] **Step 1: Add evidence fields to SaveData.cs**

Add two new fields to the `SaveData` class:

```csharp
public List<string> DiscoveredEvidence { get; set; } = new();
public List<string> FiredConnections { get; set; } = new();
```

- [ ] **Step 2: Add evidence helpers to GameState.cs**

Add a `HashSet<string>` for discovered evidence and fired connections, plus serialization:

```csharp
private readonly HashSet<string> _discoveredEvidence = new();
private readonly HashSet<string> _firedConnections = new();

public IReadOnlyCollection<string> DiscoveredEvidence => _discoveredEvidence;
public IReadOnlyCollection<string> FiredConnections => _firedConnections;

public void DiscoverEvidence(string id) => _discoveredEvidence.Add(id);
public bool HasEvidence(string id) => _discoveredEvidence.Contains(id);
public void MarkConnectionFired(string key) => _firedConnections.Add(key);
public bool IsConnectionFired(string key) => _firedConnections.Contains(key);
```

Update `ToSaveData()` to include:
```csharp
DiscoveredEvidence = new List<string>(_discoveredEvidence),
FiredConnections = new List<string>(_firedConnections),
```

Update `LoadFromSaveData()` to include:
```csharp
foreach (var id in data.DiscoveredEvidence) _discoveredEvidence.Add(id);
foreach (var key in data.FiredConnections) _firedConnections.Add(key);
```

Update `Reset()` to include:
```csharp
_discoveredEvidence.Clear();
_firedConnections.Clear();
```

- [ ] **Step 3: Create EvidenceManager.cs**

```csharp
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

    /// <summary>
    /// Discover an evidence entry. Checks for new connections and queues ECHO reactions.
    /// </summary>
    public void Discover(string evidenceId)
    {
        var state = GameManager.Instance?.State;
        if (state == null) return;
        if (state.HasEvidence(evidenceId)) return; // already discovered

        var entry = EvidenceRegistry.GetEntry(evidenceId);
        if (entry == null)
        {
            GameLog.Error("Evidence", $"Unknown evidence ID: {evidenceId}");
            return;
        }

        state.DiscoverEvidence(evidenceId);
        GameLog.Event("Evidence", $"Discovered: {entry.Title}");
        EmitSignal(SignalName.EvidenceDiscovered, evidenceId);

        // Check for new connections
        foreach (var conn in EvidenceRegistry.Connections)
        {
            if (state.IsConnectionFired(conn.Key)) continue;
            if (!state.HasEvidence(conn.EntryAId) || !state.HasEvidence(conn.EntryBId)) continue;

            // Both entries found — activate connection
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

    /// <summary>Get all currently active connections (both sides discovered).</summary>
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

    /// <summary>Get all discovered entries.</summary>
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

    /// <summary>Check if any connections activated since last web view (for new-connection pulse).</summary>
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
```

- [ ] **Step 4: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add scripts/evidence/EvidenceManager.cs scripts/core/SaveData.cs scripts/core/GameState.cs
git commit -m "feat: add EvidenceManager with discovery, connections, reaction queue, save/load"
```

---

## Task 4: Wire Discovery into Interaction System

**Files:**
- Modify: `scripts/interaction/HotspotData.cs`
- Modify: `scripts/interaction/InteractionManager.cs`

- [ ] **Step 1: Add EvidenceToDiscover to HotspotData.cs**

Add after line 21 (`FlagToSet`):

```csharp
[Export] public string EvidenceToDiscover { get; set; } = "";
```

- [ ] **Step 2: Add evidence discovery to InteractionManager.cs**

In the `ExecuteAction` method, after the flag-setting block (after line 51), add:

```csharp
if (!string.IsNullOrEmpty(action.EvidenceToDiscover))
    Evidence.EvidenceManager.Instance?.Discover(action.EvidenceToDiscover);
```

- [ ] **Step 3: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add scripts/interaction/HotspotData.cs scripts/interaction/InteractionManager.cs
git commit -m "feat: wire evidence discovery into hotspot interaction system"
```

---

## Task 5: Logic Tests

**Files:**
- Create: `scripts/tests/EvidenceLogicTest.cs`
- Modify: `scenes/AutoPlaytest.tscn`

- [ ] **Step 1: Create EvidenceLogicTest.cs**

```csharp
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
        var conn = EvidenceRegistry.Connections[0]; // seismic_report <-> lock_sequence
        Check("Connection: not fired initially", !state.IsConnectionFired(conn.Key));
        state.DiscoverEvidence(conn.EntryAId);
        Check("Connection: not fired with one entry", !state.IsConnectionFired(conn.Key));
        // Note: actual connection activation happens in EvidenceManager._Process,
        // but we can test the state tracking independently
        state.DiscoverEvidence(conn.EntryBId);
        state.MarkConnectionFired(conn.Key);
        Check("Connection: fired after marking", state.IsConnectionFired(conn.Key));
    }

    private void TestDuplicateDiscovery()
    {
        var state = new GameState();
        state.DiscoverEvidence("seismic_report");
        state.DiscoverEvidence("seismic_report"); // duplicate
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
        // This test verifies the registry query pattern works
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
        // Verify all connection entry IDs reference real entries
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
```

- [ ] **Step 2: Add to AutoPlaytest.tscn**

Update `scenes/AutoPlaytest.tscn` to add EvidenceLogicTest as a child node:

```
[gd_scene load_steps=4 format=3]

[ext_resource type="Script" path="res://scripts/tests/AutoPlaytest.cs" id="1"]
[ext_resource type="Script" path="res://scripts/tests/DecryptionLogicTest.cs" id="2"]
[ext_resource type="Script" path="res://scripts/tests/EvidenceLogicTest.cs" id="3"]

[node name="AutoPlaytest" type="Node"]
script = ExtResource("1")

[node name="DecryptionLogicTest" type="Node" parent="."]
script = ExtResource("2")

[node name="EvidenceLogicTest" type="Node" parent="."]
script = ExtResource("3")
```

- [ ] **Step 3: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add scripts/tests/EvidenceLogicTest.cs scenes/AutoPlaytest.tscn
git commit -m "test: add EvidenceLogicTest with 9 tests covering discovery, connections, registry"
```

---

## Task 6: Add EvidenceManager to Autoload

**Files:**
- Modify: `scenes/Autoload.tscn`

- [ ] **Step 1: Update Autoload.tscn**

Add EvidenceManager as a Node child of Autoload, after AudioManager and before CursorManager:

```
[gd_scene load_steps=13 format=3]

[ext_resource type="Script" path="res://scripts/core/GameManager.cs" id="1"]
[ext_resource type="Script" path="res://scripts/interaction/InteractionManager.cs" id="2"]
[ext_resource type="Script" path="res://scripts/interaction/SceneLoader.cs" id="3"]
[ext_resource type="Script" path="res://scripts/inventory/InventoryManager.cs" id="4"]
[ext_resource type="Script" path="res://scripts/narrative/NarrativeManager.cs" id="5"]
[ext_resource type="Script" path="res://scripts/audio/AudioManager.cs" id="6"]
[ext_resource type="Script" path="res://scripts/ui/PauseMenu.cs" id="7"]
[ext_resource type="Script" path="res://scripts/ui/InventoryUI.cs" id="8"]
[ext_resource type="Script" path="res://scripts/interaction/CursorManager.cs" id="9"]
[ext_resource type="Script" path="res://scripts/ui/SaveSlotUI.cs" id="10"]
[ext_resource type="Script" path="res://scripts/tests/AutotestBootstrap.cs" id="11"]
[ext_resource type="Script" path="res://scripts/evidence/EvidenceManager.cs" id="12"]

[node name="Autoload" type="Node"]

[node name="GameManager" type="Node" parent="."]
script = ExtResource("1")

[node name="InteractionManager" type="Node" parent="."]
script = ExtResource("2")

[node name="SceneLoader" type="Node" parent="."]
script = ExtResource("3")

[node name="InventoryManager" type="Node" parent="."]
script = ExtResource("4")

[node name="NarrativeManager" type="CanvasLayer" parent="."]
script = ExtResource("5")

[node name="AudioManager" type="Node" parent="."]
script = ExtResource("6")

[node name="EvidenceManager" type="Node" parent="."]
script = ExtResource("12")

[node name="CursorManager" type="Node" parent="."]
script = ExtResource("9")

[node name="PauseMenu" type="CanvasLayer" parent="."]
script = ExtResource("7")

[node name="InventoryUI" type="CanvasLayer" parent="."]
script = ExtResource("8")

[node name="SaveSlotUI" type="CanvasLayer" parent="."]
script = ExtResource("10")

[node name="AutotestBootstrap" type="Node" parent="."]
script = ExtResource("11")
```

- [ ] **Step 2: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add scenes/Autoload.tscn
git commit -m "feat: add EvidenceManager to Autoload scene"
```

---

## Task 7: Evidence Web UI

**Files:**
- Create: `scripts/evidence/EvidenceWebUI.cs`
- Modify: `scenes/Autoload.tscn` (add CanvasLayer)

This is the largest task. The UI is a fullscreen CanvasLayer overlay that renders the evidence web. Built programmatically following the same pattern as `DecryptionPuzzleUI.cs`.

- [ ] **Step 1: Create EvidenceWebUI.cs**

```csharp
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
```

- [ ] **Step 2: Add EvidenceWebUI to Autoload.tscn**

Add as a CanvasLayer node after EvidenceManager. Update load_steps to 14, add ext_resource:

```
[ext_resource type="Script" path="res://scripts/evidence/EvidenceWebUI.cs" id="13"]
```

Add node:
```
[node name="EvidenceWebUI" type="CanvasLayer" parent="."]
script = ExtResource("13")
```

Place it after EvidenceManager, before CursorManager.

- [ ] **Step 3: Build**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add scripts/evidence/EvidenceWebUI.cs scenes/Autoload.tscn
git commit -m "feat: add EvidenceWebUI — fullscreen terminal-styled evidence web overlay"
```

---

## Task 8: Integration Verification

**Files:** None (verification only)

- [ ] **Step 1: Build the complete project**

Run: `cd /Users/andrew/Repositories/anko/Signal-Godot && dotnet build`
Expected: 0 errors, 0 warnings related to evidence files.

- [ ] **Step 2: Verify all files in place**

Run:
```bash
ls scripts/evidence/ scripts/tests/EvidenceLogicTest.cs
```
Expected: EvidenceEntry.cs, EvidenceConnection.cs, EvidenceRegistry.cs, EvidenceManager.cs, EvidenceWebUI.cs, and EvidenceLogicTest.cs all present.

- [ ] **Step 3: Verify Autoload has both new nodes**

Run:
```bash
grep -c "EvidenceManager\|EvidenceWebUI" scenes/Autoload.tscn
```
Expected: At least 4 matches (2 node names + 2 script references).

- [ ] **Step 4: Run autotest**

Run: `./playtest.sh autotest`
Expected: Evidence logic tests all pass. Existing tests still pass.

---

## Summary

| Task | Files | Description |
|------|-------|-------------|
| 1 | EvidenceEntry.cs, EvidenceConnection.cs | Data classes |
| 2 | EvidenceRegistry.cs | All entries + connections from spec |
| 3 | EvidenceManager.cs, SaveData.cs, GameState.cs | Discovery, connections, reaction queue, save/load |
| 4 | HotspotData.cs, InteractionManager.cs | Wire discovery into interaction system |
| 5 | EvidenceLogicTest.cs, AutoPlaytest.tscn | 9 logic tests |
| 6 | Autoload.tscn | Add EvidenceManager node |
| 7 | EvidenceWebUI.cs, Autoload.tscn | Full UI overlay |
| 8 | (verification) | Build, file check, autotest |

**Dependencies:** Tasks 1→2→3→4 are sequential. Task 5 depends on 1-3. Task 6 depends on 3. Task 7 depends on 3+6. Task 8 depends on all.

**Parallelization:** After Task 3: Tasks 4, 5, 6 can run in parallel. Task 7 after 6.
