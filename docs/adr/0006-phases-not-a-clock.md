# The Mission timeline is a sequence of Phases, not a clock

The editor's timeline shows a Mission as a strip of Phases, because a Mission isn't a video: the Player's speed changes when things happen. A Phase ends on its Objectives, a time limit or a Trigger, so clip lengths on the strip are only estimates. We deliberately don't support scrubbing backwards, because that needs a simulation that replays identically every run, which is costly and fragile. Instead, Simulate fast-forwards the AI from a Phase start, and Play-from-here starts a test at any Phase.
