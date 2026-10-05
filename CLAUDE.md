# Flight Combat Sandbox

Single-player arcade flight combat game (Ace Combat's rules, an original universe) with a Mario Maker-style in-game Mission editor, built in Godot 4 (.NET) and C#. The user and Claude build it together; the user is new to game engines, so explain Godot concepts in plain words the first time they come up.

## Docs

- `GLOSSARY.md`: the project's vocabulary. Read it before naming anything (classes, files, UI text, docs) and use its terms; each entry's _Avoid_ list names the words to replace.
- `docs/PLAN.md`: the confirmed design and the roadmap. Read it at the start of a task to find the current step; tick the task's checkbox when it's done.
- `docs/adr/`: one file per hard-to-reverse decision. Read the matching ADR before touching the engine setup, the Core/Game split, Packs, Mission files, the flight model, or anything copyright-related.

## Tools

- Godot 4.7.2 (.NET): `C:\Users\jvill\source\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe`. Use this `_console` build from the command line; `--headless --path game --build-solutions --quit` checks that the project opens and builds.

## How we work

- One task from the plan at a time; the user plays it before the next one starts.
- Flight and combat feel is done when the user has flown it and says it feels right.
- Each finished task gets its own commit.
- A term that settles goes into `GLOSSARY.md`; a hard-to-reverse decision gets a new ADR.
- Everything in this repository is original. The user's personal Ace Combat and Project Wingman music lives in a private Pack in their user Packs folder.
