using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Flies its jet, its parent, through the same controls the Player uses, as well as its Skill allows:
/// the Skill sets how many G it pulls, how long it flies its pattern between attack runs and flies on
/// after one, whether it turns back on a hostile jet behind it, how quickly it notices its Target's
/// speed changing, and how slow it lets a hard turn make it before it works the triggers again. It
/// flies one maneuver at a time. Its pattern is flying to its point at cruise speed: close to the
/// point it flies on level through it, and some way past it turns back for another pass, rather than
/// circling it in a tight turn. After its Skill's wait in its pattern it makes an attack run on its
/// Target, the nearest hostile jet, though a Rookie or Regular only on one ahead of it: it aims
/// ahead of it while it's further back than its place behind it, so it cuts inside the Target's
/// turn, and it works the triggers to close on it. The run ends once it has held its Target in the
/// missile seeker for the lock time, the shot it will take once it's armed; or after RunLimit; or,
/// for a Rookie or Regular, when the Target gets behind it. Then it breaks off, flying straight and
/// level for its Skill's time, and goes back to its pattern.
/// <para>
/// A missile fired at it can cut in on any of that. It sees what is ahead of its nose, and looks
/// behind only every so often, by Skill, so a missile from behind goes unnoticed until its next
/// look. Each missile it sees gets one roll against its Skill's chance; on a pass it evades, turning
/// level and square across the missile's path at full throttle until no missile it's evading still
/// homes on it. Every evasion costs a share of its Stamina, and pulling flat out drains it too; it
/// refills the rest of the time. With none left it recovers, held to a gentle G with no attack runs
/// and no evading, straight and level or in its pattern by an even chance, until a share is back.
/// </para>
/// In every maneuver it rolls to put where
/// it's headed straight above the canopy and pulls, the way an Ace Combat turn goes, and near the
/// nose it levels the wings and finishes with small stick and L1/R1 corrections. The ground comes
/// before all of that, and before its G limit: it looks a few seconds
/// along its nose, and while the ground is in the way it rolls the sky above its canopy and pulls up;
/// it heads for its goal level while the straight way there runs through the ground; and it climbs
/// for room before turning hard close to the ground.
/// </summary>
public partial class AiPilot : Node
{
    /// <summary>The point the jet flies to in its pattern.</summary>
    [Export] public Node3D Point { get; set; }

    /// <summary>How good it is.</summary>
    [Export] public Skill Skill { get; set; } = Skill.Regular;

    /// <summary>What it's flying right now.</summary>
    public Maneuver Maneuver { get; private set; }

    /// <summary>How much hard flying it has left, from 0 to 1.</summary>
    public float Stamina { get; private set; } = 1;

    /// <summary>
    /// Whether it squeezes the triggers for a high-G turn through an evasion, still inside its G limit.
    /// Off for every Skill: measured in task 2.44, a break in time beat the missile without it. Kept
    /// so it can be switched on later.
    /// </summary>
    public bool HighGEvade { get; set; }

    /// <summary>The test scene's switch (F6): off, no AI Pilot makes attack runs, and each flies its pattern only.</summary>
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
    const float HardPull = 0.5f;      // stick beyond which a Skill that lets its turns cost it speed lets go of the triggers
    const float RunLimit = 20;        // seconds an attack run lasts at most
    const float SalvoTime = 1;        // seconds from an evasion starting in which another missile it sees joins it for free
    const float FlatOut = 0.9f;       // share of the stick its G limit allows from which a pull drains its Stamina
    const float FlatOutTime = 30;     // seconds of pulling flat out that empty full Stamina
    const float GentleG = 4;          // the most G it pulls while recovering

    // What each Skill comes to, Rookie to Ace: the seconds it takes to notice its Target's speed changing;
    // the most G it pulls, as the debug overlay counts them; how slow it lets a hard turn make it, as a
    // share of corner speed, before it works the triggers again (Veterans and Aces never let go of
    // them); the seconds it flies its pattern before an attack run; the seconds it flies on after one;
    // the seconds between its looks behind; its chance of evading a missile it sees; the evasions full
    // Stamina pays for; and the seconds it takes to get one of them back.
    static readonly (float SpeedReaction, float GLimit, float TurnFloor, float Wait, float BreakOff, float Look, float Dodge, float Evasions, float Regain)[] Skills =
    {
        (2, 10, 0.65f, 12, 6, 8, 0.25f, 1, 15),
        (1, 14, 0.75f, 8, 4, 2.5f, 0.7f, 2, 10),
        (0.5f, 19, float.PositiveInfinity, 4, 3, 1.5f, 0.9f, 3, 8),
        (0.2f, 30, float.PositiveInfinity, 2, 1.5f, 1, 1, 5, 8),
    };

    // The two ground checks, each asked every frame with the same query rather than a new one: a ball
    // swept along a path, and a ball around the jet.
    readonly PhysicsShapeQueryParameters3D _alongPath = new() { Shape = new SphereShape3D { Radius = Clearance } };
    readonly PhysicsShapeQueryParameters3D _around = new() { Shape = new SphereShape3D { Radius = TurnRoom } };

    Jet _jet;
    bool _passing;  // flying on level through the point until TurnBackRange past it
    float _since;      // seconds it has flown this maneuver
    float _inSights;   // seconds an attack run has held its Target in the missile seeker
    Jet _seen;         // the Target it chased last frame
    float _seenSpeed;  // the speed, in km/h, it has seen that Target doing: it trails the real one by its Skill's reaction
    readonly HashSet<Missile> _judged = new();  // the missiles homing on it that it has seen and decided about
    readonly List<Missile> _evading = new();    // those of them it's evading
    float _sinceLook;  // seconds since it last looked behind
    bool _flatOut;     // it pulled flat out last frame, which drains its Stamina
    bool _straight;    // it's recovering straight and level rather than in its pattern

    public override void _Ready() => _jet = GetParent<Jet>();

    /// <summary>Where it puts the controls for a frame <paramref name="seconds"/> long. Its jet asks once a frame, before moving.</summary>
    public FlightInput Fly(float seconds)
    {
        var fromJet = _jet.GlobalBasis.Inverse();
        var sky = fromJet * Vector3.Up;
        var skill = Skills[(int)Skill];
        var hostile = Chasing ? Unit.Hostiles(GetTree(), _jet.Team).OfType<Jet>().MinBy(DistanceTo) : null;
        Watch(seconds);
        Choose(hostile, seconds);
        var target = Maneuver == Maneuver.AttackRun ? hostile : null;
        var evading = Maneuver == Maneuver.Evade;
        var recovering = Maneuver == Maneuver.Recover;
        if (target != null)
            _seenSpeed = target == _seen ? Mathf.Lerp(_seenSpeed, target.Speed, 1 - Mathf.Exp(-seconds / skill.SpeedReaction)) : target.Speed;
        _seen = target;
        var offset = (target != null ? LeadPoint(target) : Point.GlobalPosition) - _jet.GlobalPosition;
        var throttle = evading ? 1 : _jet.Handling.ThrottleFor(target != null ? ChaseSpeed(target) : _jet.Handling.CruiseSpeed);

        // Ground ahead: roll the sky above the canopy and pull, once the canopy isn't facing the ground,
        // until the way is clear. This close to its point, that ends the pass.
        var nose = -_jet.GlobalBasis.Z;
        if (GroundAlong(nose))
        {
            _flatOut = false;  // the pull-up costs it no Stamina
            _passing |= target == null && offset.Length() < TurnBackRange;
            return new FlightInput(Pitch: sky.Y > -0.2f ? 1 : 0, Roll: Mathf.Clamp(Mathf.Atan2(sky.X, sky.Y) * RollGain, -1, 1), Yaw: 0, throttle, Squeeze: 0);
        }

        // Where it heads: across the missile's path while evading; otherwise its goal, the Target's
        // lead point or its own point; level toward that while the straight way there runs through the
        // ground, so it doesn't dive at what the pull-up would only haul it back from; and level along
        // its heading while passing its point, breaking off, or recovering straight.
        _passing = target == null && (offset.Length() < PassRange || (_passing && offset.Length() < TurnBackRange));
        var level = _passing || Maneuver == Maneuver.BreakOff || (recovering && _straight);
        var heading = evading ? Across(nose) : level ? nose with { Y = 0 } : GroundAlong(offset.Normalized()) ? offset with { Y = 0 } : offset;
        var to = (fromJet * heading).Normalized();  // seen from the jet: -Z ahead, +Y above, +X right

        // A hard turn low down would swing it into ground its nose can't see yet, so without room to
        // turn it climbs straight ahead until it has some.
        if (Vector3.Forward.AngleTo(to) > Mathf.DegToRad(HardTurn) && GroundNear())
            to = (fromJet * ((nose with { Y = 0 }).Normalized() + Vector3.Up * Mathf.Tan(Mathf.DegToRad(ClimbOut)))).Normalized();

        // Flying level along its heading, the horizon is straight above or below its nose: it holds its
        // wings level and eases its nose there with the stick, however far off. Banking at it would
        // mean rolling onto its back to pull down, with no side to pick.
        var far = level ? 0 : Mathf.Min(Vector3.Forward.AngleTo(to) / Mathf.DegToRad(NearNose), 1);  // 0 on the nose, 1 from NearNose out
        var bank = Mathf.LerpAngle(Mathf.Atan2(sky.X, sky.Y), Mathf.Atan2(to.X, to.Y), far);  // roll still to go: to wings level on the nose, to the point above the canopy further out
        var squeeze = evading && HighGEvade ? 1f : 0;
        var limit = recovering ? GentleG : skill.GLimit;
        var pull = _jet.Handling.PullForG((limit - _jet.GlobalBasis.Y.Y) / _jet.Handling.HighGTurnScale(squeeze), _jet.Speed);  // the most stick its G limit allows: gravity's share counts, as on the debug overlay
        var pitch = Mathf.Clamp(Mathf.Atan2(to.Y, -to.Z) * PitchGain, Mathf.Max(far - 1, -pull), pull);  // pushes only near the nose
        _flatOut = !evading && !recovering && Mathf.Abs(pitch) >= FlatOut * pull;
        var coasting = !evading && Mathf.Abs(pitch) > HardPull && _jet.Speed > _jet.Handling.CornerSpeed * skill.TurnFloor;  // a hard pull costs it speed, as it costs the Player, down to its floor; an evasion keeps the throttle open
        return new FlightInput(
            pitch,
            Roll: Mathf.Clamp(bank * RollGain, -1, 1),
            Yaw: Mathf.Clamp(Mathf.Atan2(to.X, -to.Z) * YawGain, -1, 1) * (1 - far),
            Throttle: coasting || squeeze > 0 ? 0 : throttle,
            Squeeze: squeeze);
    }

    // Its Stamina and the missiles homing on it, which can cut in on any maneuver. Stamina refills, or
    // drains while it pulls flat out, and holds through an evasion. Each missile is judged once, the
    // first time it sees it: ahead of its nose, or anywhere on the frame it looks behind. Within
    // SalvoTime of an evasion starting the missile joins it; otherwise its Skill's chance decides, and
    // a pass costs a share of Stamina and starts one. The evasion ends when no missile it's evading
    // still homes on it. With no Stamina left it recovers, until a share is back.
    void Watch(float seconds)
    {
        var skill = Skills[(int)Skill];
        var share = 1 / skill.Evasions;
        if (Maneuver != Maneuver.Evade)
            Stamina = Mathf.Clamp(Stamina + (_flatOut ? -seconds / FlatOutTime : seconds * share / skill.Regain), 0, 1);

        _sinceLook += seconds;
        var looking = _sinceLook >= skill.Look;
        if (looking)
            _sinceLook = 0;
        _evading.RemoveAll(missile => !IsInstanceValid(missile) || missile.Target != _jet);
        var homing = false;
        foreach (var missile in Missile.Homing(GetTree(), _jet))
        {
            homing = true;
            if ((!looking && Behind(missile)) || !_judged.Add(missile))
                continue;
            if (Maneuver != Maneuver.Evade || _since >= SalvoTime)
            {
                if (Maneuver == Maneuver.Recover || Stamina <= 0 || GD.Randf() >= skill.Dodge)
                    continue;
                Stamina = Mathf.Max(Stamina - share, 0);
                Become(Maneuver.Evade);
            }
            _evading.Add(missile);
        }
        if (!homing)
            _judged.Clear();

        if (Maneuver == Maneuver.Evade && _evading.Count > 0)
            return;
        if (Stamina <= 0 && Maneuver != Maneuver.Recover)
        {
            Become(Maneuver.Recover);
            _straight = GD.Randf() < 0.5f;
        }
        else if (Maneuver == Maneuver.Evade || (Maneuver == Maneuver.Recover && Stamina >= share))
            Become(Maneuver.Pattern);
    }

    void Become(Maneuver next)
    {
        Maneuver = next;
        _since = 0;
    }

    // Level and square across the path of the nearest missile it's evading, to the side its nose already leans.
    Vector3 Across(Vector3 nose)
    {
        var side = (_jet.GlobalPosition - _evading.MinBy(DistanceTo).GlobalPosition).Cross(Vector3.Up);
        return side.LengthSquared() < 1 ? nose with { Y = 0 } : side.Dot(nose) < 0 ? -side : side;
    }

    // Moves it on to its next maneuver once this one is over: from its pattern to an attack run on the
    // hostile jet, from the run to a break-off, and from that back to its pattern.
    void Choose(Jet hostile, float seconds)
    {
        var skill = Skills[(int)Skill];
        var turnsBack = Skill >= Skill.Veteran;  // on a hostile jet behind it; a Rookie or Regular leaves it there
        _since += seconds;
        _inSights = Maneuver == Maneuver.AttackRun && hostile != null && Targeting.InSeeker(_jet, hostile) ? _inSights + seconds : 0;
        var next = Maneuver switch
        {
            Maneuver.Pattern when hostile != null && _since >= skill.Wait && (turnsBack || !Behind(hostile)) => Maneuver.AttackRun,
            Maneuver.AttackRun when hostile == null => Maneuver.Pattern,
            Maneuver.AttackRun when _inSights >= Targeting.Seeker.LockTime || _since >= RunLimit || (!turnsBack && Behind(hostile)) => Maneuver.BreakOff,
            Maneuver.BreakOff when _since >= skill.BreakOff => Maneuver.Pattern,
            _ => Maneuver,
        };
        if (next != Maneuver)
            Become(next);
    }

    // True for a jet or a missile in the half of the sky behind the jet's tail.
    bool Behind(Node3D other) => (other.GlobalPosition - _jet.GlobalPosition).Dot(_jet.GlobalBasis.Z) > 0;

    float DistanceTo(Node3D other) => _jet.GlobalPosition.DistanceTo(other.GlobalPosition);

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
    // Target's own as far as it has noticed, faster the further back it is and slower when it's too close.
    float ChaseSpeed(Jet target) => Mathf.Clamp(
        _seenSpeed + (DistanceTo(target) - TrailDistance) * CloseRate,
        _jet.Handling.StallSpeed + StallMargin,
        _jet.Handling.TopSpeed);

    // True when a ball Clearance wide, swept from the jet along a direction as far as it flies in
    // LookAhead, meets the ground, or already touches it. The sweep alone ignores ground the ball
    // starts inside, so it went blind just when the jet was closest to the ground.
    bool GroundAlong(Vector3 direction)
    {
        _alongPath.Transform = _jet.GlobalTransform;
        _alongPath.Motion = direction * _jet.Speed / 3.6f * LookAhead;
        var space = _jet.GetWorld3D().DirectSpaceState;
        if (space.CastMotion(_alongPath)[1] < 1)
            return true;
        using var touching = space.GetRestInfo(_alongPath);  // empty when the ball touches nothing
        return touching.Count > 0;
    }

    // True when the ground is within TurnRoom of the jet, in any direction.
    bool GroundNear()
    {
        _around.Transform = _jet.GlobalTransform;
        using var touching = _jet.GetWorld3D().DirectSpaceState.GetRestInfo(_around);  // empty when the ball touches nothing
        return touching.Count > 0;
    }
}
