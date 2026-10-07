using Core;
using Godot;

/// <summary>
/// Flies a jet straight from its Handling, with no aerodynamics: R2 and L2 set its speed, hard pulls
/// and climbs bleed it, dives build it, and the stick (through the Feel Settings' response curve) and
/// L1/R1 turn it at the Handling rates, strongest at corner speed and easing in and out over its
/// response time. Below stall speed the nose falls toward the ground. Squeezing R2 and L2 together
/// while pulling makes a high-G turn: tighter, and much costlier in speed. It's a Unit: losing all its
/// health or hitting the ground shoots it down, and after it explodes and a short pause it starts
/// again where it first started, with full health.
/// </summary>
public partial class Jet : Unit
{
    /// <summary>Current speed in km/h.</summary>
    public float Speed { get; private set; }

    /// <summary>R2 (+) against L2 (-), from -1 to 1.</summary>
    public float Throttle { get; private set; }

    /// <summary>How hard the jet is pulling, in G: 1 in level flight, more when pitching up.</summary>
    public float GLoad { get; private set; }

    /// <summary>The Handling the jet flies with: its Unit Type's, with the Feel Settings' multipliers on top.</summary>
    public Handling Handling { get; private set; }

    /// <summary>How hard a high-G turn is pulling, from 0 to 1: the trigger squeeze times the pull squared, like its speed loss.</summary>
    public float HighG => _squeeze * _pitch * _pitch;

    /// <summary>True from being shot down until the jet starts again.</summary>
    public bool ShotDown { get; private set; }

    const float RespawnDelay = 2;  // seconds from being shot down to starting again

    // The ball swept along each frame's move to find the ground: about the jet's size, and swept rather
    // than checked where the move ends, so a fast frame can't skip through the ground.
    readonly SphereShape3D _crashBall = new() { Radius = 4 };

    Handling _handling;
    Transform3D _start;
    float _sinceShotDown;

    // How much of each full turn rate the jet uses, from -1 to 1; follows the stick over the response time.
    float _pitch, _roll, _yaw;

    // How hard R2 and L2 are squeezed together for a high-G turn, from 0 to 1; eased like the stick.
    float _squeeze;

    public override void _Ready()
    {
        base._Ready();
        _handling = UnitType.Handling;
        Handling = Packs.Base.Feel.Apply(_handling);
        Speed = Handling.CruiseSpeed;
        _start = Transform;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        if (ShotDown)
        {
            _sinceShotDown += dt;
            if (_sinceShotDown >= RespawnDelay)
                Respawn();
            return;
        }

        Handling = Packs.Base.Feel.Apply(_handling);  // with the Feel Settings' multipliers on top
        _pitch = Handling.NextTurn(_pitch, Stick(Controls.PitchDown, Controls.PitchUp), dt);  // + = nose up
        _roll = Handling.NextTurn(_roll, Stick(Controls.RollLeft, Controls.RollRight), dt);    // + = right wing down
        _yaw = Handling.NextTurn(_yaw, Stick(Controls.YawLeft, Controls.YawRight), dt);        // + = nose right
        Throttle = Input.GetAxis(Controls.Brake, Controls.Throttle);
        var squeeze = Mathf.Min(Input.GetActionStrength(Controls.Brake), Input.GetActionStrength(Controls.Throttle));
        _squeeze = Handling.NextTurn(_squeeze, squeeze, dt);
        Speed = Handling.NextSpeed(Speed, Throttle, dt);
        Speed = Handling.SpeedAfterTurn(Speed, _pitch, dt);
        Speed = Handling.SpeedAfterHighG(Speed, _pitch, _squeeze, dt);
        Speed = Handling.SpeedAfterClimb(Speed, -GlobalTransform.Basis.Z.Y, dt);  // the nose points along -Z

        var scale = Handling.TurnRateScale(Speed);
        var pitchRate = Mathf.DegToRad(Handling.PitchRate * _pitch * scale * Handling.HighGTurnScale(_squeeze));  // radians per second
        RotateObjectLocal(Vector3.Right, pitchRate * dt);
        RotateObjectLocal(Vector3.Forward, Mathf.DegToRad(Handling.RollRate * _roll * scale * dt));
        RotateObjectLocal(Vector3.Down, Mathf.DegToRad(Handling.YawRate * _yaw * scale * dt));

        // A stall drops the nose toward the ground, whichever way the jet is banked.
        var nose = -GlobalTransform.Basis.Z;
        var drop = Mathf.Min(Mathf.DegToRad(Handling.NoseDropRate(Speed)) * dt, nose.AngleTo(Vector3.Down));
        if (drop > 0)
            GlobalRotate(nose.Cross(Vector3.Down).Normalized(), drop);
        Transform = Transform.Orthonormalized(); // stops rounding errors slowly skewing the jet

        // Move forward, as far as the ground allows; touching it shoots the jet down.
        var metersPerSecond = Speed / 3.6f;
        var move = GlobalBasis * Vector3.Forward * metersPerSecond * dt;
        var free = GetWorld3D().DirectSpaceState.CastMotion(new PhysicsShapeQueryParameters3D { Shape = _crashBall, Transform = GlobalTransform, Motion = move })[0];
        GlobalPosition += move * free;
        if (free < 1)
        {
            Damage(Health);
            return;
        }

        // Pitching bends the flight path toward the jet's top side; gravity adds its share along that axis.
        GLoad = metersPerSecond * pitchRate / 9.81f + GlobalTransform.Basis.Y.Y;
    }

    // The jet stays in the scene, stopped and hidden by the camera, until it starts again.
    protected override void Destroyed()
    {
        ShotDown = true;
        _sinceShotDown = 0;
        Speed = 0;
    }

    void Respawn()
    {
        ShotDown = false;
        Health = UnitType.Health;
        Transform = _start;
        Speed = Handling.CruiseSpeed;
        _pitch = _roll = _yaw = _squeeze = 0;
    }

    // An axis's position after the Feel Settings' response curve.
    static float Stick(StringName negative, StringName positive) => Packs.Base.Feel.Curve(Input.GetAxis(negative, positive));
}
