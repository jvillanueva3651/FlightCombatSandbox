using System.Collections.Generic;
using Godot;

/// <summary>
/// The jet's gun: holding Cross fires tracer rounds straight ahead from where the gun sits on the jet.
/// Each round flies in a straight line at the jet's speed plus the muzzle speed and damages the Unit
/// whose hit box it reaches; terrain stops it, and it vanishes at the end of its range.
/// </summary>
public partial class Gun : Node3D
{
    const float FireRate = 20;       // rounds per second
    const int RoundDamage = 4;       // damage per hit: 25 hits destroy a light fighter
    const float MuzzleSpeed = 1000;  // m/s on top of the jet's speed
    const float RoundLife = 1.2f;    // seconds before a round vanishes: about 1.4 km at cruise speed

    sealed class Round
    {
        public MeshInstance3D Tracer;
        public Vector3 Velocity;  // m/s
        public float Age;         // seconds
    }

    readonly List<Round> _rounds = new();

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
                Fire();
        else
            _cooldown = Mathf.Max(_cooldown, 0);

        var space = GetWorld3D().DirectSpaceState;
        for (var i = _rounds.Count - 1; i >= 0; i--)
        {
            var round = _rounds[i];
            var from = round.Tracer.GlobalPosition;
            var to = from + round.Velocity * dt;
            var hit = space.IntersectRay(new PhysicsRayQueryParameters3D { From = from, To = to, CollideWithAreas = true });
            round.Age += dt;
            if (hit.Count == 0 && round.Age < RoundLife)
            {
                round.Tracer.GlobalPosition = to;
                continue;
            }
            if (hit.Count > 0)
                (hit["collider"].As<Node>().GetParent() as Unit)?.Damage(RoundDamage);  // terrain just stops it
            round.Tracer.QueueFree();
            _rounds.RemoveAt(i);
        }
    }

    // Tracers go in the jet's parent rather than under the jet, which the camera hides in the nose view and while it's shot down.
    void Fire()
    {
        var tracer = new MeshInstance3D { Mesh = _tracerMesh, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off };
        _jet.GetParent().AddChild(tracer);
        tracer.GlobalTransform = GlobalTransform;
        _rounds.Add(new Round { Tracer = tracer, Velocity = -GlobalBasis.Z * (_jet.Speed / 3.6f + MuzzleSpeed) });
    }
}
