# Tasks

Take the first unchecked task whose dependencies are done, do only that task, tick it, and commit. Design details: [docs/SPEC.md](../docs/SPEC.md). Layout, risks and open questions: [plan.md](plan.md).

**Format:** ID and title · size · after (dependencies), then *Done when* and *Verify*. Steps 1 and 2 also list the files likely touched; later steps get files when they start. IDs never change: a task added later takes the next free number in its step and sits where it belongs in the order.

**Sizes:** XS one file · S one or two files · M three to five files.

**Verify shorthands:**
- **build:** `dotnet build game/FlightCombatSandbox.sln` succeeds with no new warnings.
- **test:** the task's Core behavior has tests written first, and `dotnet test core.tests` passes.
- **godot:** the headless check in [CLAUDE.md](../CLAUDE.md) exits 0 with no errors.
- **play:** the user runs the game and confirms *Done when*.

From step 3 on, *Verify* is **test** for Core behavior and **play** for anything on screen, plus **build** and **godot** always.

## Done

- [x] **0.1 Install Godot 4.7.2 (.NET) and the .NET SDK**
- [x] **0.2 Godot project in `game/` with a C# solution and `.gitignore`**

## Step 1: Fly

- [x] **1.1 Test scene: ground, sky, sun** · XS · after 0.2
  - Done when running the project shows a large flat ground grid under a sky with sunlight.
  - Verify: godot, play. Files: `game/world/test_flight.tscn`, `game/project.godot` (main scene).
- [x] **1.2 Gray-box jet** · S · after 1.1
  - Done when a jet built from simple shapes (fuselage, wings, tail), nose along -Z (Godot's forward), sits above the ground in the test scene.
  - Verify: godot, play. Files: `game/flight/jet.tscn`, `game/world/test_flight.tscn`.
- [x] **1.3 Controls: DualSense layout with keyboard fallback** · S · after 1.1
  - Done when input actions exist for pitch, roll, yaw, throttle, brake, fire missile, fire gun, switch weapon, switch target, look around, change view, flare, radar map and pause, and an on-screen readout shows their live values from both the DualSense and the keyboard.
  - Layout, matching Ace Combat 7's PlayStation defaults: left stick pitch and roll; right stick look around; L1/R1 yaw; R2 throttle; L2 brake; Cross gun; Circle missile or Special Weapon; Square switch weapon; Triangle switch target; R3 change view; L3+R3 flare; touchpad radar map; Options pause. Holding Triangle or Circle does more in tasks 2.27 and 2.28. Godot names face buttons by Xbox position: A = Cross, B = Circle, X = Square, Y = Triangle.
  - Verify: godot, play. Files: `game/flight/Controls.cs` (actions defined in code so bindings stay readable), `game/ui/debug_overlay.tscn`, `game/ui/DebugOverlay.cs`.
- [x] **1.4 Core library with Handling** · M · after 0.2
  - Done when `core/` (plain C#, net8.0) has a Handling type (cruise speed; pitch, roll and yaw rates) read from JSON, `core.tests/` (xUnit) tests that load, and the Godot project references Core and still builds.
  - Verify: test, build, godot. Files: `core/Core.csproj`, `core/Handling.cs`, `core.tests/Core.Tests.csproj`, `core.tests/HandlingTests.cs`, `game/FlightCombatSandbox.csproj`.
- [x] **1.5 Base Pack with the first jet** · S · after 1.4
  - Done when `packs/base/` holds `pack.json` (id, name, version) and the light fighter's Unit Type file with its Handling, and the game finds the base Pack (beside `game/` in the editor, beside the .exe in builds) and logs the loaded Handling at startup.
  - Verify: test, godot, play (log line). Files: `packs/base/pack.json`, `packs/base/unit-types/light-fighter.json`, `core/PackLoader.cs`, `game/content/Packs.cs`.
- [x] **1.6 First flight** · S · after 1.2, 1.3, 1.5
  - Done when the jet flies forward at cruise speed and turns with the stick and L1/R1 at its Handling rates, with a camera fixed behind it.
  - Verify: godot, play. Files: `game/flight/Jet.cs`, `game/flight/jet.tscn`.

**Checkpoint: first flight.** The user flies the jet and notes how it feels before more flight features land.

- [x] **1.26 Pause** · S · after 1.6
  - Done when Options (or Escape) pauses the game and shows a menu with Resume and Quit.
  - Verify: play. Files: `game/ui/pause_menu.tscn`, `game/ui/PauseMenu.cs`.
- [x] **1.7 Flight readout** · XS · after 1.6
  - Done when the debug overlay shows speed, altitude, throttle and G-load.
  - Verify: play. Files: `game/ui/DebugOverlay.cs`.
- [x] **1.8 Throttle and brake** · S · after 1.7
  - Done when R2 accelerates toward top speed, L2 slows toward minimum speed (set below where the jet will stall, so braking can stall it), and letting go returns to cruise speed, all at Handling rates.
  - Verify: test, play. Files: `core/Handling.cs`, `packs/base/unit-types/light-fighter.json`, `game/flight/Jet.cs`.
- [x] **1.9 Response time** · S · after 1.6
  - Done when turn rates ease toward the stick position over the Handling response time instead of snapping, so a higher value feels heavier.
  - Verify: test, play. Files: `core/Handling.cs`, `packs/base/unit-types/light-fighter.json`, `game/flight/Jet.cs`.
- [x] **1.10 Turn rates change with speed** · S · after 1.8
  - Done when a Core function gives a turn-rate scale from speed (best at the corner speed, weaker toward stall and top speed), and the jet uses it.
  - Verify: test, play. Files: `core/Handling.cs`, `core.tests/HandlingTests.cs`, `game/flight/Jet.cs`.
- [x] **1.11 Speed lost in hard turns** · XS · after 1.10
  - Done when pulling hard bleeds speed at a Handling rate.
  - Verify: play. Files: `core/Handling.cs`, `game/flight/Jet.cs`.
- [x] **1.12 Climbing and diving change speed** · XS · after 1.8
  - Done when climbing slows the jet and diving speeds it up, scaled by a Handling value, without climbing alone ever slowing the jet enough to stall.
  - Verify: play. Files: `core/Handling.cs`, `game/flight/Jet.cs`.
- [x] **1.13 Stall** · S · after 1.10, 1.12
  - Done when below stall speed the controls weaken and the nose drops until the dive restores speed.
  - Verify: play. Files: `core/Handling.cs`, `game/flight/Jet.cs`.
- [x] **1.14 High-G turn** · S · after 1.11
  - Done when holding L2 and R2 together while pulling tightens the turn by the Handling high-G strength and bleeds speed fast.
  - Verify: play. Files: `core/Handling.cs`, `game/flight/Jet.cs`.

**Checkpoint: flight model complete.** The user flies everything above and lists what feels off.

- Flown 2026-10-06, together with tasks 1.15 to 1.18, 1.23 and 1.27. Passed: turns easing in and out, turning best at corner speed, speed lost in hard turns, tuning panel, Feel Settings, chase camera, speed zoom and shake, look around. The stall stays gradual.
- Changed in `light-fighter.json`: `topSpeed` 1300 → 2500 ("for now"), `throttleStrength` 100 → 200, `minSpeed` 200 → 100 with `stallSpeed` left at 300 (so a slow jet stays stalled for a while before the throttle pulls it out), `stallDropRate` 90 → 150, `gravityStrength` 150 → 75 (full throttle straight up now gains 125 km/h per second), `highGSpeedLoss` 150 → 100 (a full high-G turn costs about 140 km/h per second, and can still drag the jet into a stall).
- Look at again later: climbing and diving (the least settled; full throttle straight up must beat gravity by a lot), the stall, the high-G speed loss, `topSpeed`, and the Feel Settings' response curve.

- [x] **1.15 Feel Settings and stick response** · S · after 1.6
  - Done when `packs/base/feel.json`, loaded into a Core type, sets the stick dead zone and response curve, and the jet's input uses them.
  - Verify: test, play. Files: `packs/base/feel.json`, `core/FeelSettings.cs`, `core.tests/FeelSettingsTests.cs`, `game/flight/Jet.cs`.
- [x] **1.16 Overall Handling multipliers** · XS · after 1.15
  - Done when Feel Settings hold multipliers (speed, pitch, roll, yaw, response) applied on top of every aircraft's Handling.
  - Verify: test, play. Files: `core/FeelSettings.cs`, `game/flight/Jet.cs`.
- [x] **1.17 Chase camera** · S · after 1.15
  - Done when the camera follows the jet with smoothing set by the Feel Settings' camera lag, instead of being fixed behind it.
  - Verify: play. Files: `game/flight/ChaseCamera.cs`, `game/world/test_flight.tscn`.
- [x] **1.18 Speed zoom and camera shake** · S · after 1.17, 1.14
  - Done when the field of view widens with speed and the camera shakes in high-G turns and stalls, by amounts from the Feel Settings.
  - Verify: play. Files: `game/flight/ChaseCamera.cs`, `core/FeelSettings.cs`.
- [x] **1.27 Look around** · S · after 1.17
  - Done when the right stick swings the camera around the jet, and the camera eases back behind the jet when the stick is let go.
  - Verify: play. Files: `game/flight/ChaseCamera.cs`.
- [x] **1.28 Change view** · S · after 1.17
  - Done when R3 switches between the chase camera and a view from the jet's nose with the jet hidden, and L3+R3 for flares never switches the view, whichever stick is clicked first. A cockpit view comes with models in 6.25.
  - Verify: play. Files: `game/flight/ChaseCamera.cs`, `game/flight/jet.tscn`.
- [x] **1.19 Speed cues** · S · after 1.6
  - Done when faint wind streaks around the camera grow with speed, so speed reads even high above the ground.
  - Verify: play. Files: `game/flight/wind_streaks.tscn`, `game/flight/ChaseCamera.cs`.
- [x] **1.20 Engine and wind sound** · S · after 1.8
  - Done when placeholder engine and wind sounds, generated in code (the user's pick over CC0 files), play with pitch and volume following throttle and speed.
  - Verify: play. Files: `game/flight/JetSound.cs`, `game/flight/jet.tscn`.
- [x] **1.21 Generated test terrain** · M · after 1.6
  - Done when the flat ground is replaced by generated terrain about 16 km square, with hills and a winding canyon, and with collision.
  - Verify: godot, play. Files: `game/world/TestTerrain.cs`, `game/world/test_flight.tscn`.
- [x] **1.22 Crash and respawn** · S · after 1.21
  - Done when hitting the terrain resets the jet to its start after a short pause. Step 2 turns this into being shot down.
  - Verify: play. Files: `game/flight/Jet.cs`.
- [x] **1.23 Tuning panel: Handling** · M · after 1.14
  - Done when a key toggles an in-game panel with a slider for every Handling value, built from the type's properties, and changes apply mid-flight.
  - Verify: play. Files: `game/ui/tuning_panel.tscn`, `game/ui/TuningPanel.cs`.
- [x] **1.24 Tuning panel: Feel Settings** · S · after 1.23, 1.18
  - Done when the panel also shows every feel setting and multiplier.
  - Verify: play. Files: `game/ui/TuningPanel.cs`.
- [x] **1.25 Tuning panel: save and reset** · S · after 1.24
  - Done when Save writes the current values back to the jet's file and `feel.json` in the same JSON shape, and Reset reloads them from disk.
  - Verify: test (save and reload round-trip), play. Files: `core/PackLoader.cs`, `core.tests/HandlingTests.cs`, `game/ui/TuningPanel.cs`.

**Checkpoint: Step 1 done.** The user flies the test terrain and says the jet feels right.

- Signed off 2026-10-06: the user flew the test terrain, tried the tuning panel's new tab and buttons, and said everything looks good. No values changed.

## Step 2: Fight

- [x] **2.1 Unit Types gain health, points and Role** · S · after 1.5
  - Done when Core's Unit Type has health, points and Role (fighter, multirole, attacker, bomber, armor or anti-air; later tasks add Roles as their Unit Types need them), and the light fighter's file sets them.
  - Verify: test. Files: `core/UnitType.cs`, `core.tests/UnitTypeTests.cs`, `packs/base/unit-types/light-fighter.json`.
- [x] **2.2 Health and destruction** · S · after 2.1
  - Done when placed Units have a Team and health; at zero health they explode (placeholder flash and debris) and disappear; a gray-box dummy on Team 2 sits in the test scene, and a debug key damages it.
  - Verify: play. Files: `game/combat/Unit.cs`, `game/combat/explosion.tscn`, `game/combat/Explosion.cs`, `game/world/test_flight.tscn`.
- [x] **2.3 Gun** · S · after 2.2
  - Done when holding Cross fires tracer rounds that damage the Units they hit, and the dummy can be shot down.
  - Verify: play. Files: `game/combat/Gun.cs`, `game/flight/jet.tscn`.
- [x] **2.4 Player damage and being shot down** · S · after 2.2, 1.22
  - Done when the Player's jet has health and, at zero or on hitting terrain, is shot down: explosion, then respawn after a pause.
  - Verify: play. Files: `game/flight/Jet.cs`, `game/combat/Unit.cs`, `game/flight/ChaseCamera.cs`, `game/flight/JetSound.cs`, `game/ui/DebugOverlay.cs`.
- [x] **2.5 Targeting** · M · after 2.2
  - Done when the nearest hostile Unit in front becomes the current Target (hostile means another Team until Alliances arrive in 3.4), Triangle cycles Targets, and a HUD box marks the Target with its distance.
  - Verify: play. Files: `game/combat/Targeting.cs`, `game/ui/hud.tscn`, `game/ui/Hud.cs`, `game/ui/TargetMarker.cs`, `game/combat/Unit.cs`, `game/ui/DebugOverlay.cs`, `game/flight/jet.tscn`, `game/world/test_flight.tscn`.
- [x] **2.6 Lock-on** · S · after 2.5
  - Done when a Target inside the seeker cone and range locks after a short delay, its marker turns red, and a lock tone plays.
  - Verify: play. Files: `game/combat/Targeting.cs`, `game/ui/TargetMarker.cs`.
- [x] **2.7 Standard missile** · M · after 2.6
  - Done when Circle fires a missile that homes on a locked Target with a limited turn rate (so tight turns can dodge it), explodes by proximity and deals damage, and flies straight without a lock. Its stats come from the base Pack.
  - Verify: test (weapon stats load), play. Files: `core/WeaponType.cs`, `core/Pack.cs`, `core/PackLoader.cs`, `packs/base/weapons/standard-missile.json`, `game/combat/Missile.cs`, `game/combat/missile.tscn`, `game/combat/MissileLauncher.cs`, `game/combat/Targeting.cs`, `game/combat/Unit.cs`, `game/content/Packs.cs`, `game/flight/jet.tscn`.
- [x] **2.8 Missile Hardpoints and count** · S · after 2.7
  - Done when two missiles are ready at a time, each reloads after a delay, and the count starts at 144 and runs down.
  - Verify: play. Files: `game/combat/MissileLauncher.cs`, `game/flight/jet.tscn`, `game/ui/DebugOverlay.cs`.
  - Played 2026-10-08 at the armed checkpoint: passed, the wingtip missiles' look included. The reload goes from 3 s to 5 s in 2.32.
- [x] **2.9 HUD** · M · after 2.8
  - Done when a green Ace Combat-style HUD shows speed, altitude, damage %, the selected weapon and its count.
  - Verify: play. Files: `game/ui/hud.tscn`, `game/ui/Hud.cs`, `game/ui/SpeedAltitude.cs`, `game/ui/WeaponDisplay.cs`, `game/ui/TargetMarker.cs`, `game/combat/MissileLauncher.cs`, `game/combat/Unit.cs`, `game/flight/Controls.cs`, `game/ui/DebugOverlay.cs`, `game/ui/debug_overlay.tscn`.
  - Played 2026-10-08 at the armed checkpoint: passed.
- [x] **2.31 Stall warning** · XS · after 2.9, 1.13
  - Done when the HUD shows STALL in its green while the jet is below stall speed (steady, the user's pick over flashing), and a voice says "Stall warning" when a stall starts and every 3 s while it lasts, as in Ace Combat. The voice is Windows' Zira, through Godot's text-to-speech, until 6.26 brings recordings.
  - Verify: play. Files: `game/ui/StallWarning.cs`, `game/ui/hud.tscn`, `game/project.godot`.
  - Played 2026-10-08 at the armed checkpoint: passed, with the game's own Zira voice and the voice finishing its phrase when paused.
- [x] **2.10 Radar minimap** · M · after 2.5
  - Done when a heading-up radar shows hostile Units in red and allied Units in blue around the Player.
  - Verify: play. Files: `game/ui/Radar.cs`, `game/ui/hud.tscn`, `game/ui/Hud.cs`, `game/ui/TargetMarker.cs`, `game/combat/Targeting.cs`.
  - Played 2026-10-08 at the armed checkpoint: passed.

- [x] **2.27 Look at the Target** · S · after 2.5, 1.17
  - Done when holding Triangle turns the camera to keep the current Target on screen and releasing it brings back the chase camera, while a tap still switches Targets.
  - Verify: play. Files: `game/flight/ChaseCamera.cs`, `game/combat/Targeting.cs`.
  - Played 2026-10-08 at the armed checkpoint: holding Triangle passed, and a tap switching when let go is fine. The order a tap switches in changes to on-the-nose in 2.34.
- [x] **2.28 Missile camera** · S · after 2.7, 1.17
  - Done when holding Circle while firing makes the camera follow the fired missile (or Special Weapon, once those exist) until Circle is released or about a second after it explodes, then brings back the chase camera.
  - Verify: play. Files: `game/flight/ChaseCamera.cs`, `game/combat/MissileLauncher.cs`, `game/combat/Missile.cs`.
  - Played 2026-10-08 at the armed checkpoint: passed, keeping the 80 m back-off from the fireball and the missile's smoke in the ride view as they are. Holding Circle with every Hardpoint empty follows the last missile in 2.33.
- [x] **2.29 Large radar map** · S · after 2.10
  - Done when each touchpad press cycles the radar through default, zoomed out (3x the range) and whole map (the box grows as far as it can without covering the SPEED box, north-up, showing the whole terrain), easing between them (the user's design).
  - Verify: play. Files: `game/ui/Radar.cs`, `game/world/TestTerrain.cs`, `game/ui/SpeedAltitude.cs`.
  - Played 2026-10-08 at the armed checkpoint: passed, with the whole map at 292 px.

**Checkpoint: armed.** The user shoots dummies with the gun and missiles, reads the HUD and the large radar map, and tries the Target and missile cameras.

- Flown 2026-10-08, together with tasks 2.8, 2.9, 2.31, 2.10, 2.27, 2.28 and 2.29, and the Step 1 and Step 2 clean-ups. Passed: the HUD, the radar and its three modes, lock-on, holding Triangle to watch the Target, firing, reloading and riding missiles, shooting dummies down, damage, and the stall warning and voice. Kept as they are: the Target zoom (1.25×), how quickly the view swings to the Target (1.5×), the radar's range (4 km, ×3 zoomed out) and the stall voice's repeat (3 s).
- Changed in `light-fighter.json`, from the user's tuning panel changes during the checkpoint: `cruiseReturnStrength` 60 → 25.8 (letting go of the triggers drifts back to cruise speed more slowly), `yawRate` 20 → 18, `turnSpeedLoss` 100 → 75.
- Changes asked for, in tasks 2.32 to 2.37, for a session of fixes before 2.11: a longer missile reload, the missile camera with every Hardpoint empty, on-the-nose Target switching, and a more forgiving gun (more rounds, near misses counting, and a gun reticle). 2.38 cleans up the numbers the tuning panel saved.

- [x] **2.32 Longer missile reload** · XS · after 2.8
  - Done when an empty Hardpoint loads its next missile 5 s after firing instead of 3 s (the user's pick, "for now").
  - Verify: play. Files: `game/combat/MissileLauncher.cs`.
  - Played 2026-10-08: tried at 6 s first, then the user turned it down to 5 s.
- [x] **2.33 Missile camera with every Hardpoint empty** · XS · after 2.28
  - Done when holding Circle while every Hardpoint is empty (all reloading, or the count spent) follows the last missile fired, if it's still flying, the same way holding Circle after firing does. Today that press fires nothing, so the camera follows nothing. A press that fires a missile still follows the new one.
  - Verify: play. Files: `game/combat/MissileLauncher.cs`, `game/flight/ChaseCamera.cs`.
  - Not played yet: committed 2026-10-09 while the user couldn't play. Play it: fire both missiles, then hold Circle (the view rides the second one after a moment); let go and hold Circle again just as it hits (the view stays on the jet, no flicker); hold Circle once both have exploded while still reloading (nothing happens); and with a missile loaded, holding Circle still fires and rides the new one. Also fixed: the camera no longer jumps for a second when the followed missile explodes before the view picks it up.
- [ ] **2.34 On-the-nose Target switching** · S · after 2.27
  - Done when a tap of Triangle switches first to the hostile Unit most on the jet's nose, judged by both its distance and how far it sits from the middle of the HUD, and each further tap moves to the next one out. The order follows the nose as the jet turns, so the Player switches by pointing at what they want (the user's design). Holding Triangle stays as it is.
  - To settle when planning: how distance and angle weigh against each other (the user's example: a Target in the middle of the HUD but 10 km away, against one a little off the middle at 3 km); how quick taps go through an order that keeps changing without repeating or skipping Targets; and whether the automatic first Target (today the nearest hostile in front) uses the same order.
  - Verify: play. Files: `game/combat/Targeting.cs`.
- [ ] **2.35 Gun fire rate** · S · after 2.3
  - Done when the gun fires 80 rounds a second instead of 20 (the user's pick), a dense, easily followed stream of tracers down range, more like Project Wingman's minigun than Ace Combat's machine gun (the user's design).
  - Verify: play. Files: `game/combat/Gun.cs`.
- [ ] **2.36 Forgiving gun hits** · XS · after 2.3
  - Done when a round passing within about 1 m of a Unit's hit box counts as a hit, so the stream needn't land exactly on it.
  - Verify: play. Files: `game/combat/Gun.cs`.
- [ ] **2.37 Gun reticle** · M · after 2.35, 2.9
  - Done when a small reticle shows on the HUD while a hostile Unit is in gun range, marking where the rounds will be at its distance. It stays still when flying straight at the Unit, and drops down the HUD in a pull-up, furthest in a high-G turn, since rounds fired mid-turn end up below where the nose now points (the user's design).
  - Verify: play. Files: `game/ui/GunReticle.cs`, `game/ui/hud.tscn`, `game/combat/Gun.cs`.
- [ ] **2.38 Clean numbers from the tuning panel** · XS · after 1.25
  - Done when Save writes the values the sliders were set to without rounding leftovers (25.8, not 25.800001; 0.245, not 0.24499999), and the number beside each slider shows the value Save will write. Sliders move in steps, and the leftovers of the step's rounding went into `light-fighter.json` at the armed checkpoint. The number beside each slider shows two decimals, so the 0.24499999 saved for the response time showed as 0.24.
  - Verify: play (save, then read the file). Files: `game/ui/TuningPanel.cs`.

- [ ] **2.11 AI pilot flies to a point** · M · after 2.4
  - Done when the flight model takes its inputs from either the controller or an AI pilot, and an enemy jet (same Unit Type, Team 2) flies to a given point with it.
  - Verify: play. Files: `game/flight/FlightInput.cs`, `game/flight/Jet.cs`, `game/ai/AiPilot.cs`.
  - Note (from the Step 2 review): `jet.tscn` still assumes it's the Player's. An enemy jet built from it would fire its gun and missiles and switch Targets on the Player's buttons (`Gun`, `MissileLauncher` and `Targeting` read the controller directly), play its engine and wind at full volume wherever it is (`JetSound` isn't placed in 3D space), start again where it began 2 s after being shot down (`Jet.Destroyed`), and be copied by the debug overlay as a test dummy (every Unit on another Team at startup).
- [ ] **2.12 AI terrain avoidance** · S · after 2.11, 1.21
  - Done when AI jets pull up before terrain ahead and don't crash in normal flight; watch one fly the canyon area for a few minutes.
  - Verify: play. Files: `game/ai/AiPilot.cs`.
- [ ] **2.13 AI pursuit** · M · after 2.12
  - Done when an enemy jet chases its Target using lead pursuit and manages its speed.
  - Verify: play. Files: `game/ai/AiPilot.cs`, `game/ai/Pursuit.cs`.
- [ ] **2.14 AI attacks** · S · after 2.13, 2.7, 2.3
  - Done when AI jets fire the gun and missiles when their Target is in range and in front.
  - Verify: play. Files: `game/ai/AiPilot.cs`.
  - Note: jets get a hit box here, the first time anything shoots at them. Gun rounds must then skip the shooter's own hit box and Team (the spec has no friendly fire).
- [ ] **2.15 Missile warning** · S · after 2.14
  - Done when a "MISSILE" warning and alarm tone appear while a missile tracks the Player.
  - Verify: play. Files: `game/ui/Hud.cs`, `game/combat/Missile.cs`.
  - Note (from the Step 2 review): the alarm tone is the third sound made in code, after `JetSound`'s and the lock tone in `TargetMarker`, which pack their samples into a looping WAV the same way. Share that code here.
- [ ] **2.16 AI defense** · S · after 2.14
  - Done when AI jets break hard when a missile closes on them or a hostile sits on their tail.
  - Verify: play. Files: `game/ai/AiPilot.cs`.
- [ ] **2.30 Flares** · S · after 2.15, 2.9
  - Done when L3+R3 releases flares that can pull missiles tracking the Player off course, and the HUD shows how many flares are left.
  - Verify: play. Files: `game/combat/Flares.cs`, `game/combat/Missile.cs`, `game/ui/Hud.cs`.
  - Note: the user's layout for the HUD's bottom-right block (`game/ui/WeaponDisplay.cs`, 2.9): a FLR row with the flares left, between the weapons and DMG, and little filled slots under the jet's silhouette that show each flare and refill as it recharges.

**Checkpoint: 1v1.** The user dogfights one enemy jet and judges whether the AI is fun: neither helpless nor perfect.

- To tune here, against a moving jet: missile tracking. Since 2.7, missiles fly straight at where the Target is (pure pursuit, `game/combat/Missile.cs`) rather than leading it. One `seekerCone` (50°, the user's pick) sets both how wide lock-on is and how far off its nose a missile in flight can still see its Target; these may need to be split. The turn rate (50°/s) decides how easily a break turn dodges.

- [ ] **2.17 Special Weapon slots** · M · after 2.8, 2.9
  - Done when Square cycles standard missile → Special Weapon 1 → Special Weapon 2, Special Weapons are defined in the base Pack, and the HUD shows the selected one and its count.
  - Verify: test (definitions load), play. Files: `core/WeaponType.cs`, `packs/base/weapons/`, `game/combat/WeaponSlots.cs`, `game/ui/Hud.cs`.
  - Note: Special Weapons reload at their own rates, so the reload time moves into the Weapon Type. It has been a constant in `MissileLauncher.cs` since 2.8.
  - Note: in the HUD's bottom-right block (`game/ui/WeaponDisplay.cs`, 2.9), each Special Weapon gets a row with its count under MSL, and ">" moves to the selected weapon (the user's layout).
- [ ] **2.18 Special: agile short-range missile** · S · after 2.17
  - Done when a missile with a much higher turn rate and shorter range works as a Special Weapon.
  - Verify: play. Files: `packs/base/weapons/agile-missile.json`, `game/combat/Missile.cs`.
- [ ] **2.19 Special: long-range missile** · S · after 2.17
  - Done when a missile with long lock range and lower agility works as a Special Weapon.
  - Verify: play. Files: `packs/base/weapons/long-range-missile.json`, `game/combat/Targeting.cs`.
- [ ] **2.20 Tank** · S · after 2.2
  - Done when a gray-box tank Unit Type (armor Role) sits in the test scene and can be destroyed.
  - Verify: play. Files: `packs/base/unit-types/tank.json`, `game/combat/tank.tscn`.
- [ ] **2.21 Special: air-to-ground weapon** · S · after 2.17, 2.20
  - Done when an air-to-ground missile or bombs (pick one; the spec allows either) can destroy the tank.
  - Verify: play. Files: `packs/base/weapons/`, `game/combat/`.
- [ ] **2.22 AA gun** · M · after 2.20
  - Done when an AA gun Unit Type (anti-air Role) tracks the Player within range and fires bursts that can damage it.
  - Verify: play. Files: `packs/base/unit-types/aa-gun.json`, `game/combat/aa_gun.tscn`, `game/combat/AaGun.cs`.
- [ ] **2.23 Score** · XS · after 2.2
  - Done when destroying a Unit adds its points and the HUD shows the score.
  - Verify: play. Files: `game/ui/Hud.cs`.
- [ ] **2.24 Combat sounds** · S · after 2.15
  - Done when placeholder CC0 sounds play for the gun, missile launch, hits, explosions and the missile alarm, and the credits are updated.
  - Verify: play. Files: `packs/base/sounds/`, `packs/base/CREDITS.md`, `game/combat/`.
- [ ] **2.25 Hit and explosion effects** · S · after 2.7
  - Done when missiles leave smoke trails, hits flash, and explosions throw debris and smoke.
  - Verify: play. Files: `game/combat/explosion.tscn`, `game/combat/missile.tscn`.
- [ ] **2.26 Dogfight test scene** · XS · after 2.16, 2.22
  - Done when the test scene has four enemy jets, an AA gun and two tanks. It's a throwaway dev scene, not a Mission, so [ADR 0002](../docs/adr/0002-every-mission-built-in-the-editor.md) still holds; task 3.5 replaces it.
  - Verify: play. Files: `game/world/test_flight.tscn`.

**Checkpoint: Milestone 1, is it fun?** The user fights four enemy jets and says it's fun. Re-estimate the remaining steps in [plan.md](plan.md).

## Step 3: Missions

- [ ] **3.1 Mission model and file** · M · after 2.26
  - Done when Core has the Mission model (Map id, required Packs, Teams and Alliances, Groups with their Units and positions, the Player's start and Team) and reads and writes it as JSON, round-trip tested.
- [ ] **3.2 Mission file versioning** · S · after 3.1
  - Done when Mission files carry a format version and older versions upgrade on load, tested with a sample old file.
- [ ] **3.3 Mission validation** · S · after 3.1
  - Done when loading reports unknown Unit Types and missing Packs by name.
- [ ] **3.4 Alliances** · S · after 3.1
  - Done when Core answers whether two Teams are hostile from the Mission's Alliances (tested: 1 vs 2, and 1+2 vs 3), and the game's targeting uses it.
- [ ] **3.5 Game runs a Mission file** · M · after 3.4
  - Done when the game loads a hand-written Mission from `missions/` and spawns its Groups and the Player, replacing the dogfight test scene.
- [ ] **3.6 Routes and Zones** · S · after 3.1
  - Done when Core models named Routes (points with altitudes) and Zones (areas with an optional altitude band), with "is this point in the Zone" tested, including above the band.
- [ ] **3.7 Orders model** · S · after 3.6
  - Done when Core models Orders with their parameters and a hold-fire switch, each Group has a current Order, and ordering a single Unit splits it into its own Group.
- [ ] **3.8 Order: Fly Route** · S · after 3.7, 3.5
  - Done when an AI Group flies its Route once or looping.
- [ ] **3.9 Order: Patrol Zone** · S · after 3.8
  - Done when an AI Group circles a Zone and intercepts hostile intruders.
- [ ] **3.10 Order: Attack** · S · after 3.8
  - Done when an AI Group attacks a given Group or Target.
- [ ] **3.11 Order: Escort** · S · after 3.8
  - Done when an AI Group escorts a Group or the Player and engages threats to it.
- [ ] **3.12 Order: Engage at will** · XS · after 3.8
  - Done when an AI Group hunts any hostile it can find.
- [ ] **3.13 Order: Retreat** · S · after 3.8
  - Done when an AI Group flies to a Zone, or off the Map edge, and is removed.
- [ ] **3.14 Hold fire** · XS · after 3.10
  - Done when Groups set to hold fire never shoot until it's switched off.
- [ ] **3.15 Spotting** · M · after 3.5
  - Done when Units spot hostiles within their Unit Type's spotting range and each Team knows what it has spotted.
- [ ] **3.16 Rules engine** · M · after 3.1
  - Done when Core runs Triggers (event, conditions, actions; once or repeat; Phase or Mission scope; same-moment Triggers in list order) against a fake world in tests.
- [ ] **3.17 Events: Mission and Phase start and end, time passing** · S · after 3.16
  - Done when these events fire Triggers, including "every N seconds".
- [ ] **3.18 Events: destroyed and damaged** · S · after 3.16
  - Done when a Unit or Group being destroyed, or a Unit being damaged, fires Triggers.
- [ ] **3.19 Events: Zone entry and exit, Route finished** · S · after 3.16, 3.6
  - Done when entering or leaving a Zone, and a Group finishing its Route, fire Triggers.
- [ ] **3.20 Event: spotted by an enemy** · XS · after 3.15, 3.16
  - Done when a Unit being spotted by a hostile Team fires Triggers.
- [ ] **3.21 Variables, comparisons and random chance** · S · after 3.16
  - Done when Missions declare Variables and conditions compare them or roll a random chance.
- [ ] **3.22 Conditions: counts and Unit state** · S · after 3.16
  - Done when conditions count living Units in a Group, Team or Zone, and check a Unit's health, altitude, speed, whether it's in a Zone, and the current Phase.
- [ ] **3.23 Actions: set Variable, wait** · S · after 3.21
  - Done when actions set or add to a Variable and wait N seconds before the next action.
- [ ] **3.24 Actions: bring in, remove, give Order** · S · after 3.16, 3.7
  - Done when actions bring in hidden Groups, remove Groups, and give Orders to Groups or single Units.
- [ ] **3.25 Actions: rearm the Player, win, lose** · XS · after 3.16
  - Done when these three actions work in a running Mission.
- [ ] **3.26 Radio messages** · S · after 3.16
  - Done when a radio action shows the speaker and subtitle text on screen, queued one after another.
- [ ] **3.27 Objectives: Destroy and Protect** · M · after 3.16
  - Done when Core tracks required and optional Destroy and Protect Objectives, checks the world when they activate (an already-destroyed convoy completes at once), and raises Objective events.
- [ ] **3.28 Objectives: Reach, Survive, Score** · S · after 3.27
  - Done when these three kinds complete and fail as the spec says.
- [ ] **3.29 Objectives: Destruction and Custom** · S · after 3.27
  - Done when Destruction completes at X% of the Targets in a Zone, and Custom changes only through actions.
- [ ] **3.30 Objectives: hidden, time limits, reveal/complete/fail actions** · S · after 3.27
  - Done when Objectives can start hidden, a time limit fails them, and actions reveal, complete or fail them.
- [ ] **3.31 Phases** · M · after 3.27
  - Done when Phases run in a line, each ending on its Objectives, its time limit (moving on) or an "end Phase" action, and Objectives and Triggers belong to a Phase or the whole Mission.
- [ ] **3.32 Phase Groups** · S · after 3.31, 3.24
  - Done when Groups placed in a Phase appear when it starts, and Groups set to leave when the Phase ends retreat then.
- [ ] **3.33 Mission end** · S · after 3.31
  - Done when the Mission is won when the last Phase ends and lost when a required Objective fails or the Player is shot down, with an on-screen banner.
- [ ] **3.34 Objective HUD** · S · after 3.27
  - Done when an Ace Combat-style Objective list shows on screen, things to destroy carry OBJ markers, and things to protect carry their own marker.
- [ ] **3.35 Map edge** · S · after 3.33
  - Done when leaving the Map shows a "return to the operational area" countdown that fails the Mission if ignored.
- [ ] **3.36 Score and Rank** · S · after 3.1
  - Done when Core scores destroyed Units (Unit Type points, Group overrides) and gives S/A/B/C from score and time, with automatic or Author-set thresholds.
- [ ] **3.37 Results screen** · S · after 3.33, 3.36
  - Done when the end of a Mission shows score, time, Rank and the Objectives' outcomes.
- [ ] **3.38 Ruleset** · S · after 3.1, 2.17
  - Done when the Mission's Ruleset (allowed aircraft or enforced jet, missiles, gun, Special Weapon count; defaults 144, on, 2) applies at Mission start.
- [ ] **3.39 Briefing screen** · S · after 3.5
  - Done when the Mission's briefing text shows before take-off.
- [ ] **3.40 Hangar screen** · M · after 3.38, 3.39
  - Done when the Player picks a jet and Special Weapons within the Ruleset before take-off, and bombers never appear.
- [ ] **3.41 Trait: Invulnerable** · XS · after 3.5
  - Done when Invulnerable Units take no damage.
- [ ] **3.42 Trait: Stealth** · XS · after 3.15
  - Done when Stealth Units are spotted at a much shorter range.
- [ ] **3.43 Trait: Ace** · S · after 3.5
  - Done when Ace Units fly and shoot better and show their name on the HUD.
- [ ] **3.44 Actions: set Stance, add or remove Trait** · XS · after 3.16, 3.41
  - Done when actions change a Group's Stance and add or remove Traits.
- [ ] **3.45 Stances: Aggressive and Covering** · S · after 3.9, 3.11
  - Done when Aggressive Groups chase further and press attacks, and Covering Groups stay near allies and go after whatever threatens them.
- [ ] **3.46 Stance: Baiting** · S · after 3.45, 2.22
  - Done when Baiting Groups, once chased, run for their side's air defenses.
- [ ] **3.47 Scenery pieces** · M · after 3.5
  - Done when the test map has a bridge and some buildings as separate Scenery pieces with ids, indestructible until owned.
- [ ] **3.48 Scenery Targets** · S · after 3.47, 3.18
  - Done when an action gives a Scenery piece to a Team or takes it back, making it a Target or not, and destroying it fires the destroyed event.
- [ ] **3.49 Trucks: Drive Route, Hold position** · M · after 3.8
  - Done when a gray-box truck Unit Type drives its Route on the ground or holds position, and a convoy works as one Group. Adds the truck's Role (proposed: transport; confirm with the user).
- [ ] **3.50 Ground Orders: Take Zone, Attack, Retreat** · S · after 3.49
  - Done when ground Groups carry out these three Orders.
- [ ] **3.51 Bomber and Strike Zone** · M · after 3.8
  - Done when an AI-only bomber Unit Type bombs the Targets in a Zone.
- [ ] **3.52 Frigate** · M · after 3.8
  - Done when a frigate Unit Type with AA guns sails a Route, holds position or retreats. Adds the frigate's Role (proposed: ship; confirm with the user).
- [ ] **3.53 Mission state save and load** · S · after 3.31
  - Done when Core saves and restores a running Mission's state (Phase, Variables, Objectives, fired Triggers), round-trip tested. Groundwork for Checkpoints.
- [ ] **3.54 Sample two-Phase Mission** · S · after 3.33, 3.32
  - Done when a hand-written Mission in `missions/` (air superiority, then a ground strike) can be won and lost.

**Checkpoint: Step 3 done.** The user wins and loses the sample Mission.

## Step 4: Editor

- [ ] **4.1 Edit mode with a top-down camera** · S · after 3.54
  - Done when the game has an Edit mode that shows the Map from above, with mouse pan and zoom.
- [ ] **4.2 Free camera** · S · after 4.1
  - Done when a key switches to a free-flying camera for close-up work and back.
- [ ] **4.3 Place Units** · M · after 4.1
  - Done when a palette lists the Unit Types from the loaded Packs and a click places a Group of one on the terrain.
- [ ] **4.4 Altitude** · S · after 4.3
  - Done when placed aircraft can be raised and lowered, with a line down to the ground showing their position.
- [ ] **4.5 Select, move, rotate, delete** · M · after 4.3
  - Done when placed things can be selected (several at once), moved, rotated and deleted.
- [ ] **4.6 Undo and redo** · M · after 4.5
  - Done when every editor change can be undone and redone.
- [ ] **4.7 Groups and Teams** · S · after 4.5
  - Done when Units can be put into named Groups and each Group's Team set.
- [ ] **4.8 Alliances panel** · S · after 4.7
  - Done when the Author groups Teams into Alliances.
- [ ] **4.9 New, save, open** · S · after 4.7
  - Done when Missions save as the step 3 JSON in the user Missions folder, open again, and New asks for a Map.
- [ ] **4.10 Draw Routes** · M · after 4.4
  - Done when the Author clicks out a named Route with an altitude per point.
- [ ] **4.11 Draw Zones** · S · after 4.1
  - Done when the Author draws a named Zone with an optional altitude band.
- [ ] **4.12 Orders panel** · M · after 4.10, 4.11
  - Done when a Group's Order and its parameters (Route, Zone, target Group, hold fire) can be set.
- [ ] **4.13 Stance and Traits panel** · XS · after 4.12
  - Done when a Group's Stance and Traits can be set.
- [ ] **4.14 Variables panel** · S · after 4.9
  - Done when Variables can be added, named, given a start value and removed.
- [ ] **4.15 Objectives panel** · M · after 4.11
  - Done when Objectives can be added and edited: kind, what they refer to, required or optional, hidden, time limit.
- [ ] **4.16 Trigger list** · S · after 4.9
  - Done when Triggers can be created, deleted and reordered, set to once or repeat, and scoped to a Phase or the Mission.
- [ ] **4.17 Trigger events picker** · S · after 4.16
  - Done when a Trigger's event and its parameters are chosen from dropdowns.
- [ ] **4.18 Trigger conditions editor** · S · after 4.17
  - Done when conditions are added, edited and removed from dropdowns.
- [ ] **4.19 Trigger actions editor** · M · after 4.17
  - Done when actions, including waits, are added, edited, removed and reordered from dropdowns.
- [ ] **4.20 Phase timeline** · M · after 4.15, 4.16
  - Done when Phases can be added, renamed and reordered on a strip, and selecting one shows its Groups, Objectives and Triggers.
- [ ] **4.21 Phase end settings** · XS · after 4.20
  - Done when each Phase's end (Objectives, time limit, Trigger) can be set.
- [ ] **4.22 Timed arrivals** · S · after 4.20
  - Done when a Group can be set to arrive at a moment inside a Phase, shown as a marker on the strip.
- [ ] **4.23 Scenery Targets in the editor** · S · after 4.5, 3.48
  - Done when the Author picks Scenery pieces and gives them to a Team.
- [ ] **4.24 Mission settings panel** · S · after 4.9
  - Done when the Ruleset, the Player's start and Team, and the briefing text can be set.
- [ ] **4.25 Play from here** · M · after 4.20
  - Done when Play starts the Mission at the selected Phase from the camera's position, and quitting returns to the editor where it was.
- [ ] **4.26 Simulate** · M · after 4.25
  - Done when Simulate runs the AI without the Player at up to 8x speed, and stopping resets everything.
- [ ] **4.27 Broken reference warnings** · S · after 4.19
  - Done when Play and Save warn about anything that refers to a deleted Group, Zone, Route or Variable.

**Checkpoint: Milestone 2, the sandbox loop works.** The user rebuilds task 3.54's Mission in the editor without touching a file.

## Step 5: Packs

- [ ] **5.1 User Packs folder** · S · after 4.27
  - Done when Packs load from both the base folder and the user Packs folder, with `pack.json` validated.
- [ ] **5.2 Load order and overrides** · S · after 5.1
  - Done when later Packs override earlier ones by id.
- [ ] **5.3 Packs screen** · S · after 5.2
  - Done when the user can list, enable, disable and reorder Packs.
- [ ] **5.4 glTF models from Packs** · M · after 5.1
  - Done when a Unit Type with a model shows it, loaded at runtime, instead of a gray box.
  - Note: the gray-box jet's Hardpoints are nodes on its wingtips in `jet.tscn` (2.8, for now). With models, each Unit Type places its own Hardpoints where its model carries missiles (the user's plan).
- [ ] **5.5 Images and sounds from Packs** · S · after 5.1
  - Done when Unit Types and weapons use images and sounds from their Pack.
- [ ] **5.6 Catalog interfaces** · M · after 5.1
  - Done when Events, Conditions, Actions and Traits register through Core interfaces, and the base game registers its own entries the same way.
- [ ] **5.7 Pack code** · M · after 5.6
  - Done when a Pack's C# library loads and registers catalog entries, and the game asks before enabling it.
- [ ] **5.8 Missing Packs message** · XS · after 5.1
  - Done when opening a Mission names any Packs it needs that aren't installed.
- [ ] **5.9 Windows build** · S · after 5.1
  - Done when an exported Windows build runs with the base Pack beside the .exe, and Core is compiled with optimizations (Godot's ExportRelease configuration leaves it unoptimized unless `core/Core.csproj` turns them on).
- [ ] **5.10 Sample second Pack** · S · after 5.7, 5.4
  - Done when a test Pack adds a jet and a Trait without changing the game.

## Step 6: Depth

- [ ] **6.1 Floating origin** · M · after 5.10
  - Done when the world recenters around the Player so a 64 km Map stays precise at its edges.
- [ ] **6.2 Elevation data to heightmap** · M · after 6.1
  - Done when a script turns public elevation data for a region the user picks into a 16-bit heightmap for a 64 km Map.
- [ ] **6.3 Terrain technology spike** · S · after 6.2
  - Done when the Terrain3D plugin and a custom heightmap mesh have both been tried with the heightmap, and the choice is recorded as an ADR.
- [ ] **6.4 64 km terrain** · M · after 6.3
  - Done when the Map's heightmap renders with level of detail and collision.
- [ ] **6.5 Terrain colors** · S · after 6.4
  - Done when ground color follows slope and height (grass, rock, snow, sand).
- [ ] **6.6 Map file** · S · after 6.4
  - Done when a Map in a Pack is a heightmap plus a Scenery list, loaded by Core.
- [ ] **6.7 City blocks** · M · after 6.6
  - Done when the Map has a generated city of separate Scenery pieces.
- [ ] **6.8 Airbase, bridge, refinery** · M · after 6.6
  - Done when these landmarks sit on the Map as Scenery pieces.
- [ ] **6.9 Multirole jet** · S · after 5.10
  - Done when a flyable multirole Unit Type exists with its own Handling.
- [ ] **6.10 Attacker jet** · S · after 5.10
  - Done when a flyable attacker Unit Type exists with its own Handling.
- [ ] **6.11 SAM site** · M · after 5.10
  - Done when a SAM site Unit Type (anti-air Role) launches missiles at aircraft in range.
- [ ] **6.12 Radar** · S · after 5.10
  - Done when a radar Unit Type extends its Team's spotting range. Adds its Role (proposed: structure, shared with 6.13; confirm with the user).
- [ ] **6.13 Bunker** · XS · after 5.10
  - Done when a hardened bunker Unit Type exists for ground strikes. Adds its Role (proposed: structure, shared with 6.12; confirm with the user).
- [ ] **6.14 Command menu** · M · after 5.10
  - Done when a D-pad radial menu offers the quick selections, limited to Units under the Player's command.
- [ ] **6.15 Commands** · M · after 6.14
  - Done when Attack my target, Cover me, Engage at will, Resume orders and Retreat override Group Orders as the spec says.
- [ ] **6.16 Type-specific Commands** · S · after 6.15
  - Done when Bombing run (bombers and attackers) and Take Zone (ground units) work.
- [ ] **6.17 Command actions** · XS · after 6.15
  - Done when Triggers give and take away the Player's command of Units.
- [ ] **6.18 Time of day** · S · after 5.10
  - Done when sun and sky follow the Mission's time of day.
- [ ] **6.19 Clouds and fog** · M · after 6.18
  - Done when Missions can set cloud cover and fog.
- [ ] **6.20 Rain** · S · after 6.19
  - Done when Missions can set rain.
- [ ] **6.21 Weather actions** · XS · after 6.20
  - Done when a Trigger action changes weather or time of day mid-Mission.
- [ ] **6.22 Music** · S · after 5.5
  - Done when Packs carry tracks, each Phase can set one, and the "change music" action switches tracks.
- [ ] **6.23 Checkpoints** · M · after 3.53
  - Done when each Phase start is saved and a Player who is shot down can retry from there.
- [ ] **6.24 Low-poly models** · M · after 5.4
  - Done when the base Unit Types use free CC0 or CC-BY models, credited in `packs/base/CREDITS.md`.
- [ ] **6.25 Cockpit view** · M · after 6.24, 1.28
  - Done when R3's views include a cockpit view with a simple cockpit model.
- [ ] **6.26 Real sounds** · M · after 5.5, 2.24, 6.22
  - Done when the generated engine and wind sounds and the placeholder combat sounds are replaced with free CC0 or CC-BY recordings, credited in `packs/base/CREDITS.md`, with their volumes balanced against each other and the music.
  - Note: the "Stall warning" voice (2.31) is Windows' text-to-speech for now, which plays outside the game's own audio mixing. A recording replaces it here.
