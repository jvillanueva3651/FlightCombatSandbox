# Flight Combat Sandbox

A single-player arcade flight combat game in the spirit of Ace Combat. Players build their own Missions in an in-game editor and fly them.

## Language

### People

**Author**:
Someone building a Mission in the editor.
_Avoid_: Creator, Maker, Designer

**Player**:
Someone flying a Mission. An Author is usually also the Player of their own Missions, and can share them with other Players.
_Avoid_: User, Pilot

### Content

**Pack**:
A bundle of content the game loads: Unit Types, weapons, Maps and optionally code. The base game is itself the first Pack; anything players add is just another Pack.
_Avoid_: Mod, DLC, Add-on

**Map**:
The world a Mission takes place on: terrain plus Scenery. Every Map comes from a Pack.
_Avoid_: Level, World, Theater

**Scenery**:
Everything that is part of the Map rather than placed in a Mission: terrain, buildings, bridges, refineries. Never a Unit. It belongs to no Team and can't be damaged until a Mission gives a piece of it to a Team.
_Avoid_: Props, Environment

### Missions

**Mission**:
A playable scenario built in the editor, at any size, finished or not: a Map plus the Units, Objectives and logic placed on it.
_Avoid_: Operation, Level, Scenario

**Phase**:
One stage of a Mission, shown as a clip on the editor's timeline, with its own Groups, Objectives and Triggers. It ends when its Objectives are done, its time limit runs out, or a Trigger ends it, and the next Phase begins.
_Avoid_: Stage, Act, Segment

**Checkpoint**:
The start of a Phase, saved so that a Player who is shot down can retry from there instead of restarting the Mission.
_Avoid_: Save point

**Objective**:
A goal in a Mission: destroy, protect, reach, survive, score, destruction or custom. Required Objectives decide whether the Player wins or loses; optional ones earn bonus points. An Objective can stay hidden until a Trigger reveals it.
_Avoid_: Goal, Task, Primary objective, Secondary objective

**Rank**:
The S/A/B/C grade a Player earns at the end of a Mission, from score and time.
_Avoid_: Grade, Rating

**Playlist**:
An ordered chain of Missions played one after another.
_Avoid_: Campaign, Super World

### Forces

**Unit**:
A military craft or structure placed in a Mission (jet, ship, tank, AA gun, radar tower). It belongs to a Team, can be destroyed, and may engage other Units depending on its Unit Type.
_Avoid_: Entity, Actor, Object

**Unit Type**:
What kind of Unit something is: its model, health, weapons and what it can engage (an AA gun engages aircraft, a tank engages ground units). Unit Types come from Packs; a Mission only places Units of existing Unit Types.
_Avoid_: Class, Template, Prefab

**Group**:
A named set of Units from one Team that take Orders together. A Unit on its own is a Group of one.
_Avoid_: Squad, Squadron, Flight, Formation

**Team**:
A numbered side in a Mission. Every Unit belongs to exactly one Team.
_Avoid_: Faction, Side, Coalition

**Alliance**:
A set of Teams that fight on the same side in a Mission. Teams in different Alliances are hostile to each other.
_Avoid_: Game mode rules, Allegiance

**Target**:
Anything that can be locked on to and attacked: every Unit, and every piece of Scenery, that belongs to a hostile Team. Giving Scenery to a Team, or taking it back, changes what is a Target during a Mission.
_Avoid_: TGT (Ace Combat's marker for objective targets is a different idea)

### Flying

**Role**:
The job a Unit Type is built for: fighter, multirole, attacker or bomber for aircraft; armor or anti-air on the ground. New Roles arrive with the Unit Types that need them. Players only ever fly fighters, multiroles and attackers.
_Avoid_: Class, Category

**Handling**:
The tunable numbers that define how an aircraft flies (speeds, turn rates, response time and so on), stored in its Unit Type.
_Avoid_: Variables, Weights, Stats, Flight physics, Maneuverability

**Feel Settings**:
The tunable numbers shared by every aircraft that shape how flying feels: stick response, camera behavior, and multipliers on every aircraft's Handling. Unlike Handling, they belong to no single aircraft.
_Avoid_: Global Handling, Options, Sensitivity

**Weapon Type**:
What kind of weapon something is: its damage, how fast it flies and turns, and how its seeker locks on. Weapon Types come from Packs; the standard missile and every Special Weapon are Weapon Types.
_Avoid_: Weapon class, Weapon stats, Loadout

**Special Weapon**:
An extra weapon type a Player picks for a Mission, on top of standard missiles and the gun. Each aircraft offers several; the Ruleset says how many the Player takes.
_Avoid_: Secondary weapon, SP weapon

**Hardpoint**:
A place on an aircraft where a weapon hangs, ready to fire. After firing, it loads the next one a few seconds later, while any are left.
_Avoid_: Pylon, Rail, Station

**Ruleset**:
The Author's rules for how a Mission is flown: which aircraft the Player may choose (or one enforced jet), the missile count, whether the gun is available, and how many Special Weapons the Player takes. Defaults: any fighter, multirole or attacker; 144 missiles; gun; 2 Special Weapons.
_Avoid_: Game mode, Loadout rules

### Places

**Route**:
A named path of points, each with its own altitude, that a Group follows.
_Avoid_: Path, Flight plan

**Zone**:
A named area on the Map, optionally limited to an altitude band, that Orders, Triggers, Objectives and Commands can refer to. Flying above or below its band counts as leaving it.
_Avoid_: Area, Region

### Mission logic

**Order**:
What a Group is told to do: patrol a route, attack something, escort, retreat. The Units' AI carries it out.
_Avoid_: Task, Behavior

**Stance**:
How a Group fights while carrying out its Order: Balanced, Aggressive, Covering (protecting allies) or Baiting (luring chasers into its side's air defenses). A Group has exactly one Stance at a time.
_Avoid_: Behavior, Personality, Temperament, Mode

**Command**:
An Order the Player gives during a Mission to Units they select from those under their command. It overrides the Units' Group Order until it's done or the Player tells them to resume.
_Avoid_: Player order, Wingman command

**Trait**:
A lasting property of a Unit that the game itself has to implement because no Trigger can express it: stealth, invulnerability, ace-level skill.
_Avoid_: Characteristic, Attribute, Behavior

**Trigger**:
A Mission rule of the form: when something happens, if conditions hold, do actions.
_Avoid_: Script, Behavior

**Variable**:
A named value in a Mission, such as a flag or a counter, that Triggers read and change.
_Avoid_: Global value

### Editor

**Simulate**:
Running a Mission in the editor without the Player, sped up, to watch Orders play out. Stopping resets everything.
_Avoid_: Preview, Fast-forward, Dry run
