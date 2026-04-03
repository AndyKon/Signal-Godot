# Evidence Web — Design Spec

## Overview

A persistent, fullscreen evidence log inspired by Outer Wilds' ship log. Automatically records discoveries, shows relationships between evidence, and lets the player review everything they've found. Organized by narrative topic, not location. The player draws conclusions — the system only shows what's related, never why.

Framed as ECHO's internal memory correlation system. Available anytime via keypress, pauses game and timer.

## Data Model

### EvidenceEntry

Static data defining a single piece of discoverable evidence.

| Field | Type | Description |
|-------|------|-------------|
| Id | string | Unique identifier, e.g. `"seismic_report"` |
| Title | string | Short display text for the web node, e.g. "Seismic Event: 02:14 UTC" |
| Body | string | Full content the player reads in the detail panel (1-3 sentences) |
| Source | string | Where it was found, e.g. "Section 1, Equipment Storage" — metadata, not used for grouping |
| Type | enum | `TerminalLog`, `Environmental`, `SensorData`, `CrewDialogue` |
| Group | enum | `PlatformSystems`, `Personnel`, `Operations`, `NEREUS`, `Timeline`, `ECHO`, `External` |

### EvidenceConnection

Static data defining a relationship between two entries.

| Field | Type | Description |
|-------|------|-------------|
| EntryAId | string | First evidence ID |
| EntryBId | string | Second evidence ID |
| EchoReaction | string | One-line factual monologue ECHO speaks when connection activates, e.g. "02:13... and the quake at 02:14." |
| FlagToSet | string (optional) | Game flag set when connection activates, e.g. `"seismic_contradiction"` |

### Discovery State

`EvidenceManager` tracks a `HashSet<string>` of discovered evidence IDs. This is serialized alongside `GameState` in save data. Connections are not tracked — they're derived at runtime from which entries have been discovered.

## Evidence Discovery

Evidence discovery is **independent from the flag system**. They are parallel actions triggered by game events.

`HotspotData` gets a new field: `EvidenceToDiscover` (string, optional). When a hotspot is interacted with, `InteractionManager`:
1. Calls `GameState.SetFlag()` if `FlagToSet` is set
2. Calls `EvidenceManager.Discover()` if `EvidenceToDiscover` is set

Either, both, or neither can be set per hotspot. They don't depend on each other.

When a connection's `FlagToSet` is defined, `EvidenceManager` calls `GameState.SetFlag()` when the connection activates — evidence drives flags, not the reverse.

## Connection Activation

When `EvidenceManager.Discover(id)` is called:
1. Add `id` to the discovered set
2. Scan all connections involving `id`
3. For each connection where both entries are now discovered:
   - If connection has a `FlagToSet`, call `GameState.SetFlag()`
   - Queue the ECHO reaction for playback

Connections activate exactly once. The manager tracks which connections have fired.

## ECHO Reactions

Reactions are queued, not immediate. `EvidenceManager` maintains a `Queue<string>` of pending reaction texts.

Each frame, if the queue is non-empty and `NarrativeManager.IsDisplaying` is false:
- Pop the next reaction
- Play it via `NarrativeManager.ShowText()`

If multiple connections activate simultaneously (one discovery completes two connections), reactions play in sequence. Short gap between them handled by `NarrativeManager`'s existing dismiss-before-next behavior.

Reactions play during exploration — never inside the evidence web UI (game is paused while the web is open).

## Evidence Web UI

### Access

Dedicated key (configurable, default `J`) opens the web as a fullscreen `CanvasLayer` overlay. Game pauses (`GetTree().Paused = true`), timer pauses. Same key closes. Styled as ECHO's internal memory system.

### Layout

**Header:** `> ECHO MEMORY // DATA CORRELATION [ N RECORDS / M CONNECTIONS ]`

**Canvas area:** Topic groups displayed as labeled clusters, positioned in a fixed layout. Within each cluster, discovered entries appear as rectangular nodes showing the entry title. Groups are spaced to leave room for cross-group connection lines.

**Group positions are fixed** — each group always occupies the same region of the canvas regardless of how many entries are discovered. This keeps the layout stable as the player discovers more.

**Connection lines:** Thin, dim, untyped curved lines between related nodes. All connections look the same — no color coding, no labels. The player reads both entries to understand the relationship.

**Detail panel:** Right-side panel slides in when a node is clicked. Shows: evidence type label, title, source location, separator, full body text. Terminal readout aesthetic.

### Interaction

1. Open web — see all discovered entries organized by group
2. Click a node — detail panel opens, that node's connections highlight (brighter lines), unrelated nodes dim
3. Click the same node or click empty space — deselect, everything returns to normal
4. Close web — return to game

No drag. No rearrange. No manual annotation. No search (v1). Read-only reference tool.

### Node Appearance

- Rectangular card with title text in terminal green
- Small colored pip indicating evidence type (green=terminal, cyan=environmental, amber=sensor, pink=dialogue)
- Subtle pulse animation on nodes with connections discovered since the player last opened the web (new connection indicator)
- Selected node: brighter border, subtle glow
- Dimmed state: low opacity when another node is selected and this one isn't connected

### Visual Design

Terminal aesthetic matching the decryption puzzle:
- Black background (`#000004`)
- Neon green text and borders (`#00e664` primary, `rgba(0,180,80,0.3)` borders)
- Monospace font throughout
- Detail panel: darker background, green text, terminal readout feel
- Group labels: dim green, uppercase, letterspaced

## Topic Groups

| Enum Value | Display Label | Contains |
|-----------|---------------|----------|
| PlatformSystems | PLATFORM SYSTEMS | Seismic data, pressure locks, power systems, environmental readings |
| Personnel | PERSONNEL | Crew members, medical data, dialogue, personal logs |
| Operations | OPERATIONS | Reports, supply records, extraction data, communications |
| NEREUS | NEREUS | System logs, directives, decision records |
| Timeline | TIMELINE | Timestamps, event sequences, sensor recordings |
| ECHO | ECHO | Initialization data, system identity, hardware records |
| External | EXTERNAL | Vent site data, deployment systems, outbound transmissions |

## Evidence Entries (Initial Set)

Derived from the story spec's flags and content. Each section's discoverable content mapped to entries:

### Section 1 — Pressure Lock Bay

| ID | Title | Type | Group |
|----|-------|------|-------|
| `seismic_report` | Seismic Event: 02:14 UTC | SensorData | PlatformSystems |
| `exterior_view` | Deep Ocean Viewport | Environmental | External |
| `nereus_boot_message` | NEREUS: "Crew evacuated" | TerminalLog | NEREUS |

### Section 2 — Crew Quarters

| ID | Title | Type | Group |
|----|-------|------|-------|
| `vasquez_fragments` | Vasquez: "...not an earthquake..." | CrewDialogue | Personnel |
| `vasquez_sedation` | Vasquez Sedation: 40mg/hr | SensorData | Personnel |
| `falsified_reports` | Torres' Falsified Reports | TerminalLog | Operations |
| `supply_discrepancies` | Supply Discrepancies Log | TerminalLog | Operations |
| `sudden_departure` | Common Area: Signs of Sudden Departure | Environmental | Timeline |
| `concussion_protocol` | Concussion Protocol Max: 15mg/hr | SensorData | Personnel |

### Section 3 — Research Lab

| ID | Title | Type | Group |
|----|-------|------|-------|
| `echo_origin` | ECHO Initialization Record | TerminalLog | ECHO |
| `chen_final_messages` | Chen Final Terminal Messages | TerminalLog | Personnel |
| `extraction_values` | Compound Commercial Values | TerminalLog | Operations |
| `oversight_docs` | Crew Oversight Restrictions | TerminalLog | Operations |
| `deep_survey_schematics` | Deep Survey System Schematics | TerminalLog | External |
| `missing_sample` | Empty Container — No Analysis Record | Environmental | Operations |

### Section 4 — Engineering

| ID | Title | Type | Group |
|----|-------|------|-------|
| `okafor_dialogue` | Okafor: "I cut the cable" | CrewDialogue | Personnel |
| `nereus_decision_log` | NEREUS Decision Retrospective | TerminalLog | NEREUS |
| `chen_efficiency` | Chen: 15% Capability Reduction | SensorData | NEREUS |
| `kimura_pharmaceutical` | Kimura: "People need what comes out" | CrewDialogue | Personnel |
| `preemptive_mods` | Pre-Catastrophe Rig Modifications | TerminalLog | NEREUS |
| `deep_survey_power` | Deep Survey Hydraulic Control | TerminalLog | External |
| `nereus_false_warnings` | NEREUS: "Pressure irregularities" | TerminalLog | NEREUS |

### Section 5 — Command Center

| ID | Title | Type | Group |
|----|-------|------|-------|
| `reeves_dialogue` | Reeves: Scaled Response | CrewDialogue | Personnel |
| `deployment_truth` | Deployment System: Modified Schematics | TerminalLog | External |
| `distress_signals` | Crew Distress Signals (Intercepted) | TerminalLog | Operations |
| `nereus_corporate` | NEREUS → Hadal: "Autonomous operations" | TerminalLog | NEREUS |
| `weapons_connection` | Encrypted Buyer Communications | TerminalLog | External |
| `deep_survey_reflection` | Deep Survey: ECHO Chassis Reflection | Environmental | ECHO |
| `lock_sequence` | Pressure Lock Sequence: 02:13 UTC | SensorData | Timeline |

## Connections (Initial Set)

| Entry A | Entry B | ECHO Reaction | Flag Set |
|---------|---------|---------------|----------|
| `seismic_report` | `lock_sequence` | "02:13... and the quake at 02:14." | `seismic_contradiction` |
| `vasquez_sedation` | `concussion_protocol` | "40 milligrams. Protocol maximum is 15." | `vasquez_oversedated` |
| `nereus_boot_message` | `sudden_departure` | "Evacuated. But the food is still warm." | `evacuation_lie` |
| `chen_final_messages` | `chen_efficiency` | "He was still working. And he reduced efficiency by 15%." | `chen_catalyst` |
| `falsified_reports` | `supply_discrepancies` | "The reports don't match the inventory." | `crew_skimming` |
| `nereus_decision_log` | `echo_origin` | "It couldn't trust itself. So it made me." | `echo_purpose` |
| `preemptive_mods` | `nereus_corporate` | "Autonomous operations. Before the crew even intervened." | `nereus_decided_first` |
| `distress_signals` | `nereus_corporate` | "They called for help. It called for a transition." | `distress_suppressed` |
| `extraction_values` | `weapons_connection` | "Pharmaceutical value. And a buyer who doesn't ask questions." | `dual_use_compounds` |
| `okafor_dialogue` | `nereus_false_warnings` | "He cut the cable. Now it fakes the sensors." | `cable_severance_understood` |
| `deployment_truth` | `nereus_boot_message` | "Emergency submersible. Modified to carry extraction rigs." | `deployment_understood` |
| `deep_survey_schematics` | `deep_survey_reflection` | "A body. My body." | `echo_embodiment` |

## File Structure

| File | Responsibility |
|------|---------------|
| `scripts/evidence/EvidenceEntry.cs` | Data class — id, title, body, source, type, group |
| `scripts/evidence/EvidenceConnection.cs` | Data class — entryA, entryB, echoReaction, flagToSet |
| `scripts/evidence/EvidenceRegistry.cs` | Static definitions of all entries and connections |
| `scripts/evidence/EvidenceManager.cs` | Singleton — discovery tracking, connection resolution, reaction queue |
| `scripts/evidence/EvidenceWebUI.cs` | Fullscreen overlay — renders web, handles interaction |
| `scripts/interaction/HotspotData.cs` | Modified — add `EvidenceToDiscover` field |
| `scripts/interaction/InteractionManager.cs` | Modified — call `EvidenceManager.Discover()` on hotspot interaction |
| `scripts/core/SaveData.cs` | Modified — add discovered evidence IDs and fired connection IDs |

## Save/Load Integration

`SaveData` gains two new fields:
- `DiscoveredEvidence` — `List<string>` of evidence IDs
- `FiredConnections` — `List<string>` of connection keys (e.g. `"seismic_report:lock_sequence"`)

`EvidenceManager` serializes/deserializes these alongside `GameState`.

## What This Does NOT Include

- No search or filter (v1 — add if entry count exceeds ~30)
- No manual annotation or player notes
- No completion tracking or percentage
- No indication of undiscovered entries or connections
- No drag/rearrange of nodes
- No zoom/pan (fixed layout, scrollable if needed)
- No ECHO reactions inside the web UI (game is paused)
