---
date: 2026-04-05T15:51:52-05:00
researcher: jackie
git_commit: null
branch: null
repository: board
topic: "Board of Education - Educational Math Game Setup"
tags: [unity, board-sdk, csharp, educational-game, fraction-explorers, playcademy]
status: complete
last_updated: 2026-04-05
last_updated_by: jackie
type: implementation_strategy
plan_path: null
plan_status: null
plan_phase: null
---

# Handoff: Board of Education - Fraction Explorers Setup

## Task(s)

- **Completed**:
  - Installed Unity Hub via Homebrew and Unity 6.4 (6000.4.1f1) with Android Build Support
  - Installed Board SDK v3.3.0 (fun.board-3.3.0.tgz) into Unity project
  - Installed BDB (Board Developer Bridge) to `~/.local/bin/bdb`
  - Ran Board > Configure Unity Project wizard (Android, API 33, IL2CPP, Landscape Left)
  - Created 9 C# game scripts covering: game state, piece input, UI, fraction lessons, logging, progression, scene building
  - Built game scene via Board of Education > Build Game Scene menu
  - Title screen "BOARD OF EDUCATION - Fraction Explorers" renders correctly with START GAME button
  - Board Simulator confirmed working and showing Board Arcade piece set (7 pieces, Glyphs 0-6)

- **In Progress**:
  - Resolving Input System conflict (Unity restart needed for changes to take effect)
  - Scene not yet saved post-restart

- **Planned/Discussed**:
  - Hook up fraction problem gameplay loop end-to-end (pieces selecting fraction answers)
  - 2-player split layout with per-player feedback
  - E1-E4 scaffold progression based on player performance
  - CSV interaction logging for Cognitive Genome
  - Journey map progression system (ProgressionManager)
  - Android build and deploy to Board.fun console

## Critical References

1. Board SDK API docs: https://docs.dev.board.fun - specifically `BoardInput.GetActiveContacts()`, `BoardContact` struct, `BoardContactPhase`
2. `/Users/jackie/src/board/Assets/Scripts/Core/SceneBuilder.cs` - editor script that rebuilds the entire scene hierarchy; run via Board of Education > Build Game Scene
3. `/Users/jackie/src/board/ProjectSettings/ProjectSettings.asset` - contains `activeInputHandler` setting that the SDK wizard keeps resetting to 1 (New Input System only)

## Recent Changes

- `/Users/jackie/src/board/ProjectSettings/ProjectSettings.asset` - `activeInputHandler` manually changed from 1 to 2 (Both) to allow old `UnityEngine.Input` alongside New Input System
- `/Users/jackie/src/board/Assets/Scripts/Input/PieceManager.cs` - mouse simulation fallback wrapped in `#if !BOARD_SDK / #endif` to suppress errors when Board SDK is present
- `/Users/jackie/src/board/Assets/Scripts/Core/SceneBuilder.cs` - builds full scene; currently adds `StandaloneInputModule` to EventSystem which conflicts with New Input System (needs `InputSystemUIInputModule` instead)

## Learnings

- **Input System conflict**: The Board SDK Configure wizard forces `activeInputHandler = 1` (New Input System only). Any code using legacy `UnityEngine.Input` (mouse, keyboard) will spam errors every frame. After manually setting it to 2 (Both), Unity must be fully restarted before the change takes effect. Do NOT re-run the Configure wizard or it resets to 1.
- **EventSystem module mismatch**: When using the New Input System, `StandaloneInputModule` on the EventSystem GameObject must be replaced with `InputSystemUIInputModule`. SceneBuilder.cs currently adds the wrong one. After Build Game Scene, manually: select EventSystem in Hierarchy, Remove `StandaloneInputModule`, Add `Input System UI Input Module`. OR fix SceneBuilder.cs to use `typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule)`.
- **Board SDK shortcut**: Board > Input > Add BoardUIInputModule to EventSystems does this automatically - prefer this over manual steps.
- **Strata piece set not available**: The Strata piece set referenced in some SDK examples is not on the dev portal. Board Arcade pieces (Glyphs 0-6: Robot Yellow/Purple/Orange/Pink, Ship Pink/Yellow/Purple) are used instead. These map to fraction values in `LessonController.cs`.
- **Android build disabled**: "Switching to AndroidPlayer is disabled" error may indicate the Android Build Support module needs reinstallation via Unity Hub.
- **SceneBuilder re-run**: Every time "Build Game Scene" is run, it recreates the entire scene from scratch. Any manual edits to the scene (adding components, tweaking positions) will be wiped. Prefer encoding changes in SceneBuilder.cs.
- **User is new to Unity**: Provide step-by-step editor instructions (e.g., "Click the GameObject named X in the Hierarchy panel, then in the Inspector click Add Component...").

## Artifacts

All game scripts (9 total, spread across subdirectories):

- `/Users/jackie/src/board/Assets/Scripts/GameManager.cs` - game state machine, E1-E4 scaffold progression
- `/Users/jackie/src/board/Assets/Scripts/Core/BoardStartup.cs` - boot config (60fps, Landscape Left orientation)
- `/Users/jackie/src/board/Assets/Scripts/Core/SceneBuilder.cs` - editor menu script, rebuilds full scene
- `/Users/jackie/src/board/Assets/Scripts/Input/PieceManager.cs` - Board SDK piece detection, mouse fallback (guarded by `#if !BOARD_SDK`)
- `/Users/jackie/src/board/Assets/Scripts/Input/PieceVisualizer.cs` - renders piece icons on screen
- `/Users/jackie/src/board/Assets/Scripts/Lessons/LessonController.cs` - fraction problem management, scaffold levels
- `/Users/jackie/src/board/Assets/Scripts/Lessons/FractionProblem.cs` - 10 fraction equivalence problems with E1-E4 instruction text
- `/Users/jackie/src/board/Assets/Scripts/Logging/InteractionLogger.cs` - CSV logging for Cognitive Genome
- `/Users/jackie/src/board/Assets/Scripts/Progression/ProgressionManager.cs` - journey map progression system
- `/Users/jackie/src/board/Assets/Scripts/UI/UIManager.cs` - 2-player split layout UI

SDK and tooling:
- `/Users/jackie/src/board/Packages/fun.board-3.3.0.tgz` - Board SDK v3.3.0 (local tarball)
- `/Users/jackie/src/board/Packages/manifest.json` - Unity package manifest referencing the SDK
- `~/.local/bin/bdb` - Board Developer Bridge CLI
- `/Users/jackie/board.fun stuff/` - original SDK downloads directory

## Action Items & Next Steps

- [ ] **Restart Unity** - changes to `activeInputHandler` in ProjectSettings.asset will not take effect until Unity is fully quit and relaunched
- [ ] After restart, run **Board > Input > Add BoardUIInputModule to EventSystems** to fix the EventSystem input module conflict
- [ ] Save the scene (Ctrl+S / Cmd+S) after verifying no console errors on startup
- [ ] Fix `SceneBuilder.cs` to use `InputSystemUIInputModule` instead of `StandaloneInputModule` so future "Build Game Scene" runs don't recreate the conflict
- [ ] Wire up the gameplay loop: piece contact -> answer selection -> fraction comparison -> feedback. Start in `PieceManager.cs` (already polls `BoardInput.GetActiveContacts()`) and connect to `LessonController.cs`
- [ ] Test with Board Simulator: Board > Input > Simulator, click a piece icon, click in Game view, confirm piece contact is detected and a fraction answer registers
- [ ] Implement per-player feedback UI in `UIManager.cs` (correct/incorrect flash, score update)
- [ ] If Android build stays broken ("Switching to AndroidPlayer is disabled"), reinstall Android Build Support via Unity Hub > Installs > Unity 6000.4.1f1 > Add Modules
- [ ] When ready to deploy, use BDB: `~/.local/bin/bdb build` and `~/.local/bin/bdb deploy` (check `bdb --help` for exact flags)

## Other Notes

- **Challenge deadline**: This is a 1-week Playcademy/Superbuilders challenge. Prioritize a playable loop over polish.
- **Two-player design**: The game targets simultaneous 2-player play on a single Board console. Each player places pieces in their half of the screen. `UIManager.cs` has a split-screen layout scaffold but it needs wiring to actual player data.
- **Cognitive Genome logging**: `InteractionLogger.cs` writes CSV logs. The format should conform to whatever the Cognitive Genome spec requires - verify the column names/format when that integration is needed.
- **Piece-to-fraction mapping**: The 7 Board Arcade glyphs (0-6) are currently mapped to fraction values in `LessonController.cs`. Confirm this mapping makes pedagogical sense before finalizing.
- **Memory files**: Prior session context saved at `/Users/jackie/.claude/projects/-Users-jackie-src-board/memory/`
