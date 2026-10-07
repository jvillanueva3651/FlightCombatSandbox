using System.Collections.Generic;
using Godot;

/// <summary>
/// The game's input actions and their bindings: Ace Combat 7's PlayStation layout, with a
/// keyboard fallback. An autoload, so the actions exist before any scene asks for them.
/// Axes come in pairs: read them with Input.GetAxis(negative, positive).
/// </summary>
public partial class Controls : Node
{
    public static readonly StringName
        PitchDown = "pitch_down", PitchUp = "pitch_up",
        RollLeft = "roll_left", RollRight = "roll_right",
        YawLeft = "yaw_left", YawRight = "yaw_right",
        Throttle = "throttle", Brake = "brake",
        LookLeft = "look_left", LookRight = "look_right", LookDown = "look_down", LookUp = "look_up",
        FireGun = "fire_gun", FireMissile = "fire_missile",
        SwitchWeapon = "switch_weapon", SwitchTarget = "switch_target",
        ChangeView = "change_view", Flare = "flare", RadarMap = "radar_map", Pause = "pause",
        TuningPanel = "tuning_panel", DamageDummy = "damage_dummy", DamageJet = "damage_jet";

    // L3 on its own does nothing; it only counts as half of the flare chord.
    static readonly StringName LeftStickClick = "left_stick_click";

    static readonly List<StringName> Bound = new();  // every action bound here, all with the Feel Settings' dead zone

    public override void _EnterTree()
    {
        // Pushing the stick forward dives, like a plane's control stick.
        Bind(PitchDown, PadAxis(JoyAxis.LeftY, -1), Keyboard(Key.W));
        Bind(PitchUp, PadAxis(JoyAxis.LeftY, 1), Keyboard(Key.S));
        Bind(RollLeft, PadAxis(JoyAxis.LeftX, -1), Keyboard(Key.A));
        Bind(RollRight, PadAxis(JoyAxis.LeftX, 1), Keyboard(Key.D));
        Bind(YawLeft, PadButton(JoyButton.LeftShoulder), Keyboard(Key.Q));    // L1
        Bind(YawRight, PadButton(JoyButton.RightShoulder), Keyboard(Key.E));  // R1
        Bind(Throttle, PadAxis(JoyAxis.TriggerRight, 1), Keyboard(Key.Shift)); // R2
        Bind(Brake, PadAxis(JoyAxis.TriggerLeft, 1), Keyboard(Key.Ctrl));      // L2
        Bind(LookLeft, PadAxis(JoyAxis.RightX, -1), Keyboard(Key.Left));
        Bind(LookRight, PadAxis(JoyAxis.RightX, 1), Keyboard(Key.Right));
        // Pushing the right stick forward looks down, like tilting your head.
        Bind(LookDown, PadAxis(JoyAxis.RightY, -1), Keyboard(Key.Up));
        Bind(LookUp, PadAxis(JoyAxis.RightY, 1), Keyboard(Key.Down));

        // Godot names face buttons by Xbox position: A = Cross, B = Circle, X = Square, Y = Triangle.
        Bind(FireGun, PadButton(JoyButton.A), Keyboard(Key.Space));
        Bind(FireMissile, PadButton(JoyButton.B), Keyboard(Key.F));
        Bind(SwitchWeapon, PadButton(JoyButton.X), Keyboard(Key.R));
        Bind(SwitchTarget, PadButton(JoyButton.Y), Keyboard(Key.T));
        Bind(ChangeView, PadButton(JoyButton.RightStick), Keyboard(Key.V));   // R3
        Bind(Flare, Keyboard(Key.G));                                          // L3+R3: see IsFlarePressed
        Bind(LeftStickClick, PadButton(JoyButton.LeftStick));                  // L3
        Bind(RadarMap, PadButton(JoyButton.Touchpad), Keyboard(Key.M));
        Bind(Pause, PadButton(JoyButton.Start), Keyboard(Key.Escape));         // Options
        Bind(TuningPanel, Keyboard(Key.F1));                                   // developer tool, keyboard only
        Bind(DamageDummy, Keyboard(Key.F2));                                   // developer tool, keyboard only
        Bind(DamageJet, Keyboard(Key.F3));                                     // developer tool, keyboard only

        // Godot's built-in menu actions have no controller buttons for confirm and back.
        InputMap.ActionAddEvent("ui_accept", PadButton(JoyButton.A));          // Cross confirms
        InputMap.ActionAddEvent("ui_cancel", PadButton(JoyButton.B));          // Circle backs out
    }

    /// <summary>Flare is L3+R3 held together on the DualSense, a chord an input action can't express.</summary>
    public static bool IsFlarePressed() =>
        Input.IsActionPressed(Flare) || (Input.IsActionPressed(LeftStickClick) && Input.IsActionPressed(ChangeView));

    /// <summary>Changes the stick and trigger dead zone of every action, which binding set from the Feel Settings at startup.</summary>
    public static void SetDeadZone(float deadZone)
    {
        foreach (var action in Bound)
            InputMap.ActionSetDeadzone(action, deadZone);
    }

    // Stick and trigger travel inside the Feel Settings' dead zone is ignored.
    static void Bind(StringName action, params InputEvent[] events)
    {
        InputMap.AddAction(action, Packs.Base.Feel.DeadZone);
        foreach (var e in events)
            InputMap.ActionAddEvent(action, e);
        Bound.Add(action);
    }

    // Device -1 matches every keyboard and controller.
    static InputEvent Keyboard(Key key) => new InputEventKey { PhysicalKeycode = key, Device = -1 };
    static InputEvent PadButton(JoyButton button) => new InputEventJoypadButton { ButtonIndex = button, Device = -1 };
    static InputEvent PadAxis(JoyAxis axis, float direction) => new InputEventJoypadMotion { Axis = axis, AxisValue = direction, Device = -1 };
}
