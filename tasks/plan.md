# Implementation Plan: Flight Combat Sandbox

## Overview

Build the game described in [docs/SPEC.md](../docs/SPEC.md) in six steps, each ending with something playable. Words follow [GLOSSARY.md](../GLOSSARY.md); hard-to-reverse decisions live in [docs/adr/](../docs/adr/). The task list is [todo.md](todo.md).

## Architecture decisions

- [0001](../docs/adr/0001-single-player-only.md): single-player only.
- [0002](../docs/adr/0002-every-mission-built-in-the-editor.md): every Mission is built with the player-facing editor.
- [0003](../docs/adr/0003-godot-dotnet-with-csharp.md): Godot 4 (.NET) with C#.
- [0004](../docs/adr/0004-original-universe.md): original universe; the user's personal music stays in a private Pack.
- [0005](../docs/adr/0005-mission-logic-is-trigger-lists.md): Mission logic is written as trigger lists.
- [0006](../docs/adr/0006-phases-not-a-clock.md): the Mission timeline is Phases, not a clock.
- [0007](../docs/adr/0007-packs-and-mission-files.md): content comes in Packs; Missions are plain data.
- [0008](../docs/adr/0008-godot-free-core.md): Mission rules live in a Godot-free Core.
- [0009](../docs/adr/0009-arcade-flight-model.md): arcade flight model driven by Handling values.

## Project layout

- `game/`: the Godot project (Game). Scenes and scripts sit together in feature folders: `flight/`, `combat/`, `ai/`, `world/`, `ui/`, `editor/`.
- `core/` and `core.tests/`: Core, plain C# with xUnit tests (created in task 1.4). Pack data (Unit Types, Handling, feel settings) lives here too, since Missions refer to it.
- `packs/base/`: the base Pack (created in task 1.5). Exported builds carry it next to the .exe.
- `missions/`: hand-written dev sample Missions (step 3).
- At runtime, the user's own Packs and Missions live in Godot's user folder (`user://packs`, `user://missions`). The user's private music Pack goes there, never in this repository.

## Testing

- Core: test-first with xUnit (`dotnet test core.tests`).
- Game: build plus the headless Godot check in [CLAUDE.md](../CLAUDE.md); anything on screen is verified by the user playing it.

## Roadmap

Sizes are rough guesses at 10-20 hours a week; re-estimate after Milestone 1.

1. **Fly** (tasks 1.x): a gray-box jet over generated test terrain, tuned live. *Done when flying feels right to the user.* (~2-4 weeks) The first flight (1.6) comes before terrain (1.21) so feel work starts early.
2. **Fight** (2.x): lock-on, missiles, gun, Special Weapons, HUD and radar, enemy jets with basic dogfight AI, ground targets. *Done when fighting four enemy jets is fun.* (~4-8 weeks)

   **Milestone 1: is it fun?**

3. **Missions** (3.x): Core runs hand-written Mission files on top of step 2. *Done when a two-Phase Mission can be won and lost.* (~4-8 weeks)
4. **Editor** (4.x): build, Simulate and play Missions in the game. *Done when the user rebuilds step 3's Mission without touching a file.* (~2-4 months)

   **Milestone 2: the sandbox loop works.**

5. **Packs** (5.x): extra Packs and Pack code. *Done when a second Pack adds a jet and a Trait without changing the game.* (~2-4 weeks)
6. **Depth** (6.x): the real-terrain Map, all base Unit Types, the Commands menu, weather, music, Checkpoints, low-poly models. Ongoing.

## Risks and mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Flight never feels right | High | Every Handling value is a knob with a live tuning panel; first flight lands at 1.6; the user signs off feel before combat work. |
| Dogfight AI is helpless or perfect | High | AI flies the same flight model through synthesized inputs; a 1v1 checkpoint comes before adding more enemies; Ace and Stances tune it later. |
| The editor (step 4) balloons | High | The Mission file format is proven by hand in step 3 first; editor tasks are small and each one is usable on its own. |
| Learning curve: the user is new to game engines | Medium | Small tasks; Godot concepts explained in plain words; the user plays every visible change. |
| Godot only loads mod C# as plain classes | Medium | Pack code registers catalog entries through Core interfaces; Pack scenes carry no C# scripts ([ADR 0007](../docs/adr/0007-packs-and-mission-files.md)). |
| 64 km Maps: float precision and terrain rendering | Medium | Small test terrain until step 6; floating origin (6.1) and a terrain spike (6.3) come before the real Map. |
| Copyrighted music ends up in the repository | Medium | The private music Pack lives in the user folder ([ADR 0004](../docs/adr/0004-original-universe.md)). |
| Rules engine grows tangled | Medium | Core tests run it against a fake world; the catalog grows one event, condition or action per task. |
| No art skills | Low | Gray boxes until step 6; CC0 and CC-BY assets listed in `packs/base/CREDITS.md`. |

## Open questions

- DualSense default layout: proposed in task 1.3; the user confirms while playing.
- Air-to-ground Special Weapon: missile or bombs (task 2.21).
- Whether Core projects join Godot's `.sln` (task 1.4). If they do, map its ExportDebug and ExportRelease configurations to Debug and Release.
- Which real-world region the 64 km Map comes from (task 6.2).
- Terrain technology: the Terrain3D plugin or a custom heightmap mesh (task 6.3, recorded as an ADR).
