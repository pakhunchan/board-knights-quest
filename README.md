# Knight's Quest: Math Adventures

An educational fractions game built with Unity and the [Board SDK](https://dev.board.fun) for the Playcademy Superbuilders Challenge. Players use physical tabletop pieces (robots and ships) to solve fraction equivalence problems through a narrative-driven knight's quest.

## Tech Stack

- **Unity** 6.4 (6000.4.1f1)
- **Board SDK** v3.3.0 — tabletop piece detection via Board Arcade glyph set
- **TextMesh Pro** — styled text and karaoke subtitle highlighting
- **TTS** — pre-generated narration audio (Python scripts in `tts-gen/`)

## Game Flow

The game runs as a single master scene (`OfficialGame`) with seven phases that crossfade between each other:

```
Title Screen → Knight Intro → Fraction Lesson → Practice (10 Qs) → Results → Farewell → Level Map
```

1. **Title + Level Map** — place a robot piece on the board to begin
2. **Knight Intro** — narrated introduction with karaoke-style word highlighting
3. **Chalkboard Lesson** — animated fraction equivalence walkthrough (e.g. 1/2 = 3/6)
4. **Guided Practice** — 10 multiple-choice questions with instant feedback; wrong answers trigger step-by-step explanations
5. **Results** — level-up animation, title promotion, XP display
6. **Knight Farewell** — closing narration
7. **Level Map** — world progression (future)

## Project Structure

```
Assets/
├── Scripts/
│   ├── Core/           # GameManager (state machine, scoring, scaffolding)
│   ├── Game/           # Phase managers (Intro, Lesson, Practice, Results, Outro)
│   ├── Lessons/        # LessonSequencer, FractionProblem, GuidedExplanationHelper
│   ├── Audio/          # GameAudioManager (BGM + SFX)
│   ├── Input/          # PieceManager (Board SDK piece detection + events)
│   ├── Navigation/     # Landing page, title screen, scene loading
│   ├── UI/             # FractionCircle, PieceVisualizer, StrokeRevealMask
│   └── Playground/     # Free-play exploration mode
├── Scenes/             # Unity scenes (OfficialGame is the main entry point)
├── Resources/          # Runtime-loaded audio (Music/, SFX/, TTS/)
├── Textures/           # Backgrounds, chalkboard layers, intro screens
├── Sprites/            # UI graphics and block visuals
└── Fonts/              # TextMesh Pro font assets
```

### Key Scripts

| Script | Purpose |
|--------|---------|
| `OfficialGameManager` | Master orchestrator — manages phase transitions with CanvasGroup crossfades |
| `GameManager` | Singleton state machine — tracks score, scaffolding level (E1–E4), game state |
| `LessonSequencer` | Step-by-step playback engine with barrier sync (animation + subtitle + TTS) |
| `PieceManager` | Polls Board SDK contacts each frame, fires piece placed/moved/removed events |
| `GuidedExplanationHelper` | Builds fraction equations at runtime with dwell-time answer detection |
| `FractionsDemo5Manager` | 10-question practice loop with score tracking and explanation fallback |
| `GameAudioManager` | BGM playback with ducking during lessons, one-shot SFX for feedback |

## Getting Started

### Prerequisites

- Unity 6.4+ (6000.4.1f1)
- Board SDK developer access ([dev.board.fun](https://dev.board.fun))

### Setup

1. Clone the repo
2. Open the project in Unity Hub
3. The Board SDK package is included at `Packages/fun.board-3.3.0.tgz`
4. Open `Assets/Scenes/OfficialGame.unity` to load the main game scene

### Testing in the Editor

Use the Board SDK simulator to test piece interactions without a physical board:

1. **Board > Input > Simulator** in the Unity menu
2. Select a robot glyph from the simulator panel
3. Click in the Game view to place a piece
4. Mouse angle controls piece orientation

## Architecture Notes

- **No scene loading** — phases live as CanvasGroups in one scene and crossfade (0.6s)
- **Barrier sync** — `LessonSequencer.RunStep()` runs animation, subtitle, and TTS in parallel, waiting for all three before advancing
- **Scaffolding** — four difficulty levels (E1 Learn → E2 Practice → E3 Challenge → E4 Master); 3 correct in a row advances the level
- **BGM ducking** — music pauses during lesson/practice phases for focus, resumes for intro/outro

## License

All rights reserved.
