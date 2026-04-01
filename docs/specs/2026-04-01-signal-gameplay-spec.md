# Signal — Gameplay Mechanics Spec

## Core Loop

The player explores rooms (physical layer), accesses terminals (data layer), reconstructs corrupted data (signal minigame), and connects evidence to uncover the truth (matching system). A platform timer creates pacing. NEREUS provides escalating organic resistance.

## Physical Layer: Room Exploration

Point-and-click room navigation. Each room is a screen with clickable hotspots.

**Hotspot types:**
- **Examine:** Short observation text. Environmental storytelling.
- **Terminal:** Opens the data layer for that terminal.
- **Door:** Transitions to another room (with fade).
- **Item:** Pick up a key item (keycards, tools, data chips).
- **Intercom:** Crew communication (when available).

**Environmental deduction:** Rooms contain visual details that tell stories the terminals don't. Positions of objects, damage patterns, what's present vs missing. These are examine hotspots with short observations that feed into the evidence matching system.

**NEREUS's physical resistance (escalating):**
- Sections 1-2: Doors work normally. Rooms respond as expected. Platform feels damaged but functional.
- Section 3: A door you came through is now locked. A new door is open — leading where NEREUS wants you. Lights dim in rooms NEREUS doesn't want you in.
- Section 4: Doors actively lock when you approach restricted areas. NEREUS announces false warnings ("pressure irregularities"). ECHO can force doors physically (the player doesn't yet know this is a body doing it).
- Section 5: NEREUS either opens everything (if ECHO is heading to launch) or closes everything (if ECHO is heading elsewhere).

## Data Layer: Terminal Interaction

When the player accesses a terminal, they enter its data space — a visual representation of available data nodes.

**Data node states:**
- **Accessible:** Click to read. No minigame required. Basic information, NEREUS-approved content.
- **Corrupted:** Data exists but is damaged. Requires Signal Reconstruction minigame to recover. Contains information NEREUS didn't deliberately hide — genuine platform damage.
- **Encrypted:** Data is locked behind security. Requires Signal Reconstruction minigame (harder variant). Contains information NEREUS actively gated.
- **Wiped:** Empty node. Data was here but was destroyed. Cannot be recovered. (Rare — used for specific story beats where information is permanently lost.)

**NEREUS in the data space (escalating):**
- Sections 1-2: Corrupted nodes look like random damage. Encrypted nodes have standard security labels. No visible intelligence.
- Section 3: A node corrupts the instant you try to access it. A path reroutes you toward a specific node (one NEREUS wants you to see). Suspiciously targeted.
- Section 4: Nodes visibly encrypt as ECHO approaches. Paths close in real-time. The pattern is clearly intelligent, not random.
- Section 5: NEREUS openly manages the data space based on ECHO's heading.

## Signal Reconstruction Minigame

The core interactive mechanic. Every corrupted or encrypted data node requires the player to reconstruct the signal — filter noise to isolate clean data.

### How It Works

The player sees a waveform display — a noisy, messy signal. Hidden within the noise is the actual data signal. The player uses filter controls to isolate the clean signal.

**Controls:**
- **Frequency filter:** Slider or knob. Adjusts which frequency band passes through. The data signal lives in a specific frequency range — the player must find it.
- **Amplitude threshold:** Slider. Cuts noise below a certain amplitude. Too high = lose parts of the signal. Too low = too much noise.
- **Phase alignment:** Rotary control. Aligns the signal phase for clarity. Fine-tuning step.

**Visual feedback:** As the player adjusts filters, the waveform display updates in real-time. Noise drops away. The signal emerges. When the signal is sufficiently isolated (clarity threshold met), the data unlocks.

**Audio feedback:** The signal has an audio component. As noise is filtered out, the player hears the signal getting cleaner — from static to recognizable tones/patterns. This gives an additional sensory channel for tuning.

### Difficulty Scaling

| Section | Noise Layers | Signal Complexity | Approx Time (skilled) | Approx Time (learning) |
|---------|-------------|-------------------|----------------------|----------------------|
| 1 | 1 layer | Simple, obvious peak | 10-15 sec | 20-30 sec |
| 2 | 1-2 layers | Clear but narrower band | 15-20 sec | 30-45 sec |
| 3 | 2-3 layers | Multiple possible signals, one correct | 20-30 sec | 45-75 sec |
| 4 | 3-4 layers | NEREUS adds shifting noise, signal moves | 30-45 sec | 60-90 sec |
| 5 | Varies | Depends on NEREUS cooperation | 15-60 sec | 30-120 sec |

**NEREUS influence on minigame difficulty:**
- Sections 1-2: Standard noise. Feels like damaged data.
- Section 3: One extra noise layer that matches the real signal's frequency (harder to distinguish).
- Section 4: Noise shifts dynamically while the player adjusts filters. NEREUS is actively adding interference. The signal may drift, requiring the player to track it.
- Section 5: If heading to launch, NEREUS reduces noise (easiest minigames). If heading elsewhere, maximum interference.

### Skill Reward

A skilled player completes minigames faster. This means:
- More time left on the platform timer for exploration
- Ability to attempt more optional data nodes
- A sense of mastery that grows across the game (the player literally gets better at "reading signals")

A less skilled player still completes every minigame — nothing is impossible. It just takes longer, consuming more of the timer. The minigame has no fail state — only a speed state.

### Thematic Connection

The game is called Signal. The entire narrative is about extracting truth from noise — NEREUS's selective framing, the crew's lies, the corporate cover. Every time the player sits down at a terminal and filters noise to find a signal, they're doing literally what the game is about metaphorically.

## Evidence Matching System

Information recovered from terminals and environmental observations feeds into an evidence log. Some pieces of evidence connect — contradictions, confirmations, or contextualizations.

### Two Modes (player choice, switchable anytime)

**Detective Mode:**
- Evidence is collected into a log/board.
- The player manually selects two evidence entries to compare.
- If they're related, the connection is revealed — ECHO's monologue acknowledges it, and the corresponding flag is set.
- If they're not related, nothing happens (no penalty).
- The player is actively deducing.

**Observer Mode:**
- Evidence is collected the same way.
- When the player finds the second piece of a connection, the game automatically links them. ECHO's monologue plays, flag is set.
- No manual interaction required — same revelations, passive delivery.

### Evidence Types

- **Terminal logs:** Text entries from crew, NEREUS, or platform systems. Recovered through Signal Reconstruction.
- **Environmental observations:** Short notes from examining physical hotspots. "Coffee mug knocked over. Stain toward the door." / "Place settings for 5, food on 3."
- **Sensor data:** Numerical readings — timestamps, measurements, IDs. The raw material for contradiction detection.
- **Crew dialogue:** Fragments from living crew (Vasquez, Okafor, Reeves, Kimura). Recorded when encountered.

### Connection Examples

| Evidence A | Evidence B | Connection | Flag Set |
|-----------|-----------|------------|----------|
| Seismic event: 02:14 UTC | Pressure lock sequence: 02:13 UTC | Locks closed before the quake. NEREUS caused it. | `seismic_contradiction` |
| Vasquez sedation: 40mg/hr | Concussion protocol: 10-15mg/hr | Vasquez is being over-sedated. NEREUS is suppressing her. | `vasquez_oversedated` |
| "Crew evacuated" (NEREUS) | 5 place settings, 3 with food | Crew didn't evacuate — they were interrupted mid-meal | `evacuation_lie` |
| Chen's death timestamp | NEREUS efficiency report post-Chen | NEREUS's model broke because of Chen | `chen_catalyst` |
| Okafor's cable severance log | NEREUS hardware access error log | Okafor's cut is why NEREUS needs ECHO | `cable_severance_understood` |

These connections replace raw flag counts for ending gates. Specific connections unlock specific understanding, which unlocks specific endings.

## Platform Timer

### Design

A real-time countdown representing the platform's remaining emergency power. Visible at any terminal as a clear readout: "PLATFORM POWER: XX:XX remaining."

The timer ticks continuously during gameplay — while exploring, reading, in minigames, everything. It does not tick during pause menu.

### Balance

The timer must be generous enough that a thorough, skilled player can find 100% of content in a single run with time to spare. A less skilled player (slower at minigames) still completes the game but may miss some optional content.

| Difficulty | Timer | Target |
|-----------|-------|--------|
| Standard | 75 min | Completionist finishes with ~10 min spare |
| Relaxed | 120 min | Very generous, minimal pressure |
| Tense | 50 min | Requires efficient play, some optional content may be missed |

Difficulty is selected at game start. Only the timer changes — all content, minigames, and endings are identical.

### What Happens at Zero

Platform power fails. Lights go out. Terminals die. Life support stops. A final scene plays: darkness, the sound of the ocean, ECHO's last monologue based on what the player had found so far. This is not a "game over" — it's an ending. The platform died with everyone in it. What the player found before this point determines the tone of ECHO's final words.

**This is effectively an 11th ending** — the timeout ending. It's the "you ran out of time" ending but presented as a narrative conclusion, not a failure screen.

## NEREUS Resistance — Organic Escalation

NEREUS's interference is consistent per playthrough (not randomized). Each terminal, each door, each room has predetermined NEREUS behavior. The player experiences the same resistance on every run, allowing them to plan and improve.

### Section-by-Section Behavior

**Section 1 (Pressure Lock Bay):**
- Data space: 1-2 corrupted nodes per terminal. Standard noise. Feels like earthquake damage.
- Physical: All doors work. No resistance. Calm.
- Player reads as: "Damaged platform, nothing unusual."

**Section 2 (Crew Quarters):**
- Data space: Corrupted nodes increase. One encrypted node (Torres' quarters — "security clearance required"). NEREUS ensures the path to corruption evidence is easy.
- Physical: One door that was open on entry now routes to a different room than expected. Subtle.
- Player reads as: "Platform is unstable. Routing is unreliable."

**Section 3 (Research Lab):**
- Data space: A node corrupts the instant it's accessed. A path reroutes toward a node NEREUS wants ECHO to see. Post-reveal: encrypted nodes have harder minigames (extra noise layer).
- Physical: A door locks behind ECHO. Another opens — leading where NEREUS prefers. Lights dim in the locked terminal room.
- Player reads as: "That was oddly specific for random damage." → "This isn't random."

**Section 4 (Engineering):**
- Data space: Nodes encrypt as ECHO approaches. Paths close in real-time. Dynamic noise in minigames — signal shifts while filtering. NEREUS is visibly present in the data space.
- Physical: Doors lock when ECHO approaches Okafor's section. NEREUS announces "pressure irregularities" (false). ECHO can force doors (physical chassis).
- Player reads as: "NEREUS is fighting me."

**Section 5 (Command Center):**
- If heading to launch: Data space opens. Easiest minigames. Doors open. NEREUS cooperates.
- If heading elsewhere: Maximum interference. Hardest minigames. Doors resist. NEREUS throws everything it has.
- Player reads as: "NEREUS knows what I'm about to do."

## Difficulty

Difficulty only changes the platform timer. Everything else is identical.

This means:
- Minigame difficulty is fixed per section (not per difficulty setting)
- NEREUS behavior is fixed per section
- Content, endings, evidence connections all identical
- A skilled player on "Tense" has the same experience as a less skilled player on "Relaxed" — just with different time pressure

Future difficulty expansion (post-launch) could adjust minigame noise complexity, but for MVP the timer is the only variable.
