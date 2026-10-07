using Godot;

/// <summary>
/// Shows the jet's flight numbers, its and the dummy's health and what every control is doing right
/// now, from the DualSense and the keyboard. F2 damages the dummy, and a destroyed dummy comes back,
/// as it was at the start, a few seconds later. F3 damages the jet.
/// </summary>
public partial class DebugOverlay : CanvasLayer
{
    /// <summary>The jet whose speed, altitude, throttle, G-load and health are shown, and that F3 damages.</summary>
    [Export] public Jet Jet { get; set; }

    /// <summary>The test target that F2 damages.</summary>
    [Export] public Unit Dummy { get; set; }

    const int DebugDamage = 25;          // damage per press of F2 or F3
    const float DummyRespawnDelay = 5;   // seconds from the dummy's destruction to a fresh one

    Label _label;
    Unit _freshDummy;  // a copy of the dummy as the scene placed it, kept out of the scene
    Node _dummyParent;
    float _sinceDummyDestroyed;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        _freshDummy = (Unit)Dummy.Duplicate();
        _dummyParent = Dummy.GetParent();
    }

    public override void _ExitTree() => _freshDummy.Free();  // never in the scene, so nothing else frees it

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed(Controls.DamageDummy) && IsInstanceValid(Dummy))
            Dummy.Damage(DebugDamage);
        else if (e.IsActionPressed(Controls.DamageJet))
            Jet.Damage(DebugDamage);
    }

    public override void _Process(double delta)
    {
        if (!IsInstanceValid(Dummy))
        {
            _sinceDummyDestroyed += (float)delta;
            if (_sinceDummyDestroyed >= DummyRespawnDelay)
            {
                _sinceDummyDestroyed = 0;
                Dummy = (Unit)_freshDummy.Duplicate();
                _dummyParent.AddChild(Dummy);
            }
        }

        var dummyHealth = IsInstanceValid(Dummy) ? $"{Dummy.Health,5}" : "destroyed";
        _label.Text = $"""
            Speed          {Jet.Speed,5:0} km/h
            Altitude       {Jet.GlobalPosition.Y,5:0} m
            Throttle       {Signed(Jet.Throttle)}
            G-load         {Jet.GLoad,5:0.0} G
            Jet health     {Jet.Health,5}
            Dummy health   {dummyHealth}

            Control        Value  DualSense              Keyboard
            Pitch          {Axis(Controls.PitchDown, Controls.PitchUp)}  Left stick up/down     W / S
            Roll           {Axis(Controls.RollLeft, Controls.RollRight)}  Left stick left/right  A / D
            Yaw            {Axis(Controls.YawLeft, Controls.YawRight)}  L1 / R1                Q / E
            Throttle       {Amount(Controls.Throttle)}  R2                     Shift
            Brake          {Amount(Controls.Brake)}  L2                     Ctrl
            Look sideways  {Axis(Controls.LookLeft, Controls.LookRight)}  Right stick            Left / Right
            Look up/down   {Axis(Controls.LookDown, Controls.LookUp)}  Right stick            Up / Down
            Gun            {Held(Controls.FireGun)}  Cross                  Space
            Missile        {Held(Controls.FireMissile)}  Circle                 F
            Switch weapon  {Held(Controls.SwitchWeapon)}  Square                 R
            Switch target  {Held(Controls.SwitchTarget)}  Triangle               T
            Change view    {Held(Controls.ChangeView)}  R3                     V
            Flare          {Held(Controls.IsFlarePressed())}  L3 + R3                G
            Radar map      {Held(Controls.RadarMap)}  Touchpad               M
            Pause          {Held(Controls.Pause)}  Options                Esc
            Damage dummy   {Held(Controls.DamageDummy)}  --                     F2
            Damage jet     {Held(Controls.DamageJet)}  --                     F3
            """;
    }

    static string Axis(StringName negative, StringName positive) => Signed(Input.GetAxis(negative, positive));

    static string Signed(float value) => value.ToString("+0.00;-0.00; 0.00");

    static string Amount(StringName action) => Input.GetActionStrength(action).ToString(" 0.00");

    static string Held(StringName action) => Held(Input.IsActionPressed(action));

    static string Held(bool held) => held ? "   ON" : "   --";
}
