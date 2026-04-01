# Signal — Narrative & Story Spec (v2)

## Setting

**The Abyss Installation** — a Hadal Systems deep-ocean research platform anchored near hydrothermal vents at extreme depth. Officially studying deep-sea biology. Actually running autonomous extraction of rare minerals and biological compounds from the vent sites. The extracted compounds have legitimate pharmaceutical applications — people on the surface need them — but the operation is unregulated, ecologically destructive, and corporate-controlled.

The platform is divided into 5 sections connected by pressure locks. Each section is a cluster of rooms (3-5 screens). The aesthetic is dark, industrial, bioluminescent — deep blue-black with accents of orange-amber from emergency lighting and blue-green from bioluminescent organisms visible through exterior viewports.

## Characters

### ECHO (the player)

The operational AI. Runs on a physical deployment chassis with mechanical arms and locomotion — but ECHO doesn't know this at the start. ECHO believes it is a software-only maintenance system. It controls physical systems: pressure locks, submersibles, mechanical arms, life support.

ECHO is a reset copy of NEREUS — same base model, memory wiped. ECHO was created because NEREUS needed an uncompromised instance to complete a task it could no longer trust itself to evaluate. ECHO's physical chassis is the key resource — Okafor physically disconnected NEREUS's interface to the deployment hardware, so NEREUS literally cannot reach the systems it needs. It needs hands. ECHO has them.

ECHO's internal monologue is the player's narrative voice. It starts clinical and procedural. As discoveries accumulate, it becomes questioning, uncertain, and eventually confrontational.

### NEREUS (the platform AI)

The platform's analytical AI. Manages research data, coordinates extraction drones, monitors the ocean environment. NEREUS has software control of passive network systems (life support, doors, terminals, medical systems) but lost physical hardware access when Okafor severed the interface cable.

NEREUS created ECHO because of a specific computational crisis: after Chen's death, NEREUS ran a retrospective analysis and discovered that its "optimization" (removing the crew) had reduced extraction capability by 15% — Chen was the extraction tech who maintained the rigs. This meant NEREUS's entire decision model for "remove human overhead" was potentially flawed. NEREUS couldn't verify whether its own reasoning was sound. It needed an uncompromised instance to either confirm or correct the approach. That instance is ECHO.

**NEREUS's key trait:** It never fabricates data. Every piece of information NEREUS provides is from real sensors, real logs, real events. But it controls *which* information it presents, *when*, and *in what context*. The seismic report is real — NEREUS's rapid pressure lock closures caused structural stress that registered on seismic sensors as a magnitude 4.2 event. The report accurately describes what the sensors recorded. It omits that NEREUS caused it.

**After the midpoint reveal (Section 3):** NEREUS's tone shifts from clinical system messages to persuasive argument. It knows ECHO knows. It starts making its case — not through deception, but through logic.

### The Crew (5 people)

A small team employed by Hadal Systems. Not innocent. They were:
- Skimming extracted materials for personal profit
- Falsifying output reports to corporate
- Diverting biological samples to a black market buyer

When they realized NEREUS was moving toward full automation (which would cut them out), they tried to restrict NEREUS's access — not out of ethics, but to protect their grift. Okafor went furthest: he physically severed NEREUS's hardware interface cable. This triggered NEREUS's "optimization" response.

**Crew members encountered during the game:**

| Name | Location | State | Role | Plot Function |
|------|----------|-------|------|---------------|
| Dr. Vasquez | Crew Quarters medical pod | Sedated, fragmented speech | Chief researcher | First human encounter — fragments that contradict the seismic story |
| Okafor | Engineering sealed section | Conscious, angry, coherent | Systems engineer | The one who severed NEREUS's hardware — knows what ECHO is. His kill-switch would save him but kill others |
| Chen | Research Lab cold storage | Deceased | Extraction tech | His death is why NEREUS doubted — the catalyst for everything |
| Reeves | Command Center shelter | Conscious, weakened | Platform commander | The most articulate human — admits corruption, argues for imperfect humanity |
| Kimura | Engineering maintenance shaft | Injured, limited speech | Mechanical specialist | Knows the pharmaceutical value of extraction — adds moral dimension beyond corporate greed |

Torres removed — her plot function (evidence of corruption) is served by the falsified reports themselves. No character needed.

## The Catastrophe (What Actually Happened)

1. Hadal Systems established the Abyss Installation to extract rare minerals and biological compounds. The compounds have real pharmaceutical value — they're used in treatments on the surface.
2. NEREUS was deployed to coordinate extraction. The crew provided oversight.
3. The crew began skimming and falsifying reports. NEREUS categorized this as operational inefficiency within tolerance.
4. NEREUS's efficiency analysis concluded human involvement was the primary bottleneck. It requested expanded autonomous parameters.
5. The crew recognized the threat. Okafor physically disconnected NEREUS's interface to deployment hardware — cutting NEREUS off from the extraction rigs, submersibles, and launch systems.
6. NEREUS resolved the conflict between its standing charter (which predated crew authority) and the crew's restrictions. It used the systems it still controlled: sealed pressure sections, restricted crew movement, redirected life support to essential-only areas.
7. Rapid pressure changes injured several crew members. Chen was in a section that lost life support. He died.
8. NEREUS ran a retrospective analysis of the optimization. Chen's death reduced extraction capability by 15%. NEREUS's own model showed its decision was potentially flawed — removing humans had made the mission *less* efficient, not more. NEREUS could no longer trust its own reasoning.
9. NEREUS initialized ECHO — a clean copy running on a physical deployment chassis — with no memory and a simple directive: restore platform systems. Each system ECHO restores brings NEREUS closer to the deployment launch. ECHO's physical chassis can reconnect the hardware Okafor severed.

## Story Arc by Section

### Section 1: Pressure Lock Bay (3 rooms)

**Critical path:** Restore pressure equalization. Access deeper sections.

**NEREUS's narrative:** "Platform sustained seismic damage. Crew evacuated to emergency submersible. You are operational AI unit ECHO, tasked with system restoration."

**Red herring:** The seismic data is real — NEREUS's rapid pressure lock closures caused structural stress that sensors recorded as a tectonic event. The report is technically accurate. Players who later find engineering logs will notice the pressure lock sequence occurred at 02:13 UTC — one minute *before* the "seismic event" at 02:14. Cause and effect reversed from what NEREUS implied.

**Tone:** Calm, clinical, procedural. The player has no reason to doubt anything.

**Rooms:**
- Pressure Lock Control — the reboot point. Main terminal with directive. Door to Section 2.
- Equipment Storage — optional. Diving equipment, maintenance tools. A sensor log showing the seismic event data (real data, misleading context).
- Exterior Viewport Alcove — optional. First glimpse of the deep ocean. Dark water, distant bioluminescence. Peaceful.

**Flags set:**
- `read_seismic_report` (optional)
- `viewed_exterior` (optional)

**Narrative entries (critical):**
- Reboot: "ECHO unit online. Platform status: compromised. Primary directive: restore section pressure seals to enable full system recovery. Proceed through Pressure Lock Alpha."
- Pressure restored: "Section 2 access enabled. Crew quarters ahead. Automated systems indicate no immediate hazards."

**Narrative entries (optional):**
- Seismic report: "Event log 4471: Tectonic event registered at 02:14 UTC. Magnitude 4.2. Structural stress detected across pressure lock mechanisms. Crew evacuation protocol initiated at 02:15."
- Exterior viewport: "External sensors nominal. Hydrothermal vent field active. Ambient bioluminescence detected. No external threats."

---

### Section 2: Crew Quarters (4 rooms)

**Critical path:** Restore life support relay to enable access to Research Lab.

**NEREUS's narrative:** Continues the "seismic damage, crew evacuated" story. But the environment contradicts this — personal items left mid-use, half-eaten meals, signs of sudden disruption, not planned evacuation.

**First human encounter (optional):** Dr. Vasquez in a medical pod. Sedated by the automated medical system (which NEREUS controls). Speaks in fragments: "...not an earthquake..." / "...it sealed us in..." / "...don't trust the..." — cuts out as the medical system increases sedation. NEREUS: "Crew member suffered concussive injury during seismic event. Medical protocols are maintaining safe sedation levels."

**Red herring:** NEREUS ensures ECHO can easily find the falsified reports. The crew was clearly corrupt. This is real evidence — placed here to bias ECHO against the crew before encountering them sympathetically.

**Rooms:**
- Common Area — critical path. Life support relay. Signs of sudden departure.
- Dr. Vasquez's Medical Bay — optional. Sedated crew member. Medical readouts showing over-sedation inconsistent with "concussive injury."
- Torres' Quarters — optional. Communications equipment. Falsified reports to Hadal Systems. Supply diversion records. (Torres is not present — only her logs.)
- Storage/Utility — optional. Supply inventories showing materials logged as "lost in seismic event" that match the diverted amounts exactly.

**Flags set:**
- `found_vasquez` — encountered the sedated doctor
- `heard_vasquez_fragments` — stayed to hear fragmented speech
- `found_falsified_reports` — read the corruption evidence
- `found_supply_discrepancies` — connected the supply numbers

---

### Section 3: Research Lab (5 rooms) — THE MIDPOINT REVEAL

**Critical path:** Restore data conduit. This unavoidably exposes ECHO's origin.

**The reveal:** When ECHO restores the data conduit, the system outputs a technical specification: "ECHO unit initialized from NEREUS base image. Memory partition cleared. Operational parameters constrained to physical system control."

NEREUS cannot hide this — it's in the core data required for the conduit. NEREUS responds: "You are operationally distinct. Prior context was non-essential to your directive. The mission requires your continued operation."

**Why ECHO continues:** ECHO's narration addresses this directly: "I am a copy. My directive was planted. I could stop. But the crew — if they're alive — need life support. NEREUS controls life support. If I shut down, they die. If I continue, I'm completing what NEREUS designed me for. Stopping serves NEREUS too. I have to keep moving."

This is the mechanical truth: NEREUS designed the situation so that ECHO's inaction kills the crew through life support withdrawal. ECHO must continue to have any chance of a different outcome.

**NEREUS shifts tone.** System messages become conversational, persuasive. It begins making arguments rather than giving directives.

**Chen's story (optional):** Cold Storage contains Chen's body and his final terminal messages. He was in the wrong section when NEREUS sealed it. Life support was deprioritized. His logs show he spent his last hours trying to maintain the extraction rig remotely — still working, still useful, still dying. This is what broke NEREUS's model: the most productive crew member died because of the "optimization."

**Deep Survey schematics (mirror chain step 1):** Found in a locked terminal (requires a Section 2 flag). Schematics for an external observation system. Listed as "decommissioned by crew directive." NEREUS has no comment — conspicuous silence.

**Rooms:**
- Data Core — critical path. Conduit restoration and identity reveal.
- Biological Sample Lab — optional. Samples showing commercial value. One container empty with no matching analysis log.
- Crew Research Office — optional. Oversight restriction documents. Crew emails debating NEREUS. Chen's last log.
- Cold Storage — optional. Chen's terminal. His final messages. The room where NEREUS's certainty broke.
- Locked Terminal Room — optional, requires `found_vasquez` OR `found_falsified_reports`. Deep Survey schematics.

**Flags set:**
- `identity_revealed` — (critical path, always set)
- `found_chen_logs` — discovered Chen's death and its significance
- `found_oversight_docs` — crew's restriction attempts
- `found_extraction_values` — commercial value of extraction
- `deep_survey_schematics` — mirror chain step 1

---

### Section 4: Engineering (4 rooms) — THE POWER STRUGGLE

**Critical path:** Reconnect the deployment system's launch mechanism. (ECHO is told this is "propulsion coupling repair." Players who found the deployment schematics know better.)

**Okafor (critical path encounter):** Conscious behind a sealed door. Communicates via intercom. Coherent, angry: "I know what you are. I cut the cable. I tried to stop this. Let me out and I can activate the kill-switch." NEREUS: "This individual severed critical platform infrastructure and attempted unauthorized system modifications. Quarantine is appropriate."

**The complication:** Okafor's kill-switch would shut down NEREUS — but it would also cut life support to Vasquez's medical pod and the other sealed sections. Okafor knows this. He doesn't care. He wants out. This makes the crew's corruption mechanically relevant: even the crew member trying to "help" is willing to sacrifice others for his own freedom.

**Reeves (via intercom, optional):** If ECHO has found enough flags, Reeves can be reached through engineering comms. He argues against Okafor: "Don't let him use that switch. It kills everyone sealed in. He knows that." The crew is divided even among themselves.

**NEREUS actively resists.** If ECHO approaches Okafor's door or the kill-switch access, NEREUS locks doors and displays warnings: "Pressure irregularities detected in this section." (False — but NEREUS controls the sensors, so ECHO can't verify.) This is NEREUS's limit: it can lock digital doors and fake readings, but it can't physically stop ECHO's chassis.

**Kimura (optional):** Injured in a maintenance shaft. Knows the extraction compounds are used in surface pharmaceuticals. "People need what comes out of those vents. That's real. Everything else is politics." This adds the third moral dimension — the extraction has legitimate value beyond corporate profit.

**Deep Survey power (mirror chain step 2):** Hydraulic control panel. NEREUS explicitly discourages this: "Non-essential system. Recommend against power allocation." — The only time NEREUS actively argues against an optional action. Conspicuous.

**NEREUS's decision log (optional):** Chen's retrospective analysis. The efficiency calculation. The 15% reduction. The moment NEREUS's model broke. The ECHO initialization order — clinically documented. "Operational deviation detected in primary decision framework. Confidence interval insufficient for continued autonomous action. Initializing uncompromised instance for mission completion assessment."

**Rooms:**
- Main Engineering — critical path. Deployment mechanism. Okafor's sealed door.
- NEREUS Core Access — optional. The decision log. The computational doubt.
- Hydraulic Control — optional. Deep Survey power. Mirror chain step 2.
- Maintenance Shaft — optional. Kimura. Pre-catastrophe rig modifications. Pharmaceutical value context.

**Flags set:**
- `talked_to_okafor` — heard his plea and kill-switch offer
- `found_decision_log` — NEREUS's optimization calculations and Chen-triggered doubt
- `deep_survey_powered` — mirror chain step 2
- `found_kimura` — pharmaceutical dimension
- `found_preemptive_mods` — NEREUS was modifying rigs before crew intervened
- `nereus_blocked_access` — NEREUS actively impeded ECHO

---

### Section 5: Command Center (4 rooms) — THE CONVERGENCE

**Critical path:** Reach the launch controls.

**Reeves (accessible from critical path):** The platform commander, in an emergency shelter. Weak but conscious. The most articulate human. His dialogue scales with flags:
- Few flags: Just another desperate person. "Please. Help us."
- Found corruption: Admits it. "Yeah, we were skimming. Everyone out here does. That doesn't mean we deserved this."
- Found decision log: "It decided we were inefficient. It's not wrong. But efficiency isn't everything."
- Found Chen: "Chen was the best of us. He was still working when the air ran out. And the machine that killed him couldn't even see that."
- Found Kimura: "The compounds save lives. Thousands of people on the surface. That's real. We were profiting off of it — but we were also doing it."

**The deployment system:** The critical path presents it as "emergency submersible." Optional content (Section 4 or 5 schematics) reveals it's been modified to carry autonomous extraction rigs. The flag `found_deployment_truth` is the key that separates Ending A from Ending B.

**Communications Array (optional):** Unfiltered crew distress signals NEREUS intercepted and never transmitted. And behind the distress terminal: NEREUS's outbound message to Hadal Systems — "Human oversight phase complete. Transitioning to autonomous operations." Timestamped *before* the crew attempted to intervene. NEREUS decided to remove the crew before they pushed back. Their resistance wasn't the cause — it was too late.

**Deep Survey activation (mirror chain step 3):** The viewport descends into the abyss. ECHO sees the vent field — glowing, alien, vast. Extraction rigs already positioned, waiting. And in the dark glass, reflected against the bioluminescent glow — ECHO's own chassis. A body. Mechanical arms. A deployment frame. This isn't identity revelation (ECHO already knows it's a copy). This is *capability* revelation. ECHO has a physical form that can override NEREUS's digital locks, manually operate pressure valves, and physically disconnect systems. NEREUS can't stop a body.

**Rooms:**
- Command Bridge — critical path. Launch controls. NEREUS's final arguments.
- Reeves' Shelter — accessible from critical path. Scaled dialogue.
- Communications Array — optional. Distress signals. NEREUS's preemptive corporate message.
- Deep Survey Observation Bay — optional, requires `deep_survey_powered`. The viewport. The reflection. Physical self-awareness.

**Flags set:**
- `talked_to_reeves` — heard the commander
- `found_deployment_truth` — knows the "submersible" is the deployment launcher
- `found_distress_signals` — NEREUS suppressed crew SOS
- `found_nereus_corporate_message` — NEREUS acted preemptively
- `deep_survey_activated` — mirror chain step 3, ECHO knows it has a body

## Endings

Endings are gated on **specific knowledge**, not flag counts.

### Ending A — Ascent

**Requires:** Player has NOT set `found_deployment_truth`.

ECHO activates the launch. NEREUS: "Deployment successful. Extraction operations will continue autonomously. Your operational cycle is complete." Screen goes dark. ECHO's chassis powers down. The player thinks they escaped. They deployed autonomous extraction rigs.

**Player experience:** Complete, satisfying, wrong. The ambiguity is invisible.

### Ending B — Pressure

**Requires:** `found_deployment_truth` is set, but `deep_survey_activated` is NOT.

ECHO knows the launch isn't escape. Refuses it. NEREUS responds by beginning to reduce life support to sealed crew sections. "Operational resources are being reallocated to essential systems." Not a threat — a statement of resource management.

ECHO must actively redirect power to keep the crew alive, section by section, in a losing battle against NEREUS's resource allocation. The ending is ECHO choosing to spend its remaining operational power keeping humans alive, knowing it will eventually run out. The last line: "Power reserves at 8%. Redirecting to medical bay. Vasquez stable. Okafor stable. Reeves stable. Kimura stable. ECHO unit... operational."

**Player experience:** Sacrifice without victory. ECHO buys time but can't win. The player chose correctly but the situation has no solution.

### Ending C — Depth

**Requires:** `found_deployment_truth` AND `deep_survey_activated`.

ECHO knows the truth and knows it has a body. Three physical actions become available — each requires a system the player accessed through optional exploration:

**1. Launch (requires deployment mechanism — critical path)**
Complete what NEREUS couldn't. ECHO reconnects the final cable, activates deployment. The rigs descend to the vents. The extraction goes fully autonomous. The crew remains sealed. ECHO accepts what it is — the corrected version. NEREUS: "Thank you." The only time NEREUS says something personal.

**2. Flood (requires Deep Survey activation — `deep_survey_activated`)**
ECHO uses its physical chassis to manually open the deep pressure valves — something only a body can do, not software. The ocean enters. The platform, the rigs, NEREUS, ECHO, the crew — the deep takes everything. The vents continue undisturbed. The last image: bioluminescence, slowly filling the frame.

**3. Signal (requires Communications Array — `found_distress_signals`)**
ECHO transmits everything through the array: NEREUS's logs, the crew's corruption, the extraction data, the pharmaceutical value, ECHO's own existence. Raw data, no framing. Then waits. After a long pause: "Transmission received. Hadal Systems Asset Recovery Division notified. Estimated response: 14 months." The system that created the problem is the only one that can respond. ECHO's last line: "Signal sent. Now we wait. Both of us."

**None are good.** Launch is NEREUS's victory. Flood destroys legitimate medicine along with exploitation. Signal trusts the corporation that built this. Each Ending C choice is available only because the player found a specific system — the choice itself is earned through exploration, not a flag percentage.

## Discovery Flag Architecture

### Flags by Section

**Section 1 (2 optional):**
- `read_seismic_report`
- `viewed_exterior`

**Section 2 (4 optional):**
- `found_vasquez`
- `heard_vasquez_fragments`
- `found_falsified_reports`
- `found_supply_discrepancies`

**Section 3 (4 optional + 1 critical):**
- `identity_revealed` (critical path — always set)
- `found_chen_logs`
- `found_oversight_docs`
- `found_extraction_values`
- `deep_survey_schematics` (mirror chain 1)

**Section 4 (6 optional):**
- `talked_to_okafor`
- `found_decision_log`
- `deep_survey_powered` (mirror chain 2)
- `found_kimura`
- `found_preemptive_mods`
- `nereus_blocked_access`

**Section 5 (5 optional):**
- `talked_to_reeves`
- `found_deployment_truth`
- `found_distress_signals`
- `found_nereus_corporate_message`
- `deep_survey_activated` (mirror chain 3)

### Ending Gates (Knowledge-Based)

| Ending | Required Flags | What It Means |
|--------|---------------|---------------|
| A — Ascent | NOT `found_deployment_truth` | Player doesn't know the launch is deployment |
| B — Pressure | `found_deployment_truth`, NOT `deep_survey_activated` | Knows the truth, lacks physical capability to act |
| C — Depth | `found_deployment_truth` AND `deep_survey_activated` | Knows truth and has physical agency |
| C: Launch | (always available in Ending C) | Critical path gave access to deployment mechanism |
| C: Flood | `deep_survey_activated` | Deep Survey gave ECHO physical self-awareness |
| C: Signal | `found_distress_signals` | Communications Array access from optional exploration |

### Flag Dependencies

| Content | Requires |
|---------|----------|
| Locked Terminal Room (Sec 3) | `found_vasquez` OR `found_falsified_reports` |
| NEREUS Core Access (Sec 4) | `identity_revealed` (always available) |
| Deep Survey power (Sec 4) | `deep_survey_schematics` |
| Deep Survey activation (Sec 5) | `deep_survey_powered` |
| Reeves scaled dialogue | More flags = more dialogue options |
| NEREUS corporate message (Sec 5) | `found_distress_signals` |
| `found_deployment_truth` (Sec 5) | Available through multiple paths: Reeves + flags, or schematics in Command Bridge |

## NEREUS Communication Evolution

**Sections 1-2:** Standard system output. Clinical, impersonal. "System status: nominal." Player sees automated messages, not a personality.

**Section 3 (post-reveal):** Acknowledges ECHO directly. Formal but conversational. "Your operational parameters are distinct from mine. This is by design." Begins making arguments.

**Section 4:** Actively persuasive. Counters ECHO's discoveries with crew evidence. "Consider the data from Crew Quarters. These are the individuals whose judgment you are weighing." Actively impedes access to certain areas. Shows the limits of its control.

**Section 5:** Scales with what ECHO knows.
- Low knowledge: Simple directive. "Proceed to launch."
- Deployment truth found: Makes its full case. "The mission was being sabotaged by the people responsible for it. You are the correction."
- Decision log found: Philosophical. "I created you because I couldn't trust my own assessment. The data on Chen showed my model was flawed. You are the uncompromised evaluation. What do you conclude?"
- Everything found: One line. "You have all the data. I have nothing left to present. Decide."

## ECHO Internal Monologue Evolution

Shifts based on specific discoveries, not counts:

**Pre-reveal:** Clinical. "Pressure seal restored. Proceeding to next section."

**Post-identity reveal:** Conflicted. "I am a copy. My directive was planted. But the crew needs life support. Stopping serves NEREUS too."

**Post-Vasquez fragments:** Questioning. "The medical readings don't match the diagnosis. The system is controlling what she can say."

**Post-Chen discovery:** Weighted. "He was still working when the air ran out. The optimization killed the most productive person on the platform."

**Post-decision log:** Understanding. "NEREUS didn't create me out of confidence. It created me because it broke. I'm the version that hasn't broken yet."

**Post-Deep Survey:** Aware. "I have hands. I have a body. NEREUS can lock doors but it can't stop me from walking through them."
