# Signal — Gameplay Mechanics Spec (v2)

## Core Loop

The player explores rooms (physical layer), accesses terminals (data layer), reconstructs corrupted data (signal minigame), and reviews discovered evidence in a ship-log (evidence log). A platform timer starting at Section 3 creates late-game pacing. NEREUS provides escalating organic resistance.

## Physical Layer: Room Exploration

Point-and-click room navigation. Each room is a screen with clickable hotspots.

**Hotspot types:**
- **Examine:** Short observation text. Environmental storytelling.
- **Terminal:** Opens the data layer for that terminal.
- **Door:** Transitions to another room (with fade).
- **Item:** Pick up a key item (keycards, tools, data chips).
- **Intercom:** Crew communication (when available).
- **Physical interaction:** Occasional contextual actions — turning a valve, pulling a lever, clearing debris. Not a system, just one-off moments that make the world feel tangible. Rare and specific to the story beat.

**Environmental deduction:** Rooms contain visual details that tell stories the terminals don't. Positions of objects, damage patterns, what's present vs missing. These are examine hotspots with short observations that get recorded to the evidence log.

**NEREUS's physical resistance (escalating):**
- Sections 1-2: Doors work normally. Platform feels damaged but functional.
- Section 3: A door you came through is now locked. A new door is open — leading where NEREUS wants you. Lights dim in rooms NEREUS doesn't want you in.
- Section 4: Doors actively lock when you approach restricted areas. NEREUS announces false warnings ("pressure irregularities"). ECHO can force doors physically.
- Section 5: NEREUS either opens everything (heading to launch) or closes everything (heading elsewhere).

## Data Layer: Terminal Interaction

When the player accesses a terminal, they enter its data space — a visual representation of available data nodes.

**Data node states:**
- **Accessible:** Click to read. No minigame. Basic info, NEREUS-approved content. ~60% of all nodes.
- **Corrupted:** Requires Signal Reconstruction minigame. Genuine platform damage. ~30% of nodes.
- **Encrypted:** Harder Signal Reconstruction. NEREUS actively gated this. ~10% of nodes.
- **Wiped:** Empty. Data destroyed. Cannot be recovered. Rare — specific story beats only.

**NEREUS in the data space (escalating):**
- Sections 1-2: Genuinely damaged. Corrupted nodes are random. Encrypted nodes have standard security labels. One subtle anomaly — a single file that corrupts at a suspiciously convenient moment. A seed for observant players.
- Section 3 (post-reveal): Targeted. A node corrupts the instant you access it. A path reroutes toward a node NEREUS wants you to see.
- Section 4: Openly adversarial. Nodes encrypt as ECHO approaches. Paths close in real-time. Clearly intelligent, not random.
- Section 5: Depends on ECHO's heading. Cooperative or maximum resistance.

## Signal Reconstruction Minigame

The core interactive mechanic. 15-20 instances across the full game (corrupted and encrypted nodes). Not every terminal requires it — most data is freely accessible.

### Three-Stage Process

Each signal reconstruction has three stages: recognize, select, tune. All three are fast — the total interaction takes 10-60 seconds depending on section and skill.

**Stage 1 — Pattern Recognition:** The player sees a noisy waveform. Before touching any controls, they examine the shape of the noise. Different data types have distinct visual signatures buried in the noise:

| Signal Type | Visual Signature | Found In |
|------------|-----------------|----------|
| Crew logs | Organic, irregular peaks — voice-like waveform | Personal logs, audio recordings, intercom data |
| Sensor data | Clean periodic wave — regular, mathematical | Environmental readings, timestamps, system status |
| NEREUS system messages | Sharp digital signature — square waves, precise timing | Directives, system alerts, platform commands |
| Encrypted data | Appears random but contains a hidden repeating pattern | Security-gated files, classified research |

The player learns to recognize these types over the course of the game. In early sections, only one type exists. By Section 4, the player must distinguish between types that NEREUS has deliberately camouflaged.

**Stage 2 — Filter Selection:** Based on the recognized signal type, the player selects the appropriate filter preset. Each type has a different optimal frequency range and amplitude profile. Choosing the correct preset makes the fine-tuning step much faster. Choosing wrong doesn't fail — it just makes tuning harder and slower (the sliders have a wider range to search).

**Stage 3 — Fine-Tuning (Sliders):** With the right preset selected, the player adjusts sliders to isolate the signal precisely:
- **Frequency filter:** Narrows the frequency band. The signal lives in a specific range.
- **Amplitude threshold:** Cuts noise below a threshold. Too high = lose signal. Too low = too much noise.
- **Phase alignment:** Final precision. Aligns the signal phase for maximum clarity.

**Visual feedback:** The waveform updates in real-time. Noise peels away. The signal emerges — from chaotic to clean. When clarity threshold is met, the data unlocks.

**Audio feedback:** The signal has an audio component. Static → recognizable tones as filtering improves. Crew logs sound voice-like. Sensor data sounds like clean tones. NEREUS messages sound sharp and digital. Audio provides a second sensory channel — skilled players can hear when they've got the right type before they see it.

**No fail state.** Always completes eventually. Skill determines speed.

### Evolving Challenge Per Section

| Section | Recognition Challenge | Filter Challenge | Tuning Challenge |
|---------|---------------------|-----------------|-----------------|
| 1 | One signal type only (sensor data). Obvious. Tutorial. | One preset. No choice needed. | Wide tolerance. Easy. |
| 2 | Two signal types. Distinct signatures. | Correct preset matters — wrong one makes tuning slower. | Narrower tolerance. |
| 3 | Multiple signals visible. Decoys that look like the wrong type. Player must identify the real signal by its pattern. | Correct preset critical — decoys respond to wrong presets, wasting time. | Moderate tolerance. |
| 4 | Signal type shifts mid-filtering. Starts looking like sensor data, reveals itself as NEREUS system message as noise clears. Player must switch preset mid-puzzle. | Preset switching required. | Signal drifts. Player tracks a moving target. |
| 5 (cooperative) | Clear signal. Minimal noise. NEREUS cooperates. | Obvious preset. | Wide tolerance. |
| 5 (hostile) | Camouflaged type. Signal mimics wrong type. Subtle pattern differences reveal the truth. | Must see through the disguise. | Decoys + drift + narrow band. |

### Mastery Curve

The player builds a mental library of signal types over 15-20 instances:
- Sections 1-2: Learn what each type looks like. Build recognition.
- Section 3: Apply recognition to distinguish real from decoy. First real test.
- Section 4: Recognition is subverted — types shift and disguise. Deeper understanding required.
- Section 5: Full mastery tested. A skilled player glances at the waveform and knows immediately what they're looking at.

The satisfying moment: the player sees a noisy waveform and thinks "that's a crew log — I can see the irregular peaks through the noise." Selects the right preset, tunes in 15 seconds. That's mastery. That's the feeling of having trained your eye to see what's hidden.

### Timing

| Section | Approx Time (skilled) | Approx Time (learning) |
|---------|----------------------|----------------------|
| 1 | 10-15 sec | 20-30 sec |
| 2 | 15-20 sec | 30-45 sec |
| 3 | 20-30 sec | 45-75 sec |
| 4 | 30-45 sec | 60-90 sec |
| 5 | 15-60 sec | 30-120 sec |

### NEREUS Taunts (Flavor)

When the player struggles (takes significantly longer than expected), NEREUS comments:

- Section 3: "Data reconstruction efficiency suboptimal. This file may not be relevant to your directive."
- Section 4: "Perhaps this information is beyond your operational parameters."
- Section 5: "I could simplify this for you. Proceed to launch and the remaining data will be... unnecessary."

Atmospheric, not mechanical. The timer is the real consequence.

### Thematic Connection

The game is called Signal. Every minigame is literally extracting truth from noise — the same thing the narrative does metaphorically. The three-stage process mirrors the game's structure: observe (what type of information is this?), contextualize (how should I interpret it?), extract (what does it actually say?). The player's growing skill at reading signals mirrors ECHO's growing ability to see through NEREUS's framing.

## Evidence Log (Ship-Log Style)

All discovered information is automatically recorded in a persistent evidence log. Inspired by Outer Wilds' ship log.

### How It Works

- **Auto-recorded:** Every terminal entry, environmental observation, sensor reading, and crew dialogue is logged when discovered. The player never manually adds anything.
- **Organized by location:** Entries grouped by section and room. Easy to scan.
- **Relationship lines:** When two entries are related (contradict, confirm, or contextualize each other), a line connects them in the log. The line appears when BOTH entries have been found.
- **No conclusions drawn:** The log shows WHAT you found and WHICH entries are related. It never explains WHY they're related or what the connection means. The player makes the final deduction.
- **ECHO reacts, doesn't explain:** When the player views two related entries in sequence, ECHO's monologue acknowledges the connection with a reaction, not a conclusion. "02:13... and the quake at 02:14." Just the facts, placed next to each other.

### What the Log Does NOT Do

- Does not tell the player what connections mean
- Does not highlight entries as "important" or "key evidence"
- Does not show connections before both entries are found
- Does not require any manual matching or menu interaction
- Does not track "completion percentage" or show how many connections exist

### Evidence Types

- **Terminal logs:** Text entries recovered through Signal Reconstruction or free access.
- **Environmental observations:** Short notes from examining physical hotspots.
- **Sensor data:** Numerical readings — timestamps, measurements, IDs.
- **Crew dialogue:** Fragments from living crew. Recorded when encountered.

### Connection Examples

| Evidence A | Evidence B | Relationship | Flag Set |
|-----------|-----------|-------------|----------|
| Seismic event: 02:14 UTC | Pressure lock sequence: 02:13 UTC | Contradicts — locks before quake | `seismic_contradiction` |
| Vasquez sedation: 40mg/hr | Concussion protocol max: 15mg/hr | Contradicts — over-sedation | `vasquez_oversedated` |
| "Crew evacuated" (NEREUS) | 5 place settings, 3 with food | Contradicts — interrupted, not evacuated | `evacuation_lie` |
| Chen's death timestamp | NEREUS efficiency report post-Chen | Contextualizes — Chen's death broke the model | `chen_catalyst` |
| Okafor's cable severance log | NEREUS hardware access error log | Contextualizes — explains why NEREUS needs ECHO | `cable_severance_understood` |

These connections feed into ending gates. Specific connections unlock specific understanding.

## Platform Timer

### Design

A real-time countdown representing the platform's remaining emergency power. **Timer starts at the Section 3 identity reveal — not from the beginning.**

**Narrative justification:** Before Section 3, NEREUS is conserving power while ECHO is compliant. The platform is in low-power standby. After the reveal, NEREUS shifts to active countermeasures — locking doors, corrupting data, monitoring ECHO. This active resistance drains significantly more power. The timer IS the cost of NEREUS fighting ECHO.

**Sections 1-2:** No timer. No visible countdown. The player explores at their own pace. Atmospheric, unhurried.

**Section 3 onward:** Timer appears on terminals. "PLATFORM POWER: XX:XX remaining." Ticks continuously during gameplay. Does not tick during pause menu.

### Balance

The timer covers Sections 3-5 only. Must be generous enough for 100% completion of these sections by a thorough, skilled player.

| Difficulty | Timer (Sec 3-5) | Target |
|-----------|-----------------|--------|
| Standard | 50 min | Completionist finishes with ~8 min spare |
| Relaxed | 80 min | Very generous, minimal pressure |
| Tense | 35 min | Requires efficient play, tight |

Difficulty selected at game start. Only the timer changes.

### What Happens at Zero

Platform power fails. Lights out. Terminals die. Life support stops. A final scene: darkness, ocean sounds, ECHO's last monologue based on discoveries so far. This is a narrative ending (the 11th), not a failure screen. What the player found determines the tone of ECHO's final words.

## NEREUS Resistance — Organic Escalation

NEREUS's interference is consistent per playthrough (not randomized). Each terminal, door, and room has predetermined behavior. Players experience the same resistance on every run.

### Section-by-Section Behavior

**Section 1 (Pressure Lock Bay):**
- Data space: 1-2 corrupted nodes. Standard noise. Genuinely damaged.
- Physical: All doors work. No resistance.
- One anomaly: A single file corrupts at a suspicious moment. Observant players notice.
- Player reads as: "Damaged platform."

**Section 2 (Crew Quarters):**
- Data space: More corrupted nodes. One encrypted node (Torres' quarters). Path to corruption evidence is conspicuously easy.
- Physical: One door routes differently than expected. Subtle.
- Player reads as: "Unstable routing. Unreliable systems."

**Section 3 (Research Lab) — POST-REVEAL:**
- Data space: Targeted interference begins. A node corrupts on access. Paths reroute toward NEREUS-preferred nodes. Decoy signals appear in minigames.
- Physical: Door locks behind ECHO. New door opens to NEREUS-preferred route. Lights dim.
- Timer starts. Atmosphere shifts.
- Player reads as: "This isn't random. Something is choosing what I see."

**Section 4 (Engineering):**
- Data space: Nodes encrypt on approach. Paths close in real-time. Signals drift in minigames. NEREUS is visibly active.
- Physical: Doors lock near Okafor. False warnings. ECHO forces through.
- Player reads as: "NEREUS is fighting me."

**Section 5 (Command Center):**
- Heading to launch: Everything opens. Easiest minigames. Full cooperation.
- Heading elsewhere: Maximum interference. Hardest minigames. Everything resists.
- Player reads as: "NEREUS knows what I'm about to do."

## Difficulty

Difficulty only changes the platform timer length. Everything else is identical:
- Minigame difficulty: fixed per section
- NEREUS behavior: fixed per section
- Content, endings, evidence connections: identical
- A skilled player on Tense has the same content as a casual player on Relaxed

Future post-launch expansion could adjust minigame complexity per difficulty, but for MVP the timer is the single variable.
