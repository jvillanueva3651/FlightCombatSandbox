# Flight Combat Sandbox: Plan

A single-player arcade flight combat game in the spirit of Ace Combat, where Authors build Missions in an in-game editor and Players fly them: Super Mario Maker for air combat. A personal project for now, built by you and Claude at 10-20 hours a week; maybe public later.

- Words we use: [GLOSSARY.md](../GLOSSARY.md)
- Hard-to-reverse decisions and why: [adr/](adr/)
- This file: what the first version contains, and the order we build it in.

## What we're building

### Flying and fighting

- **Platform:** Windows PC. DualSense first, with Expert controls (direct pitch, roll and yaw). Editor on keyboard and mouse. English only.
- **Flight model:** arcade, driven by Handling values ([ADR 0009](adr/0009-arcade-flight-model.md)).
  - Per aircraft, in its Unit Type: top and stall speed, throttle and brake strength, pitch/roll/yaw rates at each speed (best at corner speed), speed lost in hard turns, speed gained or lost climbing and diving, high-G turn strength, response time.
  - Shared by all aircraft: stick dead zone and response curve, camera lag, zoom-out at speed, camera shake, overall multipliers on Handling.
  - A live tuning panel changes any value mid-flight and saves it back to the file.
- **Ace Combat rules:** health bars, no fuel, air starts, lock-on HUD and radar, no friendly fire, crashing counts as being shot down, leaving the Map gives a warning and then fails the Mission.
- **Ruleset,** set per Mission by the Author: allowed aircraft or one enforced jet; missiles (default 144); gun (default on); Special Weapons (default 2).
- Players fly fighters, multiroles and attackers, never bombers.
- **Score and Rank:** points per Unit Type, which the Author can override per Group; S/A/B/C Rank from score and time, with thresholds set by the Author or worked out automatically.
- No difficulty settings, no unlocks, no currency.
- **Mission flow:** briefing (text and map) → hangar (jet and Special Weapons within the Ruleset) → fly → results.

### Missions

- **Contents:** a Map; Teams grouped into Alliances (new Missions start as Team 1, the Player's, against Team 2); Groups of Units, each with an Order, a Stance and Traits; Routes; Zones, optionally limited to an altitude band; Objectives; Triggers; Variables; weather and time of day; music.
- **Phases** ([ADR 0006](adr/0006-phases-not-a-clock.md)) run in a straight line. A Phase ends when its Objectives are done, when its time limit runs out (the Mission moves on), or when a Trigger ends it. Objectives and Triggers belong to one Phase or to the whole Mission. Units from earlier Phases stay unless told to leave.
- **Winning and losing:** win when the last Phase ends; lose when a required Objective fails or the Player is shot down. Optional Objectives from a finished Phase are missed. An Objective checks the world when it activates, so a convoy destroyed early completes a later "destroy the convoy" at once.
- **Checkpoints** at Phase starts arrive in step 6, but Mission state is saveable from day one.
- **Targets:** Units of hostile Teams, plus Scenery the Mission gives to a hostile Team. All other Scenery can't be destroyed.
- **Objectives:** Destroy, Protect, Reach, Survive, Score, Destruction, Custom. Each is required or optional, can stay hidden until a Trigger reveals it, and can have a time limit that fails it. HUD: an OBJ marker for things to destroy, a separate marker for things to protect.
- **Commands:** a real-time radial menu with no pausing.
  - Quick selections: all fighters, fighters ahead of me, all bombers, ground units below me, everything under my command.
  - Commands: Attack my target, Cover me, Engage at will, Resume orders, Bombing run (bombers and attackers), Take Zone (ground units), Retreat.
  - A Command overrides the Group's Order until it's done or the Player says "Resume orders"; then the Units go back to their Group's current Order.
  - Only Units the Author put under the Player's command can be selected. Triggers can give or take away command.
- **Wingmen** are an allied Group ordered to escort the Player and put under the Player's command. AI aircraft start in the air.

### Building blocks in the first version

- **Trigger events:** a Mission or Phase starts or ends; N seconds pass (once, or every N); a Unit, Group or Scenery Target is destroyed; a Unit is damaged; a Unit enters or leaves a Zone; a Group finishes its Route; a Unit is spotted by an enemy; an Objective completes or fails; a Variable changes.
- **Conditions,** all of which must hold: compare a Variable; count living Units in a Group, Team or Zone; a Unit's health, altitude or speed; a Unit is in a Zone; an Objective's state; the current Phase; random chance.
- **Actions:** bring in a hidden Group; remove a Group; give an Order; set a Stance; add or remove a Trait; give or take Player command; give Scenery to a Team or take it back; reveal, complete or fail an Objective; set or add to a Variable; end the Phase; win or lose; radio message (speaker and subtitle); change weather or time of day; change music; rearm the Player; wait N seconds.
- Triggers fire once unless set to repeat. Triggers firing at the same moment run top to bottom.
- **Orders.** Aircraft: Fly Route (once or looping), Patrol Zone, Attack, Escort, Strike Zone, Engage at will, Retreat. Ground vehicles: Drive Route, Hold position, Take Zone, Attack, Retreat. Ships: Sail Route, Hold position, Retreat. Static Units only use their weapons. Every Order has a hold-fire switch.
- **Stances:** Balanced (default), Aggressive, Covering, Baiting.
- **Traits:** Stealth, Invulnerable, Ace.

### Editor

- In the game, in 3D: a top-down camera by default, a free camera for detail work, and an altitude for everything placed.
- A Phase timeline, with markers for Groups that arrive partway through a Phase.
- **Simulate** runs the AI only, at up to 8x speed, and resets when stopped. **Play from here** starts at any Phase from the camera's position. No scrubbing backwards.
- Logic is written as trigger lists built from dropdowns ([ADR 0005](adr/0005-mission-logic-is-trigger-lists.md)).

### Content and files ([ADR 0007](adr/0007-packs-and-mission-files.md))

- **Packs** are plain folders: a manifest, data files, glTF models, images, sounds, and optionally one C# library. The base game is the first Pack, later Packs override earlier ones, and the game warns before enabling a Pack with code.
- **Missions** are one versioned JSON file listing the Packs they need, never code. They're shared by sending the file, and opening one with missing Packs names them.
- **Base Pack target:**
  - Aircraft: light fighter, multirole, attacker (flyable); bomber (AI only).
  - Special Weapons: agile short-range missile, long-range missile, air-to-ground missile or bombs.
  - Ground and sea: AA gun, SAM site, radar, tank, truck, frigate, bunker.
  - Map: one 64 km Map built from real elevation data, with mountains, a canyon, a coast, a city split into separate blocks, an airbase, a bridge and a refinery.
  - Gray placeholder models first, then free low-poly models listed in a credits file.
- **Weather and time of day** are Mission settings that Triggers can change. Looks only, for now.
- **Music:** Packs carry tracks, and each Phase can set one, alongside the "change music" action. Your Ace Combat and Project Wingman tracks live in a private Pack outside this repository and never ship ([ADR 0004](adr/0004-original-universe.md)).

### Tech

- Godot 4 (.NET edition) with C#, Forward+ renderer ([ADR 0003](adr/0003-godot-dotnet-with-csharp.md)).
- **Core** is plain C# tested with `dotnet test`: the Mission model, Mission files, the rules engine and the catalog. **Game** is the Godot project: flight, combat, AI, rendering, HUD, input and the editor ([ADR 0008](adr/0008-godot-free-core.md)).
- Single-player only ([ADR 0001](adr/0001-single-player-only.md)).

## Roadmap

Each step ends with something playable. Sizes are rough guesses at 10-20 hours a week; we re-estimate after Milestone 1.

1. **Fly:** a gray-box jet over generated test terrain, tuned live. *Done when flying feels right to you.* (~2-4 weeks)
2. **Fight:** lock-on, missiles, gun, Special Weapons, HUD and radar, enemy jets with basic dogfight AI, ground targets. *Done when fighting four enemy jets is fun.* (~4-8 weeks)

   **Milestone 1: is it fun?**

3. **Missions:** Core runs hand-written Mission files on top of step 2. *Done when a two-Phase Mission can be won and lost.* (~4-8 weeks)
4. **Editor:** build, Simulate and play Missions in the game. *Done when you can rebuild step 3's Mission without touching a file.* (~2-4 months)

   **Milestone 2: the sandbox loop works.**

5. **Packs:** extra Packs and Pack code. *Done when a second Pack adds a jet and a Trait without changing the game.* (~2-4 weeks)
6. **Depth:** the real-terrain Map, all base Unit Types, the Commands menu, radio, weather, Checkpoints, music, low-poly models. Ongoing.

## Milestone 1 tasks

### Step 1: Fly

- [ ] You: install Godot 4 (.NET edition, latest stable) and the .NET SDK version its docs list.
- [ ] Godot project with the Forward+ renderer, a C# solution, and a .gitignore for Godot and .NET.
- [ ] Generated test terrain with hills and a canyon to fly through.
- [ ] Gray-box jet built from simple shapes, flown by the arcade flight model.
- [ ] Handling values loaded from the jet's file in the base Pack folder.
- [ ] DualSense Expert controls with an Ace Combat-style default layout.
- [ ] Chase camera with lag, zoom-out at speed and shake, driven by the shared feel settings.
- [ ] Debug readout: speed, altitude, throttle, G.
- [ ] Live tuning panel: sliders for every Handling and feel value, saved back to the files.
- [ ] Done: you fly the test terrain and the jet feels right.

### Step 2: Fight

- [ ] Health, damage and destruction; crashing counts as being shot down.
- [ ] Lock-on with target cycling and HUD target markers.
- [ ] Standard missiles (144 by default) and the gun.
- [ ] Two Special Weapon slots, filled from the base list.
- [ ] HUD (speed, altitude, missiles, health) and radar minimap.
- [ ] Enemy jets of the same Unit Type with basic dogfight AI: chase, fire, evade.
- [ ] Ground targets: an AA gun that shoots back, and a tank.
- [ ] A throwaway test scene with four enemy jets and a few ground targets. It's a dev scene, not a Mission, so [ADR 0002](adr/0002-every-mission-built-in-the-editor.md) still holds; step 3 replaces it.
- [ ] Done: fighting four enemy jets is fun.

## How we work

- One task at a time, and play it before moving on.
- Each finished task gets its own commit.
- New terms go in the glossary; hard-to-reverse decisions get an ADR.
