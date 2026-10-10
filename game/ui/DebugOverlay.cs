using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Shows the jet's flight numbers, its and its Target's health and what every control is doing right
/// now, from the DualSense and the keyboard. It starts hidden, since the HUD shows what matters in
/// flight, and F4 shows and hides it. F2 damages the Target and F3 the jet, shown or not, and F5 moves
/// the test scene's AI point to the jet. The test dummies, every Unit on another Team when the scene
/// starts (AI jets too), each come back as they were placed a few seconds after being destroyed.
/// </summary>
public partial class DebugOverlay : CanvasLayer
{
    /// <summary>The jet whose speed, altitude, throttle, G-load and health are shown, and that F3 damages.</summary>
    [Export] public Jet Jet { get; set; }

    /// <summary>The point the test scene's AI Pilots fly to, which F5 moves to the jet.</summary>
    [Export] public Node3D AiPoint { get; set; }

    const int DebugDamage = 25;          // damage per press of F2 or F3
    const float DummyRespawnDelay = 5;   // seconds from a dummy's destruction to a fresh one

    sealed class Dummy
    {
        public Unit Live;          // the one in the scene, gone once destroyed
        public Unit Fresh;         // a copy as the scene placed it, kept out of the scene
        public Node Parent;
        public float SinceDestroyed;
    }

    readonly List<Dummy> _dummies = new();
    Label _label;
    Targeting _targeting;

    // The dummies come before this overlay in the scene, so they're ready and listed by Unit.All by now.
    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        _targeting = Jet.Targeting;
        foreach (var unit in Unit.All(GetTree()).Where(unit => unit.Team != Jet.Team))
            _dummies.Add(new Dummy { Live = unit, Fresh = (Unit)unit.Duplicate(), Parent = unit.GetParent() });
    }

    public override void _ExitTree()
    {
        foreach (var dummy in _dummies)
            dummy.Fresh.Free();  // never in the scene, so nothing else frees them
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed(Controls.DamageTarget))
            _targeting.Target?.Damage(DebugDamage);
        else if (e.IsActionPressed(Controls.DamageJet))
            Jet.Damage(DebugDamage);
        else if (e.IsActionPressed(Controls.DebugOverlay))
            Visible = !Visible;
        else if (e.IsActionPressed(Controls.MoveAiPoint))
            AiPoint.GlobalPosition = Jet.GlobalPosition;
    }

    public override void _Process(double delta)
    {
        foreach (var dummy in _dummies.Where(dummy => !IsInstanceValid(dummy.Live)))
        {
            dummy.SinceDestroyed += (float)delta;
            if (dummy.SinceDestroyed < DummyRespawnDelay)
                continue;
            dummy.SinceDestroyed = 0;
            dummy.Live = (Unit)dummy.Fresh.Duplicate();
            dummy.Parent.AddChild(dummy.Live);
        }

        if (!Visible)
            return;  // nothing to show while hidden, though the dummies above still come back
        var targetHealth = _targeting.Target is { } target ? $"{target.Health,5}" : " none";
        _label.Text = $"""
            Speed          {Jet.Speed,5:0} km/h
            Altitude       {Jet.GlobalPosition.Y,5:0} m
            Throttle       {Signed(Jet.Throttle)}
            G-load         {Jet.GLoad,5:0.0} G
            Jet health     {Jet.Health,5}
            Target health  {targetHealth}

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
            Damage target  {Held(Controls.DamageTarget)}  --                     F2
            Damage jet     {Held(Controls.DamageJet)}  --                     F3
            Debug overlay  {Held(Controls.DebugOverlay)}  --                     F4
            Move AI point  {Held(Controls.MoveAiPoint)}  --                     F5
            """;
    }

    static string Axis(StringName negative, StringName positive) => Signed(Input.GetAxis(negative, positive));

    static string Signed(float value) => value.ToString("+0.00;-0.00; 0.00");

    static string Amount(StringName action) => Input.GetActionStrength(action).ToString(" 0.00");

    static string Held(StringName action) => Held(Input.IsActionPressed(action));

    static string Held(bool held) => held ? "   ON" : "   --";
}
