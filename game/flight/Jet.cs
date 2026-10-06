using Core;
using Godot;

/// <summary>
/// Flies a jet straight from its Handling, with no aerodynamics: R2 and L2 set its speed, hard pulls
/// and climbs bleed it, dives build it, and the stick (through the Feel Settings' response curve) and
/// L1/R1 turn it at the Handling rates, strongest at corner speed and easing in and out over its
/// response time. Below stall speed the nose falls toward the ground. Squeezing R2 and L2 together
/// while pulling makes a high-G turn: tighter, and much costlier in speed.
/// </summary>
public partial class Jet : Node3D
{
    /// <summary>The Unit Type whose Handling this jet flies with.</summary>
    [Export] public string UnitTypeId { get; set; } = "light-fighter";

    /// <summary>Current speed in km/h.</summary>
    public float Speed { get; private set; }

    /// <summary>R2 (+) against L2 (-), from -1 to 1.</summary>
    public float Throttle { get; private set; }

    /// <summary>How hard the jet is pulling, in G: 1 in level flight, more when pitching up.</summary>
    public float GLoad { get; private set; }

    Handling _handling;

    // How much of each full turn rate the jet uses, from -1 to 1; follows the stick over the response time.
    float _pitch, _roll, _yaw;

    // How hard R2 and L2 are squeezed together for a high-G turn, from 0 to 1; eased like the stick.
    float _squeeze;

    public override void _Ready()
    {
        _handling = Packs.Base.UnitTypes[UnitTypeId].Handling;
        Speed = _handling.CruiseSpeed;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _pitch = _handling.NextTurn(_pitch, Stick(Controls.PitchDown, Controls.PitchUp), dt);  // + = nose up
        _roll = _handling.NextTurn(_roll, Stick(Controls.RollLeft, Controls.RollRight), dt);    // + = right wing down
        _yaw = _handling.NextTurn(_yaw, Stick(Controls.YawLeft, Controls.YawRight), dt);        // + = nose right
        Throttle = Input.GetAxis(Controls.Brake, Controls.Throttle);
        var squeeze = Mathf.Min(Input.GetActionStrength(Controls.Brake), Input.GetActionStrength(Controls.Throttle));
        _squeeze = _handling.NextTurn(_squeeze, squeeze, dt);
        Speed = _handling.NextSpeed(Speed, Throttle, dt);
        Speed = _handling.SpeedAfterTurn(Speed, _pitch, dt);
        Speed = _handling.SpeedAfterHighG(Speed, _pitch, _squeeze, dt);
        Speed = _handling.SpeedAfterClimb(Speed, -GlobalTransform.Basis.Z.Y, dt);  // the nose points along -Z

        var scale = _handling.TurnRateScale(Speed);
        var pitchRate = Mathf.DegToRad(_handling.PitchRate * _pitch * scale * _handling.HighGTurnScale(_squeeze));  // radians per second
        RotateObjectLocal(Vector3.Right, pitchRate * dt);
        RotateObjectLocal(Vector3.Forward, Mathf.DegToRad(_handling.RollRate * _roll * scale * dt));
        RotateObjectLocal(Vector3.Down, Mathf.DegToRad(_handling.YawRate * _yaw * scale * dt));

        // A stall drops the nose toward the ground, whichever way the jet is banked.
        var nose = -GlobalTransform.Basis.Z;
        var drop = Mathf.Min(Mathf.DegToRad(_handling.NoseDropRate(Speed)) * dt, nose.AngleTo(Vector3.Down));
        if (drop > 0)
            GlobalRotate(nose.Cross(Vector3.Down).Normalized(), drop);
        Transform = Transform.Orthonormalized(); // stops rounding errors slowly skewing the jet

        var metersPerSecond = Speed / 3.6f;
        Translate(Vector3.Forward * metersPerSecond * dt);

        // Pitching bends the flight path toward the jet's top side; gravity adds its share along that axis.
        GLoad = metersPerSecond * pitchRate / 9.81f + GlobalTransform.Basis.Y.Y;
    }

    // An axis's position after the Feel Settings' response curve.
    static float Stick(StringName negative, StringName positive) => Packs.Base.Feel.Curve(Input.GetAxis(negative, positive));
}
