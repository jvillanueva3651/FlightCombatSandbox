using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The jet's gun: holding Cross fires tracer rounds straight ahead from where the gun sits on the jet.
/// Each round flies in a straight line at the jet's speed plus the muzzle speed and damages the Unit
/// whose hit box it passes within a meter of; terrain stops it, and it vanishes at the end of its range.
/// </summary>
public partial class Gun : Node3D
{
    const float FireRate = 80;       // rounds per second
    const int RoundDamage = 4;       // damage per hit: 25 hits destroy a light fighter
    const float MuzzleSpeed = 1000;  // m/s on top of the jet's speed
    const float RoundLife = 1.2f;    // seconds before a round vanishes: about 1.4 km at cruise speed
    const float HitRadius = 1;       // m: a round passing this close to a hit box hits it

    sealed class Round
    {
        public MeshInstance3D Tracer;
        public Vector3 Velocity;  // m/s
        public float Age;         // seconds
    }

    readonly List<Round> _rounds = new();
    readonly SphereShape3D _hitBall = new() { Radius = HitRadius };  // swept along each round's path

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
    float _cooldown;  // seconds until the next round can fire

    public override void _Ready() => _jet = GetParent<Jet>();

    // Runs after the jet's own _Process, since the gun is the jet's child, so rounds leave from where the jet is now.
    public override void _Process(double delta)
    {
        var dt = (float)delta;
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
            var move = round.Velocity * dt;
            var query = new PhysicsShapeQueryParameters3D { Shape = _hitBall, Transform = new Transform3D(Basis.Identity, from), Motion = move, CollideWithAreas = true };
            var reached = space.CastMotion(query)[1];  // how far along the move the ball first touches something: 1 for nothing
            round.Age += dt;
            if (reached >= 1 && round.Age < RoundLife)
            {
                round.Tracer.GlobalPosition = from + move;
                continue;
            }
            if (reached < 1)
            {
                query.Transform = new Transform3D(Basis.Identity, from + move * reached);
                query.Motion = Vector3.Zero;
                space.IntersectShape(query).Select(hit => hit["collider"].As<Node>().GetParent()).OfType<Unit>().FirstOrDefault()?.Damage(RoundDamage);  // terrain just stops it
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
