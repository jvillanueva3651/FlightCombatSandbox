using Core;
using Godot;

/// <summary>
/// Flies a jet straight from its Handling, with no aerodynamics, on the controls set each frame by
/// whoever flies it: the AI Pilot among its parts if it has one, otherwise the Player's controller.
/// R2 and L2 set its speed, hard pulls and climbs bleed it, dives build it, and the stick and L1/R1
/// turn it at the Handling rates, strongest at corner speed and easing in and out over its response
/// time. Below stall speed the nose falls toward the ground. Squeezing R2 and L2 together while
/// pulling makes a high-G turn: tighter, and much costlier in speed. It's a Unit: losing all its
/// health or hitting the ground shoots it down. After it explodes the Player's jet starts again, a
/// short pause later, where it first started and with full health; an AI Pilot's jet disappears.
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

    /// <summary>True from being shot down until the jet starts again: while its health is zero.</summary>
    public bool ShotDown => Health == 0;

    /// <summary>The jet's Targeting, one of its parts in player_jet.tscn.</summary>
    public Targeting Targeting => GetNode<Targeting>("Targeting");

    /// <summary>The jet's missile launcher, one of its parts in player_jet.tscn and ai_jet.tscn.</summary>
    public MissileLauncher MissileLauncher => GetNode<MissileLauncher>("MissileLauncher");

    /// <summary>The jet's gun, one of its parts in player_jet.tscn.</summary>
    public Gun Gun => GetNode<Gun>("Gun");

    /// <summary>The AI Pilot that flies the jet and fires its missiles, one of its parts in ai_jet.tscn; null for the Player's.</summary>
    public AiPilot AiPilot { get; private set; }

    const float RespawnDelay = 2;  // seconds from being shot down to starting again

    // The ball swept along each frame's move to find the ground: about the jet's size, and swept rather
    // than checked where the move ends, so a fast frame can't skip through the ground.
    // Asked every frame with the same query rather than a new one.
    readonly PhysicsShapeQueryParameters3D _crashSweep = new() { Shape = new SphereShape3D { Radius = 4 } };

    Transform3D _start;
    float _sinceShotDown;

    // How much of each full turn rate the jet uses, from -1 to 1; follows the stick over the response time.
    float _pitch, _roll, _yaw;

    // How hard R2 and L2 are squeezed together for a high-G turn, from 0 to 1; eased like the stick.
    float _squeeze;

    public override void _Ready()
    {
        base._Ready();
        Handling = Packs.Base.Feel.Apply(UnitType.Handling);
        Speed = Handling.CruiseSpeed;
        _start = Transform;
        AiPilot = GetNodeOrNull<AiPilot>("AiPilot");
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

        Handling = Packs.Base.Feel.Apply(UnitType.Handling);  // with the Feel Settings' multipliers on top
        var input = AiPilot?.Fly(dt) ?? FlightInput.FromController();
        _pitch = Handling.NextTurn(_pitch, input.Pitch, dt);
        _roll = Handling.NextTurn(_roll, input.Roll, dt);
        _yaw = Handling.NextTurn(_yaw, input.Yaw, dt);
        Throttle = input.Throttle;
        _squeeze = Handling.NextTurn(_squeeze, input.Squeeze, dt);
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
        _crashSweep.Transform = GlobalTransform;
        _crashSweep.Motion = move;
        var free = GetWorld3D().DirectSpaceState.CastMotion(_crashSweep)[0];
        GlobalPosition += move * free;
        if (free < 1)
        {
            Damage(Health);
            return;
        }

        // Pitching bends the flight path toward the jet's top side; gravity adds its share along that axis.
        GLoad = metersPerSecond * pitchRate / 9.81f + GlobalTransform.Basis.Y.Y;
    }

    // The Player's jet stays in the scene, stopped and hidden by the camera, until it starts again.
    protected override void Destroyed()
    {
        if (AiPilot != null)
        {
            base.Destroyed();
            return;
        }
        _sinceShotDown = 0;
        Speed = 0;
    }

    void Respawn()
    {
        Health = UnitType.Health;
        Transform = _start;
        Speed = Handling.CruiseSpeed;
        _pitch = _roll = _yaw = _squeeze = 0;
    }
}
