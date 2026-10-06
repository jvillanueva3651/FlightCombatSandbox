using Godot;

/// <summary>
/// Shows the jet's flight numbers and what every control is doing right now, from the
/// DualSense and the keyboard.
/// </summary>
public partial class DebugOverlay : CanvasLayer
{
    /// <summary>The jet whose speed, altitude, throttle and G-load are shown.</summary>
    [Export] public Jet Jet { get; set; }

    Label _label;

    public override void _Ready() => _label = GetNode<Label>("Label");

    public override void _Process(double delta)
    {
        _label.Text = $"""
            Speed          {Jet.Speed,5:0} km/h
            Altitude       {Jet.GlobalPosition.Y,5:0} m
            Throttle       {Signed(Jet.Throttle)}
            G-load         {Jet.GLoad,5:0.0} G

            Control        Value  DualSense              Keyboard
            Pitch          {Axis(Controls.PitchDown, Controls.PitchUp)}  Left stick up/down     W / S
            Roll           {Axis(Controls.RollLeft, Controls.RollRight)}  Left stick left/right  A / D
            Yaw            {Axis(Controls.YawLeft, Controls.YawRight)}  L1 / R1                Q / E
            Throttle       {Amount(Controls.Throttle)}  R2                     Shift
            Brake          {Amount(Controls.Brake)}  L2                     Ctrl
            Look sideways  {Axis(Controls.LookLeft, Controls.LookRight)}  Right stick            Left / Right
            Look up/down   {Axis(Controls.LookDown, Controls.LookUp)}  Right stick            Down / Up
            Gun            {Held(Controls.FireGun)}  Cross                  Space
            Missile        {Held(Controls.FireMissile)}  Circle                 F
            Switch weapon  {Held(Controls.SwitchWeapon)}  Square                 R
            Switch target  {Held(Controls.SwitchTarget)}  Triangle               T
            Change view    {Held(Controls.ChangeView)}  R3                     V
            Flare          {Held(Controls.IsFlarePressed())}  L3 + R3                G
            Radar map      {Held(Controls.RadarMap)}  Touchpad               M
            Pause          {Held(Controls.Pause)}  Options                Esc
            """;
    }

    static string Axis(StringName negative, StringName positive) => Signed(Input.GetAxis(negative, positive));

    static string Signed(float value) => value.ToString("+0.00;-0.00; 0.00");

    static string Amount(StringName action) => Input.GetActionStrength(action).ToString(" 0.00");

    static string Held(StringName action) => Held(Input.IsActionPressed(action));

    static string Held(bool held) => held ? "   ON" : "   --";
}
