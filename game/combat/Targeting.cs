using System.Linq;
using Core;
using Godot;

/// <summary>
/// The jet's current Target, among the hostile Units: those on another Team (every other Team is
/// hostile until Alliances arrive). With no Target, the nearest hostile Unit in front of the jet
/// becomes it. A tap of Triangle switches to the next one out by distance, and from the farthest back
/// to the nearest, when it's let go; held longer, Triangle turns the camera to the Target instead. A
/// Target stays until it's destroyed or switched, even once the jet flies past it. A Target that
/// stays inside the missile seeker's cone and range for the lock time is locked; leaving the seeker,
/// switching Targets or being shot down breaks the lock.
/// </summary>
public partial class Targeting : Node
{
    const float TapTime = 0.3f;  // seconds: Triangle let go sooner switches Targets, held longer it looks at the Target

    Jet _jet;
    Unit _target;
    Unit _seeking;      // the Target the seeker was following last frame
    float _inSeeker;    // seconds it has stayed in the seeker
    float _held;        // seconds Triangle has been held down
    Unit _watched;      // the Target Triangle was held on

    /// <summary>The current Target, or null when there's none.</summary>
    public Unit Target => IsInstanceValid(_target) && _target.Health > 0 ? _target : null;

    /// <summary>
    /// The Target the camera turns to while Triangle is held, or null. Once that Target is destroyed,
    /// it stays null until Triangle is let go and held again.
    /// </summary>
    public Unit Watched => _watched != null && _watched == Target && !_jet.ShotDown ? _watched : null;

    /// <summary>True while the Target is locked on.</summary>
    public bool Locked { get; private set; }

    /// <summary>The seeker locking on: the standard missile's, until task 2.17 makes it the selected weapon's.</summary>
    public static WeaponType Seeker => Packs.Base.WeaponTypes[MissileLauncher.WeaponTypeId];

    public override void _Ready() => _jet = GetParent<Jet>();

    public override void _Process(double delta)
    {
        var hostiles = Unit.Hostiles(GetTree(), _jet.Team);
        if (Input.IsActionPressed(Controls.SwitchTarget))
        {
            _held += (float)delta;
            if (_held >= TapTime && _watched == null)
                _watched = Target;  // held long enough: the camera turns to the Target it's held on
        }
        else
        {
            if (Input.IsActionJustReleased(Controls.SwitchTarget) && _held < TapTime)
            {
                var byDistance = hostiles.OrderBy(Distance).ToList();
                if (byDistance.Count > 0)
                    _target = byDistance[(byDistance.IndexOf(Target) + 1) % byDistance.Count];  // no Target: IndexOf is -1, so the nearest
            }
            _held = 0;
            _watched = null;
        }
        if (Target == null)
            _target = hostiles.Where(InFront).MinBy(Distance);

        var target = Target;
        if (target == null || target != _seeking || _jet.ShotDown || !InSeeker(target))
            _inSeeker = 0;
        else
            _inSeeker += (float)delta;
        _seeking = target;
        Locked = _inSeeker >= Seeker.LockTime;
    }

    float Distance(Unit unit) => _jet.GlobalPosition.DistanceTo(unit.GlobalPosition);

    // In the half of the sky the jet's nose points into.
    bool InFront(Unit unit) => (unit.GlobalPosition - _jet.GlobalPosition).Dot(-_jet.GlobalBasis.Z) > 0;

    bool InSeeker(Unit unit)
    {
        var toUnit = unit.GlobalPosition - _jet.GlobalPosition;
        return toUnit.Length() <= Seeker.LockRange && toUnit.AngleTo(-_jet.GlobalBasis.Z) <= Mathf.DegToRad(Seeker.SeekerCone);
    }
}
