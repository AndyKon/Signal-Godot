# World Interaction & Room System — Design Spec

## Overview

Defines how the player experiences rooms, interacts with the world, and how room content is authored. Rooms are 2D parallax scenes with three depth layers, clickable hotspots, and ECHO's internal monologue as a distinct narrative voice. Designed for efficient collaborative content production.

## Rendering: Parallax Panning

Each room is composed of three visual layers that shift at different rates as the player moves their cursor, creating depth:

- **Background layer** (moves slowest): Deep environment — walls, viewports, distant structures, bioluminescence
- **Midground layer** (medium movement): Interactive elements — terminals, doors, crew, furniture, pipes
- **Foreground layer** (moves fastest): Close elements — floor grating, railing, debris, atmospheric particles

The room canvas is wider than the viewport (~120-130% of screen width). The player's cursor position drives the parallax offset — moving the cursor right shifts the view right, revealing content at the edges.

### Parallax Parameters
- Background shift: ±20px from center at cursor extremes
- Midground shift: ±10px
- Foreground shift: ±4px
- Transition easing: smooth (0.15s ease-out)
- Canvas overflow: ~15% beyond viewport on each side

## Hotspot Discovery

### Critical-path hotspots (must interact to progress)
Subtle ambient visual cue — a faint glow or slow pulse that draws attention without being obvious. The player should never get stuck because they couldn't find a required interaction.

### Optional hotspots (extra evidence, flavor)
Completely hidden by default. Revealed only by:
1. **Cursor hover** — cursor changes to interaction shape (already implemented via CursorManager)
2. **ECHO scan** — pressing and holding a designated key (e.g., Shift) triggers a scan-line sweep effect that briefly highlights all hotspots in the current view. Releasing hides them. Thematically: ECHO running a sensor sweep of the room.

### Hotspot visual states
- **Hidden**: invisible, no visual indicator
- **Ambient cue** (critical path only): faint pulsing glow, ~20% opacity
- **Hover**: green highlight border appears, cursor changes
- **Scan reveal**: all hotspots in view flash with scan-line effect for the duration of the key hold

## ECHO Monologue — Distinct Visual Treatment

ECHO's internal thoughts are visually distinct from terminal readouts, environmental descriptions, and crew dialogue.

### Narrative text types
| Type | Source | Visual |
|------|--------|--------|
| Terminal readout | Reading a terminal/log | Bottom panel, green terminal text, monospace |
| Environmental | Examining a hotspot | Bottom panel, white/light text, standard font |
| Crew dialogue | Talking to crew | Bottom panel, character-colored text or name prefix |
| ECHO monologue | Internal thought, evidence reactions | **Top-center overlay**, cool blue/cyan italic text, no panel background — floats over the scene |

### ECHO monologue behavior
- Appears centered near the top of the screen, not in the bottom narrative panel
- Semi-transparent text with subtle text shadow — visible but doesn't obscure the room
- Same typewriter effect as other text, but slightly faster (ECHO thinks quickly)
- Click to dismiss (same as existing behavior)
- Used for: evidence connection reactions, post-discovery observations, key narrative moments
- **Not used for**: reading terminals (that's terminal readout), examining objects (that's environmental)

## Room Content Architecture

### ParallaxRoom base class
Handles all common room behavior:
- Creates three parallax layers (bg, mid, fg) as child Controls
- Drives parallax panning from cursor position in `_Process`
- Places hotspots from room data with correct layer assignment
- Handles ambient cues on critical-path hotspots
- Implements ECHO scan reveal (Shift key)
- Manages room transitions (fade via SceneLoader)

### Room data structure
Each room is defined as a data object consumed by `ParallaxRoom`:

```
RoomDefinition:
  Id: string                     # e.g., "section1_pressure_control"
  DisplayName: string            # e.g., "Pressure Lock Control"
  Section: int                   # 1-5

  # Layer colors/backgrounds (programmatic for now, replaceable with art later)
  BgColor: Color
  MidColor: Color
  FgColor: Color

  # Hotspots
  Hotspots: list of:
    Id: string
    Position: Vector2            # relative to room canvas
    Size: Vector2
    Layer: enum (Bg, Mid, Fg)    # which parallax layer it's on
    IsCriticalPath: bool         # gets ambient cue if true
    HotspotData: HotspotData     # existing action definition
    EvidenceToDiscover: string   # optional evidence ID
```

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

- **Dust particles**: subtle floating particles in the foreground layer. Count and speed configurable per room.
- **Emergency lighting**: orange light strips on walls. Pulse animation. Present in all rooms, intensity varies.
- **Bioluminescence**: blue/cyan dots visible through viewports. Only in rooms with exterior viewports.
- **Ambient audio**: handled by existing `SceneAudio.cs` — ambience clip per room.

## Room Transitions

Existing `SceneLoader` handles fade transitions between rooms. With parallax, the transition is:
1. Fade to black (0.5s)
2. Load new room data, rebuild parallax layers
3. Fade in (0.5s)

For doors within the same section: quick fade. For section transitions: longer fade with possible loading screen text ("Restoring pressure equalization...").

## Section 1: Pressure Lock Bay — Room Content

Three rooms as the first playable section. This is the content that validates the system.

### Room 1: Pressure Lock Control
- **Background**: Dark blue-black wall with circular viewport showing deep ocean. Bioluminescent organisms visible.
- **Midground**: Main terminal (critical path — reboot sequence), equipment rack, pressure gauge readouts, door to Room 2
- **Foreground**: Metal floor grating, warning stripes near door, dust particles
- **Hotspots**:
  - Main Terminal (critical path, midground) — Narration: NEREUS boot message. Evidence: `nereus_boot_message`. Flag: none (critical path narrative)
  - Pressure Gauge (optional, midground) — Examine: pressure readings, environmental flavor
  - Door to Room 2 (critical path, midground) — Door transition
  - Viewport (optional, background) — Examine: first glimpse of deep ocean. Evidence: `exterior_view`

### Room 2: Equipment Storage
- **Background**: Narrower room, shelving units, dim lighting
- **Midground**: Storage terminal with sensor logs, equipment lockers, door back to Room 1, locked door to Room 3 (requires keycard)
- **Foreground**: Scattered tools on floor, emergency kit, dust
- **Hotspots**:
  - Storage Terminal (optional, midground) — Terminal: seismic data report. Evidence: `seismic_report`. Flag: `read_seismic_report`
  - Equipment Locker (optional, midground) — Examine: standard maintenance equipment, nothing unusual
  - Keycard (critical path, foreground) — PickUp: hub keycard. Flag: `picked_up_hub_keycard`
  - Door to Room 1 (critical path, midground) — Door transition
  - Door to Room 3 (critical path, midground) — Door transition, requires keycard

### Room 3: Power Junction
- **Background**: Red-brown tinted room, warning lights, exposed wiring
- **Midground**: Power console (critical path), cable conduits, warning signage, door back to Room 2
- **Foreground**: Floor panels partially removed, tools left mid-repair
- **Hotspots**:
  - Power Console (critical path, midground) — Terminal: restore hub power, unlocks Section 2. Evidence: none. Flag: `hub_power_restored`
  - Exposed Wiring (optional, midground) — Examine: signs of hasty repair work, not recent
  - Warning Sign (optional, background) — Examine: "CAUTION: Pressure differential — seal doors before maintenance"
  - Door to Room 2 (critical path, midground) — Door transition

## File Structure

### Create
- `scripts/room/ParallaxRoom.cs` — Base class: layer management, parallax panning, hotspot placement, scan reveal, ambient effects
- `scripts/room/RoomDefinition.cs` — Data class: room layout definition
- `scripts/room/RoomRegistry.cs` — Static data: all room definitions
- `scripts/room/EchoMonologue.cs` — Distinct visual treatment for ECHO's internal voice (top-center overlay, cyan/blue text)

### Modify
- `scripts/narrative/NarrativeManager.cs` — Add method for ECHO monologue display (separate from ShowText)
- `scripts/rooms/RoomBuilder.cs` — Deprecate (replaced by ParallaxRoom system)
- `scripts/rooms/HubRoom1.cs`, `HubRoom2.cs`, `HubRoom3.cs` — Replace with data-driven rooms in RoomRegistry
- `scenes/Section1_Hub_Room1.tscn`, `Room2.tscn`, `Room3.tscn` — Update to use ParallaxRoom

## What This Spec Does NOT Cover
- Actual art assets (using programmatic colored rectangles for now — replaceable later)
- Audio content (ambient clips not yet produced)
- Sections 2-5 room content (this spec builds Section 1 only — subsequent sections follow the same pattern)
- Decryption puzzle integration with terminals (terminal hotspot triggers puzzle — wiring exists but content connections not defined here)
- Platform timer UI (separate feature)
