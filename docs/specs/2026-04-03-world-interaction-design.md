# World Interaction & Room System — Design Spec

## Overview

Defines how the player experiences rooms, interacts with the world, and how room content is authored. Rooms are 2D parallax scenes with three depth layers that pan in both horizontal and vertical axes, clickable hotspots with layered discovery, and ECHO's internal monologue as a distinct narrative voice. Section 1 serves as a tutorial introducing core mechanics one at a time. Designed for efficient collaborative content production.

## Rendering: 2D Parallax Panning

Each room is composed of three visual layers that shift at different rates as the player moves their cursor, creating depth. Panning works in both horizontal AND vertical axes — the player looks around the room by moving the cursor.

- **Background layer** (moves slowest): Deep environment — walls, viewports, distant structures, bioluminescence
- **Midground layer** (medium movement): Interactive elements — terminals, doors, crew, furniture, pipes
- **Foreground layer** (moves fastest): Close elements — floor grating, railing, debris, atmospheric particles

The room canvas is larger than the viewport in both dimensions. The player cannot see the entire room at once — they must look around. Some hotspots are positioned at the edges or above/below the default view, requiring exploration to find.

### Parallax Parameters
- Room canvas size: **150% viewport width × 130% viewport height**
- Background shift: ±50px horizontal, ±30px vertical from center
- Midground shift: ±25px horizontal, ±15px vertical
- Foreground shift: ±10px horizontal, ±6px vertical
- Transition easing: smooth (0.15s ease-out)
- Cursor position maps to 2D viewport offset (cursor at center = default view, cursor at edge = max shift)

### Narrative uses for vertical parallax
- Ceiling-mounted elements (NEREUS cameras, overhead pipes, warning lights) — look up to notice
- Floor-level details (dropped items, floor damage, half-hidden objects under furniture) — look down to find
- Section 3+: NEREUS can dim lights when the player looks toward something it doesn't want them to see — parallax direction becomes a narrative tool

## Hotspot Discovery

### Section 1 as tutorial
Section 1 explicitly teaches core mechanics one room at a time:
- **Room 1**: Teaches scanning. NEREUS boot message includes: "ECHO sensor suite: active. Diagnostic scan available [Shift]." Player learns the scan mechanic from diegetic system initialization text.
- **Room 2**: Teaches item gating + evidence discovery + decryption puzzles. Locked terminal requires puzzle. Keycard gates door.
- **Room 3**: Teaches terminal interaction for progression. Power console unlocks Section 2.

### Critical-path hotspots (must interact to progress)
Subtle ambient visual cue — a faint glow or slow pulse (~20% opacity) that draws attention without being obvious. The player should never get stuck because they couldn't find a required interaction.

### Optional hotspots (extra evidence, flavor)
Completely hidden by default. Revealed only by:
1. **Cursor hover** — cursor changes to interaction shape (already implemented via CursorManager)
2. **ECHO scan** — pressing and holding Shift triggers a scan-line sweep effect that highlights all hotspots in the current view. Releasing hides them. Thematically: ECHO running a sensor sweep of the room.

### Hotspot visual states
- **Hidden**: invisible, no visual indicator
- **Ambient cue** (critical path only): faint pulsing glow, ~20% opacity
- **Hover**: green highlight border appears, cursor changes
- **Scan reveal**: all hotspots in view flash with scan-line effect for the duration of the key hold

## ECHO Monologue — Distinct Visual Treatment

ECHO's internal thoughts are visually distinct from terminal readouts, environmental descriptions, and crew dialogue. Displayed in the same bottom panel but with different styling.

### Narrative text types
| Type | Source | Visual |
|------|--------|--------|
| Terminal readout | Reading a terminal/log | Bottom panel, green terminal text, monospace |
| Environmental | Examining a hotspot | Bottom panel, white/light text, standard font |
| Crew dialogue | Talking to crew | Bottom panel, character-colored text or name prefix |
| ECHO monologue | Internal thought, evidence reactions | Bottom panel, **cool blue/cyan italic text**, slightly different panel border/background tint |

### ECHO monologue behavior
- Same bottom panel position as other narrative text, but visually distinct (cyan/blue italic, semi-transparent panel with blue tint instead of black)
- Slightly faster typewriter speed (ECHO thinks quickly)
- Click to dismiss (same as existing behavior)
- Used for: evidence connection reactions, post-discovery observations, key narrative moments, room entry narration
- **Not used for**: reading terminals (that's terminal readout), examining objects (that's environmental)
- **Future option**: upgrade to center-screen floating overlay for short reactions if bottom-panel feels flat during playtesting

## Room Content Architecture

### ParallaxRoom base class
Handles all common room behavior:
- Creates three parallax layers (bg, mid, fg) as child Controls
- Drives 2D parallax panning from cursor position in `_Process`
- Places hotspots from room data with correct layer assignment
- Handles ambient cues on critical-path hotspots
- Implements ECHO scan reveal (Shift key)
- Plays entry narrative on first visit (ECHO monologue style)
- Manages room transitions (fade via SceneLoader)
- Spawns atmospheric effects (dust, emergency lights, bioluminescence)

### Room data structure
Each room is defined as a data object consumed by `ParallaxRoom`:

```
RoomDefinition:
  Id: string                     # e.g., "section1_pressure_control"
  DisplayName: string            # e.g., "Pressure Lock Control"
  Section: int                   # 1-5

  # Layer colors/backgrounds (programmatic for now, replaceable with art later)
  # Art pipeline: each room needs 3 PNGs at 2880×1404 (150%×130% of 1920×1080)
  # Background: fully opaque. Midground/Foreground: transparent where empty.
  BgColor: Color
  MidColor: Color
  FgColor: Color

  # Audio
  AmbientClip: string            # audio clip ID, empty = inherit from previous room
  MusicClip: string              # optional music override, empty = no change

  # Entry narrative (ECHO monologue, first visit only)
  # First-visit tracked via auto-generated flag: "visited_" + Id
  EntryNarrative: string         # empty = no narration on entry

  # Atmospheric settings
  DustCount: int                 # number of floating particles (0 = none)
  HasEmergencyLights: bool       # orange pulse strips on walls
  HasViewport: bool              # exterior viewport with bioluminescence

  # Hotspots
  Hotspots: list of:
    Id: string
    Position: Vector2            # relative to room canvas
    Size: Vector2
    Layer: enum (Bg, Mid, Fg)    # which parallax layer it's on
    IsCriticalPath: bool         # gets ambient cue if true
    HotspotData: HotspotData     # existing action definition
    EvidenceToDiscover: string   # optional evidence ID
    RequiresPuzzle: bool          # if true, launches decryption puzzle before showing content
    PuzzleOverride: string        # empty = use game-state difficulty, non-empty = named preset key
```

Data structure is a plain class with public fields — adding new fields later requires one line plus a default value. Existing room definitions are unaffected by additions.

### Puzzle launch flow
When `InteractionManager` processes a hotspot with `RequiresPuzzle = true`:

1. Check if puzzle already solved (flag: `"solved_" + hotspotId`). If solved, skip to step 6.
2. Determine puzzle parameters: if `PuzzleOverride` is set, use that named preset. Otherwise, query game state (section, flags) for current difficulty tier.
3. Launch `DecryptionPuzzleUI` as a fullscreen overlay (CanvasLayer). **Does NOT pause the game** — the platform timer keeps ticking. Puzzle time is a real cost.
4. Room interaction is blocked while puzzle is active.
5. On puzzle completion: set flag `"solved_" + hotspotId`, close puzzle overlay.
6. Resume normal hotspot action — show terminal content via NarrativeManager, discover evidence, set flags.

Revisiting a solved terminal shows the content directly (no repeat puzzle).

### Evidence web tutorial trigger
The evidence web is not mentioned until the player has something to see in it. On the first evidence connection activation (typically early Section 2: `nereus_boot_message` + `sudden_departure`), after the ECHO reaction plays, a follow-up prompt displays: "Data correlation detected. Access memory log [J] to review." This teaches the evidence web at the moment it becomes useful.

### Room registry
Static data file (like `EvidenceRegistry.cs`) containing all room definitions. A generic scene loads the correct room data by ID.

### Custom room scripts
Rooms with unique behavior (Deep Survey reflection, Vasquez medical bay, identity reveal) extend `ParallaxRoom` and override specific hooks:
- `OnRoomEnter()` — custom logic when room loads
- `OnHotspotInteracted(string hotspotId)` — custom response to specific hotspot
- `OnRoomUpdate(double delta)` — per-frame custom behavior (e.g., medical readout animation)

Most rooms need no custom script — `ParallaxRoom` + data handles everything.

## Atmospheric Elements

Built into the `ParallaxRoom` base class, controlled by room data:

- **Dust particles**: subtle floating particles in the foreground layer. Count configurable per room.
- **Emergency lighting**: orange light strips on walls. Pulse animation. Controlled by `HasEmergencyLights`.
- **Bioluminescence**: blue/cyan dots visible through viewports. Controlled by `HasViewport`.
- **Ambient audio**: handled by existing `SceneAudio.cs` pattern — clip ID in room data.

## Room Transitions

Existing `SceneLoader` handles fade transitions between rooms. With parallax, the transition is:
1. Fade to black (0.5s)
2. Load new room data, rebuild parallax layers
3. Fade in (0.5s)

For doors within the same section: quick fade. For section transitions: longer fade with possible loading screen text ("Restoring pressure equalization...").

## Section 1: Pressure Lock Bay — Room Content

Three rooms serving as the tutorial section. Introduces scanning, evidence, puzzles, and item gating. Validates the full gameplay loop: explore → scan → discover evidence → solve puzzle → unlock content → check evidence web.

### Room 1: Pressure Lock Control
**Teaches: scanning, basic interaction**

- **Background**: Dark blue-black wall with circular viewport showing deep ocean. Bioluminescent organisms visible. Ceiling pipes visible when looking up.
- **Midground**: Main terminal (critical path), pressure gauge readouts, door to Room 2
- **Foreground**: Metal floor grating, warning stripes near door, dust particles
- **Entry narrative**: "Systems initializing. Platform damage detected. Restoration directive active."
- **Hotspots**:
  - Main Terminal (critical path, midground) — Narration: NEREUS boot message including scan tutorial ("ECHO sensor suite: active. Diagnostic scan [Shift]."). Evidence: `nereus_boot_message`
  - Pressure Gauge (optional, midground) — Examine: pressure readings show fluctuation pattern inconsistent with seismic activity. Environmental flavor.
  - Door to Room 2 (critical path, midground) — Door transition
  - Viewport (optional, background, positioned high — requires looking up) — Examine: first glimpse of deep ocean. Evidence: `exterior_view`

### Room 2: Equipment Storage
**Teaches: decryption puzzles, item gating, evidence discovery**

- **Background**: Narrower room, shelving units, dim lighting. Emergency light strip along ceiling.
- **Midground**: Locked terminal (requires decryption puzzle), equipment lockers, door back to Room 1, locked door to Room 3 (requires keycard)
- **Foreground**: Scattered tools on floor, emergency kit near door, dust
- **Hotspots**:
  - Locked Terminal (optional, midground) — `RequiresPuzzle: true`. Puzzle params from game state (Section 1 = 4 slots, 6 values, no lies). On solve: seismic data report. Evidence: `seismic_report`. Flag: `read_seismic_report`
  - Equipment Locker (optional, midground) — Examine: standard maintenance equipment, nothing unusual
  - Keycard (critical path, foreground, positioned low — on the floor near shelves) — PickUp: hub keycard. Flag: `picked_up_hub_keycard`
  - Emergency Kit (optional, foreground) — Examine: unopened. Everything still sealed. Not used.
  - Door to Room 1 (critical path, midground) — Door transition
  - Door to Room 3 (critical path, midground) — Door transition, requires keycard

### Room 3: Power Junction
**Teaches: terminal interaction for progression**

- **Background**: Red-brown tinted room, warning lights active, exposed wiring on back wall
- **Midground**: Power console (critical path), cable conduits, warning signage, door back to Room 2
- **Foreground**: Floor panels partially removed, tools left mid-repair
- **Hotspots**:
  - Power Console (critical path, midground) — Terminal: restore hub power, unlocks Section 2. Flag: `hub_power_restored`. Entry to Section 2 narration: "Power restored. Section 2 pressure locks releasing."
  - Exposed Wiring (optional, midground) — Examine: "Repair work, interrupted. The cuts are clean — deliberate, not damage."
  - Warning Sign (optional, background, positioned high) — Examine: "CAUTION: Pressure differential — seal doors before maintenance"
  - Door to Room 2 (critical path, midground) — Door transition

## File Structure

### Create
- `scripts/room/ParallaxRoom.cs` — Base class: 2D layer management, parallax panning (both axes), hotspot placement, scan reveal, ambient effects, entry narrative
- `scripts/room/RoomDefinition.cs` — Data class: room layout definition with all fields
- `scripts/room/RoomRegistry.cs` — Static data: Section 1 room definitions (3 rooms)

### Modify
- `scripts/narrative/NarrativeManager.cs` — Add `ShowEchoMonologue()` method with distinct styling (cyan italic, blue-tinted panel)

### Delete
- `scripts/rooms/RoomBuilder.cs` — Replaced by ParallaxRoom system
- `scripts/rooms/HubRoom1.cs`, `HubRoom2.cs`, `HubRoom3.cs` — Replaced by data-driven rooms
- `scenes/Section1_Hub_Room1.tscn`, `Section1_Hub_Room2.tscn`, `Section1_Hub_Room3.tscn` — Replaced by new ParallaxRoom scenes

### Modify
- `scripts/tests/AutoPlaytest.cs` — Remove room-navigation tests (tested placeholder content, not systems). Keep core system tests (GameState, SaveSystem, ending evaluator). Room integration tested via manual playtesting.
- `scripts/interaction/InteractionManager.cs` — Add puzzle gate check in ExecuteAction (RequiresPuzzle flow)

### New Scenes
- `scenes/Section1_PressureLockControl.tscn` — Room 1 using ParallaxRoom
- `scenes/Section1_EquipmentStorage.tscn` — Room 2 using ParallaxRoom
- `scenes/Section1_PowerJunction.tscn` — Room 3 using ParallaxRoom

## What This Spec Does NOT Cover
- Actual art assets (using programmatic colored rectangles — art pipeline dimensions noted in room data structure)
- Audio content (ambient clips not yet produced)
- Sections 2-5 room content (this spec builds Section 1 only — subsequent sections follow the same pattern)
- Platform timer UI (separate feature — timer runs during puzzles, pauses only in evidence web and pause menu)
- Flag-driven puzzle difficulty tiers (for now: switch on section number; later: full game-state query)
