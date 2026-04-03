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
