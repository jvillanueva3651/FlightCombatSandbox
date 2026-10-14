using System.Collections.Generic;
using System.Linq;
using Core;
using Godot;

/// <summary>
/// A missile in flight. It speeds up from its launch speed to its Weapon Type's speed and turns toward
/// its Target at no more than the type's turn rate; once the Target slips outside the seeker cone
/// (overshot, or out-turned by a tight break) it loses it and flies straight. It explodes when a
/// hostile Unit comes within the blast radius, damaging every hostile that close, on hitting the
/// ground, or at the end of its lifetime. Its smoke trail lingers after it.
/// </summary>
public partial class Missile : Node3D
{
    // The Godot node group every missile joins so Homing can list them, as Units join theirs.
    const string NodeGroup = "missiles";

    /// <summary>What kind of missile this is.</summary>
    public WeaponType Type { get; set; }

    /// <summary>The Team that fired it; it explodes only near Units on other Teams.</summary>
    public int Team { get; set; }

    /// <summary>The Unit it homes on, or null to fly straight.</summary>
    public Unit Target { get; set; }

    /// <summary>Current speed in km/h.</summary>
    public float Speed { get; set; }

    float _age;  // seconds since launch

    /// <summary>The missiles in flight that are homing on <paramref name="unit"/>.</summary>
    public static IEnumerable<Missile> Homing(SceneTree tree, Unit unit) =>
        tree.GetNodesInGroup(NodeGroup).Cast<Missile>().Where(missile => missile.Target == unit && !missile.IsQueuedForDeletion());

    // Moves before everything else each frame, so the camera following it sits just behind where the
    // missile is now, not a frame's flight behind.
    public override void _Ready()
    {
        ProcessPriority = -1;
        AddToGroup(NodeGroup);
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        _age += dt;
        Speed = Mathf.MoveToward(Speed, Type.Speed, Type.Acceleration * dt);

        if (Target != null && (!IsInstanceValid(Target) || Target.Health == 0))
            Target = null;
        if (Target != null)
        {
            var nose = -GlobalBasis.Z;
            var toTarget = Target.GlobalPosition - GlobalPosition;
            var off = nose.AngleTo(toTarget);
            if (off > Mathf.DegToRad(Type.SeekerCone))
                Target = null;  // lost: it can't see the Target any more
            else if (off > 0.0001f)
                GlobalRotate(nose.Cross(toTarget).Normalized(), Mathf.Min(off, Mathf.DegToRad(Type.TurnRate) * dt));
        }

        var from = GlobalPosition;
        var to = from - GlobalBasis.Z * Speed / 3.6f * dt;
        var ground = GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(from, to));
        if (ground.Count > 0)
        {
            Explode(ground["position"].AsVector3());
            return;
        }
        // The closest the missile passes each Unit on this frame's move, so a fast one can't skip past.
        foreach (var unit in Unit.Hostiles(GetTree(), Team))
        {
            var closest = Geometry3D.GetClosestPointToSegment(unit.GlobalPosition, from, to);
            if (closest.DistanceTo(unit.GlobalPosition) <= Type.BlastRadius)
            {
                Explode(closest);
                return;
            }
        }
        GlobalPosition = to;
        if (_age >= Type.Lifetime)
            Explode(to);
    }

    void Explode(Vector3 at)
    {
        foreach (var unit in Unit.Hostiles(GetTree(), Team))
            if (unit.GlobalPosition.DistanceTo(at) <= Type.BlastRadius)
                unit.Damage(Type.Damage);

        Explosion.Spawn(GetParent(), at);

        // The trail stays in the sky after the missile is gone, then fades and is freed.
        var trail = GetNode<GpuParticles3D>("Trail");
        trail.Reparent(GetParent());
        trail.Emitting = false;
        GetTree().CreateTimer(trail.Lifetime).Timeout += trail.QueueFree;
        QueueFree();
    }
}
