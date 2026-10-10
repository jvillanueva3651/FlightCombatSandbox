using System.Linq;
using Godot;

/// <summary>
/// Flies its jet, its parent, through the same controls the Player uses. It chases its Target, the
/// nearest hostile jet: it aims ahead of it, at where it will be by the time the jet could get there,
/// so it cuts inside the Target's turn, and it works the triggers to sit a set way behind at the
/// Target's speed. With no Target it flies to its point at cruise speed instead: close to the point it
/// flies on level through it, and some way past it turns back for another pass, rather than circling
/// it in a tight turn. Either way it rolls to put where it's headed straight above the canopy and
/// pulls, the way an Ace Combat turn goes, and near the nose it levels the wings and finishes with
/// small stick and L1/R1 corrections. The ground comes before all of that: it looks a few seconds
/// along its nose, and while the ground is in the way it rolls the sky above its canopy and pulls up;
/// it heads for its goal level while the straight way there runs through the ground; and it climbs
/// for room before turning hard close to the ground.
/// </summary>
public partial class AiPilot : Node
{
    /// <summary>The point the jet flies to while it has no Target.</summary>
    [Export] public Node3D Point { get; set; }

    /// <summary>The test scene's switch (F6): off, every AI Pilot flies to its point rather than chase a Target.</summary>
    public static bool Chasing { get; set; } = true;

    const float NearNose = 15;        // degrees off the nose inside which it eases toward wings level instead of banking at the point
    const float RollGain = 2;         // roll stick per radian of bank still to go
    const float PitchGain = 2;        // stick per radian the point sits above the nose
    const float YawGain = 3;          // L1/R1 per radian the point sits beside the nose
    const float PassRange = 150;      // meters from the point inside which it stops steering and flies on level through it
    const float TurnBackRange = 1000; // meters from the point at which, flying on past it, it turns back
    const float LookAhead = 3;        // seconds of flight ahead it checks for ground
    const float Clearance = 30;       // meters it keeps between its path and the ground
    const float HardTurn = 45;        // degrees off the nose beyond which a turn needs TurnRoom
    const float TurnRoom = 300;       // meters of clear air around it that a hard turn needs
    const float ClimbOut = 30;        // degrees it climbs at, straight ahead, to get that room
    const float MaxLead = 3;          // seconds ahead of its Target it aims, at most
    const float TrailDistance = 600;  // meters behind its Target it tries to sit
    const float CloseRate = 0.25f;    // km/h faster than its Target it flies for each meter further back than that, and slower for each meter closer
    const float StallMargin = 50;     // km/h above stall speed it stays when slowing down for its Target

    readonly SphereShape3D _clearBall = new() { Radius = Clearance };  // swept along a path to find ground on it
    readonly SphereShape3D _turnBall = new() { Radius = TurnRoom };    // around the jet, to find ground near it

    Jet _jet;
    bool _passing;  // flying on level through the point until TurnBackRange past it

    public override void _Ready() => _jet = GetParent<Jet>();

    /// <summary>Where it puts the controls this frame. Its jet asks once a frame, before moving.</summary>
    public FlightInput Fly()
    {
        var fromJet = _jet.GlobalBasis.Inverse();
        var sky = fromJet * Vector3.Up;
        var target = Chasing ? Unit.Hostiles(GetTree(), _jet.Team).OfType<Jet>().MinBy(DistanceTo) : null;
        var offset = (target != null ? LeadPoint(target) : Point.GlobalPosition) - _jet.GlobalPosition;
        var throttle = _jet.Handling.ThrottleFor(target != null ? ChaseSpeed(target) : _jet.Handling.CruiseSpeed);

        // Ground ahead: roll the sky above the canopy and pull, once the canopy isn't facing the ground,
        // until the way is clear. This close to its point, that ends the pass.
        var nose = -_jet.GlobalBasis.Z;
        if (GroundAlong(nose))
        {
            _passing |= target == null && offset.Length() < TurnBackRange;
            return new FlightInput(Pitch: sky.Y > -0.2f ? 1 : 0, Roll: Mathf.Clamp(Mathf.Atan2(sky.X, sky.Y) * RollGain, -1, 1), Yaw: 0, throttle, Squeeze: 0);
        }

        // Where it heads: its goal, the Target's lead point or its own point; level toward that while
        // the straight way there runs through the ground, so it doesn't dive at what the pull-up would
        // only haul it back from; and level along its heading while passing its point.
        _passing = target == null && (offset.Length() < PassRange || (_passing && offset.Length() < TurnBackRange));
        var heading = _passing ? nose with { Y = 0 } : GroundAlong(offset.Normalized()) ? offset with { Y = 0 } : offset;
        var to = (fromJet * heading).Normalized();  // seen from the jet: -Z ahead, +Y above, +X right

        // A hard turn low down would swing it into ground its nose can't see yet, so without room to
        // turn it climbs straight ahead until it has some.
        if (Vector3.Forward.AngleTo(to) > Mathf.DegToRad(HardTurn) && GroundNear())
            to = (fromJet * ((nose with { Y = 0 }).Normalized() + Vector3.Up * Mathf.Tan(Mathf.DegToRad(ClimbOut)))).Normalized();

        var far = Mathf.Min(Vector3.Forward.AngleTo(to) / Mathf.DegToRad(NearNose), 1);  // 0 on the nose, 1 from NearNose out
        var bank = Mathf.LerpAngle(Mathf.Atan2(sky.X, sky.Y), Mathf.Atan2(to.X, to.Y), far);  // roll still to go: to wings level on the nose, to the point above the canopy further out
        return new FlightInput(
            Pitch: Mathf.Clamp(Mathf.Atan2(to.Y, -to.Z) * PitchGain, far - 1, 1),  // pushes only near the nose
            Roll: Mathf.Clamp(bank * RollGain, -1, 1),
            Yaw: Mathf.Clamp(Mathf.Atan2(to.X, -to.Z) * YawGain, -1, 1) * (1 - far),
            throttle,
            Squeeze: 0);
    }

    float DistanceTo(Unit unit) => _jet.GlobalPosition.DistanceTo(unit.GlobalPosition);

    // Where its Target will be by the time the jet could make up the distance to its place behind it,
    // MaxLead ahead at most: ahead of the Target while it's further back than TrailDistance, to cut
    // the corner, and the Target itself once it's there. Leading by the whole distance would be a
    // collision course.
    Vector3 LeadPoint(Jet target)
    {
        var lead = Mathf.Clamp((DistanceTo(target) - TrailDistance) / (_jet.Speed / 3.6f), 0, MaxLead);
        return target.GlobalPosition - target.GlobalBasis.Z * target.Speed / 3.6f * lead;
    }

    // The speed, in km/h, that brings it to TrailDistance behind its Target and holds it there: the
    // Target's own, faster the further back it is and slower when it's too close.
    float ChaseSpeed(Jet target) => Mathf.Clamp(
        target.Speed + (DistanceTo(target) - TrailDistance) * CloseRate,
        _jet.Handling.StallSpeed + StallMargin,
        _jet.Handling.TopSpeed);

    // True when a ball Clearance wide, swept from the jet along a direction as far as it flies in LookAhead, meets the ground.
    bool GroundAlong(Vector3 direction)
    {
        var path = direction * _jet.Speed / 3.6f * LookAhead;
        var query = new PhysicsShapeQueryParameters3D { Shape = _clearBall, Transform = _jet.GlobalTransform, Motion = path };
        return _jet.GetWorld3D().DirectSpaceState.CastMotion(query)[1] < 1;
    }

    // True when the ground is within TurnRoom of the jet, in any direction.
    bool GroundNear()
    {
        var query = new PhysicsShapeQueryParameters3D { Shape = _turnBall, Transform = _jet.GlobalTransform };
        return _jet.GetWorld3D().DirectSpaceState.IntersectShape(query, 1).Count > 0;
    }
}
