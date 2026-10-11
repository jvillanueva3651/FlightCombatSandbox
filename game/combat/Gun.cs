using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The jet's gun: holding Cross fires tracer rounds straight ahead from where the gun sits on the jet.
/// Each round leaves at the jet's speed plus the muzzle speed, falls toward the ground at three times
/// gravity, and damages the Unit whose hit box it passes within a meter of; terrain stops it, and it
/// vanishes at the end of its range.
/// </summary>
public partial class Gun : Node3D
{
    const float FireRate = 30;       // rounds per second
    const int RoundDamage = 4;       // damage per hit: 25 hits destroy a light fighter
    const float MuzzleSpeed = 1800;  // m/s on top of the jet's speed
    const float RoundLife = 2.5f;    // seconds before a round vanishes: 4.5 km ahead of the jet
    const float Gravity = 3 * 9.8f;  // m/s² a round falls at: three times real gravity, so the stream visibly arcs down far out
    const float HitRadius = 1;       // m: a round passing this close to a hit box hits it
    const float AimSmoothing = 0.2f; // seconds the aim point takes to follow most of a change in the jet's turn

    sealed class Round
    {
        public MeshInstance3D Tracer;
        public Vector3 Velocity;  // m/s
        public float Age;         // seconds
    }

    readonly List<Round> _rounds = new();
    // A ball swept along each round's path. One query for every round: a new one per round per frame can crash the process as it quits.
    readonly PhysicsShapeQueryParameters3D _sweep = new() { Shape = new SphereShape3D { Radius = HitRadius }, CollideWithAreas = true };

    // Seen from the chase camera the rounds fly almost straight away from it, so a real-sized tracer
    // shrinks to nothing within a few hundred meters. Instead each one is a dot facing the camera that
    // stays the same size on screen at any distance: as wide as TracerSize meters would look 1 m away.
    const float TracerSize = 0.015f;

    readonly QuadMesh _tracerMesh = new()
    {
        Size = new Vector2(TracerSize, TracerSize),
        Material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            FixedSize = true,
            AlbedoColor = new Color(1, 0.85f, 0.4f),
        },
    };

    Jet _jet;
    float _cooldown;     // seconds until the next round can fire
    Basis _lastBasis;    // the jet's orientation last frame
    bool _wasShotDown;   // the jet was shot down last frame
    Vector3 _turn;       // how fast the jet is turning about its own axes, eased: the axis, as long as the radians per second

    public override void _Ready()
    {
        _jet = GetParent<Jet>();
        _lastBasis = _jet.GlobalBasis;
    }

    /// <summary>
    /// Where a round fired now will sit from the jet once it's <paramref name="distance"/> meters out, if
    /// the jet keeps turning as it is, placed the same way from the jet as it is now: just under the
    /// nose line in straight flight, by the round's fall, and well below it in a pull-up, since the
    /// round flies on while the jet turns away. The gun reticle shows it on the HUD.
    /// </summary>
    public Vector3 AimPoint(float distance)
    {
        const int Steps = 10;
        var speed = _jet.Speed / 3.6f;
        var flight = distance / MuzzleSpeed;  // seconds: the round pulls away from the jet at the muzzle speed
        var step = flight / Steps;
        var spin = _turn.Length();
        var stepTurn = spin > 0 ? new Basis(_turn / spin, spin * step) : Basis.Identity;
        var halfTurn = spin > 0 ? new Basis(_turn / spin, spin * step / 2) : Basis.Identity;

        // Fly the jet on in steps, each along its heading halfway through the step, measured from the jet as it is now.
        var basis = Basis.Identity;
        var at = Vector3.Zero;
        for (var i = 0; i < Steps; i++)
        {
            at += basis * halfTurn * Vector3.Forward * speed * step;
            basis *= stepTurn;
        }
        var round = Transform * (Vector3.Forward * (speed + MuzzleSpeed) * flight);  // fired from the gun, straight along the nose
        round += _jet.GlobalBasis.Inverse() * Vector3.Down * Gravity * flight * flight / 2;  // and fallen this far since
        return _jet.GlobalTransform * (basis.Inverse() * (round - at));
    }

    // Runs after the jet's own _Process, since the gun is the jet's child, so rounds leave from where the jet is now.
    public override void _Process(double delta)
    {
        var dt = (float)delta;

        // How fast the jet is turning, from how far it turned since last frame; not the jump back to
        // where it began when it starts again after being shot down. Eased, so the aim point glides
        // into each change in the turn rather than jumping with it.
        var turned = (_lastBasis.Inverse() * _jet.GlobalBasis).GetRotationQuaternion();
        var axis = new Vector3(turned.X, turned.Y, turned.Z);  // the turn's axis, as long as the sine of half its angle
        var turn = _wasShotDown || axis.IsZeroApprox() ? Vector3.Zero : axis.Normalized() * 2 * Mathf.Atan2(axis.Length(), turned.W) / dt;
        _turn = _turn.Lerp(turn, 1 - Mathf.Exp(-dt / AimSmoothing));
        _lastBasis = _jet.GlobalBasis;
        _wasShotDown = _jet.ShotDown;

        _cooldown -= dt;
        if (Input.IsActionPressed(Controls.FireGun) && !_jet.ShotDown)
            for (; _cooldown <= 0; _cooldown += 1 / FireRate)
                Fire(-_cooldown);
        else
            _cooldown = Mathf.Max(_cooldown, 0);

        var space = GetWorld3D().DirectSpaceState;
        for (var i = _rounds.Count - 1; i >= 0; i--)
        {
            var round = _rounds[i];
            var from = round.Tracer.GlobalPosition;
            var move = (round.Velocity + Vector3.Down * Gravity * dt / 2) * dt;  // its fall this frame, exact under steady gravity
            round.Velocity += Vector3.Down * Gravity * dt;
            _sweep.Transform = new Transform3D(Basis.Identity, from);
            _sweep.Motion = move;
            var reached = space.CastMotion(_sweep)[1];  // how far along the move the ball first touches something: 1 for nothing
            round.Age += dt;
            if (reached >= 1 && round.Age < RoundLife)
            {
                round.Tracer.GlobalPosition = from + move;
                continue;
            }
            if (reached < 1)
            {
                _sweep.Transform = new Transform3D(Basis.Identity, from + move * reached);
                _sweep.Motion = Vector3.Zero;
                space.IntersectShape(_sweep).Select(hit => hit["collider"].As<Node>().GetParent()).OfType<Unit>().FirstOrDefault()?.Damage(RoundDamage);  // terrain just stops it
            }
            round.Tracer.QueueFree();
            _rounds.RemoveAt(i);
        }
    }

    // Tracers go in the jet's parent rather than under the jet, which the camera hides in the nose view and while it's shot down.
    // late: seconds ago the round was due; at this fire rate a frame often fires two. It leaves as far
    // ahead of the gun as it has flown since, so the stream stays evenly spaced rather than in pairs.
    void Fire(float late)
    {
        var tracer = new MeshInstance3D { Mesh = _tracerMesh, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        _jet.GetParent().AddChild(tracer);
        tracer.GlobalTransform = GlobalTransform;
        tracer.GlobalPosition -= GlobalBasis.Z * MuzzleSpeed * late;  // the jet carries it at the jet's speed, so only the muzzle speed puts it ahead
        _rounds.Add(new Round { Tracer = tracer, Velocity = -GlobalBasis.Z * (_jet.Speed / 3.6f + MuzzleSpeed) });
    }
}
