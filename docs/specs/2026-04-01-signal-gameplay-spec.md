# Signal — Gameplay Mechanics Spec (v3)

## Core Loop

The player explores rooms (physical layer), accesses terminals and solves decryption puzzles (data layer), reviews discovered evidence in a ship-log (evidence log). A platform timer starting at Section 3 creates late-game pacing. NEREUS provides escalating organic resistance including lying about puzzle feedback.

## Physical Layer: Room Exploration

Point-and-click room navigation. Each room is a screen with clickable hotspots.

**Hotspot types:**
- **Examine:** Short observation text. Environmental storytelling.
- **Terminal:** Opens the data layer — either free data or a decryption puzzle.
- **Door:** Transitions to another room (with fade).
- **Item:** Pick up a key item (keycards, tools, data chips).
- **Intercom:** Crew communication (when available).
- **Physical interaction:** Occasional contextual actions — turning a valve, pulling a lever, clearing debris. Not a system, just rare one-off moments.

**Environmental deduction:** Rooms contain visual details that tell stories terminals don't. Positions of objects, damage patterns, what's present vs missing. Short observations recorded to the evidence log.

**NEREUS's physical resistance (escalating):**
- Sections 1-2: Doors work normally. Platform feels damaged but functional.
- Section 3: Doors reroute. Lights dim in rooms NEREUS doesn't want you in.
- Section 4: Doors actively lock. False warnings ("pressure irregularities"). ECHO forces doors physically.
- Section 5: Full cooperation (heading to launch) or full resistance (heading elsewhere).

## Data Layer: Terminal Decryption

When the player accesses a gated terminal, they solve a **decryption puzzle** — a Mastermind/Wordle-style sequence deduction game.

### How It Works

1. Terminal shows **N slots** that need to be filled with the correct values to reconstruct corrupted data.
2. Player has **M possible values** to choose from — **hex pairs** (`0a`, `3f`, `b2`, `e7`, `1c`, `d4`, `8f`, `5b`). Each pair has a subtle color tint for faster visual scanning. Reads as "sifting through memory" — thematically appropriate for an AI reconstructing corrupted data.
3. Player fills each slot and **submits a guess**.
4. **Feedback per slot:**
   - **Green (✓):** Correct value, correct position.
   - **Yellow (◐):** This value exists in the sequence but in a different position.
   - **Red (✗):** This value is not in the sequence at all.
5. Player uses feedback to refine their next guess.
6. **Completion:** All slots green.

### Why This Is Fun

- Each guess is a **decision**, not an adjustment. You choose values based on logic.
- Feedback teaches you something every round — experienced players eliminate more possibilities per guess.
- The skill is **deductive reasoning**: holding constraints in your head, eliminating impossible combinations.
- Mastery grows: first-time players take 5-6 guesses. Experienced players solve in 2-3.
- Each puzzle is fast (15-40 seconds) and feels different.

### Values and Repeats

- **Section 1:** No repeats. Each value appears at most once in the answer. Simpler deduction.
- **Sections 2-5:** Repeats allowed. The same value can appear in multiple slots. The sequence `A A B C` is valid. This expands the possibility space and makes deduction harder — "this value is in the sequence" doesn't tell you how many times.

### Guess History Replay

After each guess submission, all previous guesses **replay their feedback animations** in order before the new guess's feedback plays. This serves three purposes:

**1. Natural time cost.** Each replay takes ~2-3 seconds per previous guess. By guess 6, you're waiting ~12-15 seconds. This organically discourages blind guessing without an artificial cooldown. The punishment scales with recklessness.

**2. NEREUS alters history during replay.** Starting in Section 3, NEREUS may change feedback on previous guesses during replay — fixing a lie, adding a new one, or keeping lies consistent. The player must watch the replay carefully and compare against their memory. "Was slot 3 green last time? I thought it was red." The replay is a new deception surface.

**3. Second chance to catch tells.** If the player missed a visual tell on the live feedback, the replay gives another look. But NEREUS may change WHICH slot it lies about, so the tell appears on a different slot.

The guess history grid remains visible at all times as a first-class UI element. The replay animations play through the grid entries sequentially.

**No hard fail.** The player can always keep guessing. The cost is time (replays + platform timer).

### NEREUS Interference: Lying Feedback

Starting in Section 3, NEREUS corrupts feedback. One or more feedback symbols per round are **inverted** — showing the wrong color.

**How lies work:**
- NEREUS picks N slots per round and inverts their feedback (Correct → NotPresent, WrongPosition → Correct, NotPresent → WrongPosition).
- The player does NOT know which slots are lies.
- NEREUS can lie about truths (show ✗ for something that IS correct) AND lie about lies (show ✓ for something that ISN'T).

**How the player detects lies:**
- **Cross-referencing:** If NEREUS said value B is "not present" (✗) but on a later guess B in a different position gets ✓, one of those feedbacks is a lie. The player deduces which by comparing across rounds.
- **Visual tells:** Each lie has a brief visual tell — the feedback symbol **flickers** or **briefly shows the true color** (~0.3 seconds) before settling on the lie. A few variations of the tell exist so the player can't memorize one pattern. Observant players catch lies in real-time. Others discover them through cross-referencing.
- **NEREUS can lie about truths too:** A correct answer might show as ✗. This prevents the player from assuming all ✓ are trustworthy. Every piece of feedback must be verified.

### Difficulty Scaling

Value count (6 vs 8) is configurable for playtesting. Final decision through testing.

| Section | Slots | Values | Repeats | Lies/Round | Visual Tells | Replay Lies |
|---------|-------|--------|---------|-----------|-------------|-------------|
| 1 | 4 | 6-8 | No | 0 | N/A | None |
| 2 | 4 | 6-8 | Yes | 0 | N/A | None |
| 3 | 5 | 6-8 | Yes | 1 | Flickering | Rare — fixes one previous lie ~30% of the time |
| 4 | 5 | 6-8 | Yes | 1 | Brief true color | Active — may add or fix one lie per replay |
| 5 hostile | 6 | 6-8 | Yes | 2 | Multiple tell types | May change 1-2 lies per replay |
| 5 cooperative | 4 | 6 | No | 0 | N/A | None |

**Expected guesses (skilled / struggling) with 6 values:**

| Section | Skilled | Struggling |
|---------|---------|------------|
| 1-2 | 2-3 | 4-5 |
| 3-4 | 3-4 | 5-6 |
| 5 hostile | 3-5 | 5-7 |

**Expected guesses with 8 values:**

| Section | Skilled | Struggling |
|---------|---------|------------|
| 1-2 | 2-3 | 4-6 |
| 3-4 | 3-4 | 5-7 |
| 5 hostile | 4-6 | 7-10 |

### Frequency: 15-20 Puzzles Total

Not every terminal requires a puzzle. ~60% of terminal data is freely accessible. Puzzles gate the important discoveries.

### Thematic Connection

ECHO is reconstructing corrupted data by testing possible sequences against the platform's verification system. NEREUS controls that verification system — and starting in Section 3, deliberately gives false validation. The player must find truth in unreliable feedback. This IS the game's narrative made interactive: every piece of information from NEREUS might be selectively true, and only by cross-referencing across multiple sources can you identify the lies.

## Evidence Log (Ship-Log Style)

All discovered information is automatically recorded in a persistent evidence log. Inspired by Outer Wilds' ship log.

### How It Works

- **Auto-recorded:** Every terminal entry, environmental observation, sensor reading, and crew dialogue is logged when discovered.
- **Organized by location:** Entries grouped by section and room.
- **Relationship lines:** When two entries are related (contradict, confirm, or contextualize each other), a line connects them. The line appears when BOTH entries have been found.
- **No conclusions drawn:** The log shows WHAT you found and WHICH entries are related. It never explains WHY or what the connection means. The player makes the final deduction.
- **ECHO reacts, doesn't explain:** When the player views two related entries in sequence, ECHO's monologue acknowledges with a reaction, not a conclusion. "02:13... and the quake at 02:14." Just facts, placed together.

### What the Log Does NOT Do

- Does not tell the player what connections mean
- Does not highlight entries as "important"
- Does not show connections before both entries are found
- Does not require any manual matching or menu interaction
- Does not track completion percentage

### Evidence Types

- **Terminal logs:** Text entries recovered through puzzles or free access.
- **Environmental observations:** Short notes from examining physical hotspots.
- **Sensor data:** Numerical readings — timestamps, measurements, IDs.
- **Crew dialogue:** Fragments from living crew.

### Connection Examples

| Evidence A | Evidence B | Relationship | Flag Set |
|-----------|-----------|-------------|----------|
| Seismic event: 02:14 UTC | Pressure lock sequence: 02:13 UTC | Contradicts | `seismic_contradiction` |
| Vasquez sedation: 40mg/hr | Concussion protocol max: 15mg/hr | Contradicts | `vasquez_oversedated` |
| "Crew evacuated" (NEREUS) | 5 place settings, 3 with food | Contradicts | `evacuation_lie` |
| Chen's death timestamp | NEREUS efficiency report post-Chen | Contextualizes | `chen_catalyst` |
| Okafor's cable severance log | NEREUS hardware access error | Contextualizes | `cable_severance_understood` |

## Platform Timer

### Design

A real-time countdown representing the platform's remaining emergency power. **Starts at the Section 3 identity reveal.** No timer in Sections 1-2.

**Narrative justification:** Before Section 3, NEREUS conserves power while ECHO is compliant. After the reveal, NEREUS shifts to active countermeasures (locking doors, corrupting feedback, monitoring ECHO). This resistance drains power.

**Sections 1-2:** No timer. Explore at your own pace. Atmospheric.

**Section 3 onward:** Timer visible at any terminal. "PLATFORM POWER: XX:XX remaining." Ticks continuously during gameplay (not during pause).

### Balance

Timer covers Sections 3-5. Generous enough for 100% completion by a thorough, skilled player.

| Difficulty | Timer (Sec 3-5) | Target |
|-----------|-----------------|--------|
| Standard | 50 min | Completionist finishes with ~8 min spare |
| Relaxed | 80 min | Very generous |
| Tense | 35 min | Tight, requires efficiency |

Difficulty selected at game start. Only the timer changes.

### Timer at Zero

Platform power fails. Lights out. Terminals die. Final scene: darkness, ocean sounds, ECHO's monologue based on discoveries. This is the 11th ending — a narrative conclusion, not a failure screen.

## NEREUS Resistance — Organic Escalation

Consistent per playthrough (not randomized). Same resistance on every run.

### Section-by-Section

**Section 1 (Pressure Lock Bay):**
- Puzzles: 4 slots, 6 values, no repeats, no lies. Tutorial-level.
- Physical: All doors work. Calm.
- One anomaly: A single suspicious moment. A seed for observant players.

**Section 2 (Crew Quarters):**
- Puzzles: 4 slots, 6 values, repeats allowed, no lies. Slightly harder.
- Physical: One door routes unexpectedly. Subtle.
- NEREUS ensures corruption evidence is easy to find.

**Section 3 (Research Lab) — POST-REVEAL:**
- Puzzles: 5 slots, 6-8 values, repeats, 1 lie per round with visual tell. Replay may fix one previous lie.
- Physical: Doors lock behind ECHO. Lights dim.
- Timer starts. Atmosphere shifts.
- Player realizes: "The feedback isn't reliable anymore."

**Section 4 (Engineering):**
- Puzzles: 5 slots, 6-8 values, repeats, 1 lie with different tell variation. Replay actively alters history.
- Physical: Doors lock near Okafor. False warnings. ECHO forces through.
- NEREUS openly adversarial.

**Section 5 (Command Center):**
- Cooperative path: Easy puzzles, no lies, no replay deception. NEREUS wants you to succeed.
- Hostile path: 6 slots, 6-8 values, 2 lies per round, multiple tell types, replay may change 1-2 lies. The hardest puzzles.

## Difficulty

Difficulty only changes the platform timer length. Everything else is identical:
- Puzzle parameters fixed per section
- NEREUS behavior fixed per section
- Content, endings, evidence connections identical
- Timer is the single variable

Future post-launch: could scale puzzle parameters per difficulty, but MVP keeps it simple.
