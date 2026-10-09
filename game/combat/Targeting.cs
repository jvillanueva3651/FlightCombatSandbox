using System.Collections.Generic;
using System.Linq;
using Core;
using Godot;

/// <summary>
/// The jet's current Target, among the hostile Units: those on another Team (every other Team is
/// hostile until Alliances arrive). Hostiles rank by how close they are to being on the jet's nose,
/// judged by both how far they sit off the nose and how far away they are. With no Target, the
/// hostile Unit in front of the jet most on its nose becomes it. A tap of Triangle, when it's let go,
/// switches to the hostile most on the nose, or the next one if that's already the Target; taps
/// soon after each other step on through the same lineup and back to the start. Held longer,
/// Triangle turns the camera to the Target instead. A Target stays until it's destroyed or switched,
/// even once the jet flies past it. A Target that stays inside the missile seeker's cone and range
/// for the lock time is locked; leaving the seeker, switching Targets or being shot down breaks the lock.
/// </summary>
public partial class Targeting : Node
{
    const float TapTime = 0.3f;  // seconds: Triangle let go sooner switches Targets, held longer it looks at the Target
    const float DegreesPerKm = 2;  // on the nose: how many degrees off the nose each km away counts as
    const float TapRun = 2;      // seconds: a tap this soon after the last steps on through the same lineup

    Jet _jet;
    Unit _target;
    List<Unit> _lineup = new();  // the hostiles in on-the-nose order, as the first of a run of taps found them
    float _sinceTap = TapRun;    // seconds since the last tap; starts as long ago
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
        _sinceTap += (float)delta;
        if (Input.IsActionPressed(Controls.SwitchTarget))
        {
            _held += (float)delta;
            if (_held >= TapTime && _watched == null)
                _watched = Target;  // held long enough: the camera turns to the Target it's held on
        }
        else
        {
            if (Input.IsActionJustReleased(Controls.SwitchTarget) && _held < TapTime)
                SwitchTarget(hostiles);
            _held = 0;
            _watched = null;
        }
        if (Target == null)
            _target = hostiles.Where(InFront).MinBy(OffNose);

        var target = Target;
        if (target == null || target != _seeking || _jet.ShotDown || !InSeeker(target))
            _inSeeker = 0;
        else
            _inSeeker += (float)delta;
        _seeking = target;
        Locked = _inSeeker >= Seeker.LockTime;
    }

    // A tap after a pause goes to the hostile most on the nose, or the next one if that's already the
    // Target: the lineup starts with the Target. Quicker taps step on through that lineup, so turning
    // between taps neither repeats nor skips a hostile; after the last one it comes back to the first.
    void SwitchTarget(IEnumerable<Unit> hostiles)
    {
        if (_sinceTap >= TapRun)
            _lineup = hostiles.OrderBy(unit => unit == Target ? -1 : OffNose(unit)).ToList();
        _sinceTap = 0;
        _lineup.RemoveAll(unit => !IsInstanceValid(unit) || unit.Health <= 0);  // destroyed since the lineup was taken
        if (_lineup.Count > 0)
            _target = _lineup[(_lineup.IndexOf(Target) + 1) % _lineup.Count];  // no Target: IndexOf is -1, so the first
    }

    /// <summary>
    /// How far a hostile is from being on the jet's nose, lowest first: degrees off the nose plus
    /// DegreesPerKm for each km away.
    /// </summary>
    public float OffNose(Unit unit)
    {
        var toUnit = unit.GlobalPosition - _jet.GlobalPosition;
        return Mathf.RadToDeg(toUnit.AngleTo(-_jet.GlobalBasis.Z)) + toUnit.Length() / 1000 * DegreesPerKm;
    }

    /// <summary>True for a Unit in the half of the sky the jet's nose points into.</summary>
    public bool InFront(Unit unit) => (unit.GlobalPosition - _jet.GlobalPosition).Dot(-_jet.GlobalBasis.Z) > 0;

    bool InSeeker(Unit unit)
    {
        var toUnit = unit.GlobalPosition - _jet.GlobalPosition;
        return toUnit.Length() <= Seeker.LockRange && toUnit.AngleTo(-_jet.GlobalBasis.Z) <= Mathf.DegToRad(Seeker.SeekerCone);
    }
}
