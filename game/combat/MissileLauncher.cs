using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Fires the jet's standard missiles from its Hardpoints, the launcher's children. Each holds one
/// missile, shown while it's loaded. Each press of Circle fires a loaded one, the Hardpoints taking
/// turns, and the missile homes on the Target if it's locked and flies straight if not. An empty
/// Hardpoint loads again a few seconds after firing, while the count, which includes the loaded
/// missiles, still has one for it. Being shot down refills the count and every Hardpoint. Holding
/// Circle on after firing has the camera follow that missile.
/// </summary>
public partial class MissileLauncher : Node3D
{
    /// <summary>The Weapon Type the launcher fires.</summary>
    public const string WeaponTypeId = "standard-missile";

    const int FullCount = 144;   // the Ruleset's default, until task 3.38 applies each Mission's
    const float ReloadTime = 5;  // seconds from a Hardpoint firing to its next missile
    const float FollowTime = 0.3f;  // seconds Circle stays held after firing before the camera follows the missile

    /// <summary>Standard missiles left, including those loaded on the Hardpoints.</summary>
    public int Count { get; private set; }

    /// <summary>The Hardpoints, each showing its missile while loaded.</summary>
    public IReadOnlyList<Node3D> Hardpoints => _hardpoints;

    /// <summary>
    /// The missile the camera follows while Circle stays held after firing it, or null. It may have
    /// exploded since.
    /// </summary>
    public Missile Followed => _held >= FollowTime ? _fired : null;

    Jet _jet;
    Targeting _targeting;
    Node3D[] _hardpoints;  // each is loaded while it shows its missile
    float[] _reload;       // seconds until each empty Hardpoint loads again
    int _next;             // the Hardpoint whose turn it is to fire
    Missile _fired;        // the missile this press of Circle fired, if it fired one
    float _held;           // seconds Circle has been held

    public override void _Ready()
    {
        _jet = GetParent<Jet>();
        _targeting = _jet.Targeting;
        _hardpoints = GetChildren().Cast<Node3D>().ToArray();
        _reload = new float[_hardpoints.Length];
        Rearm();
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionPressed(Controls.FireMissile) && !_jet.ShotDown)
            _held += (float)delta;
        else
        {
            _held = 0;
            _fired = null;
        }

        if (_jet.ShotDown)
        {
            Rearm();  // the jet starts again with every missile, like its health
            return;
        }

        for (var i = 0; i < _hardpoints.Length; i++)
        {
            if (_hardpoints[i].Visible)
                continue;
            _reload[i] -= (float)delta;
            if (_reload[i] <= 0 && Count > Loaded)  // the count has a missile for it besides those already loaded
                _hardpoints[i].Visible = true;
        }

        if (!Input.IsActionJustPressed(Controls.FireMissile))
            return;
        for (var turn = 0; turn < _hardpoints.Length; turn++)
        {
            var i = (_next + turn) % _hardpoints.Length;
            if (!_hardpoints[i].Visible)
                continue;
            _fired = Launch(_hardpoints[i]);
            _hardpoints[i].Visible = false;
            _reload[i] = ReloadTime;
            _next = (i + 1) % _hardpoints.Length;
            Count--;
            return;
        }
    }

    /// <summary>
    /// How ready a Hardpoint is to fire, from 0 to 1: 1 while loaded, rising while it reloads, and 0
    /// when the count has no missile left for it.
    /// </summary>
    public float Readiness(int hardpoint)
    {
        if (_hardpoints[hardpoint].Visible)
            return 1;
        return Count > Loaded ? Mathf.Clamp(1 - _reload[hardpoint] / ReloadTime, 0, 1) : 0;
    }

    int Loaded => _hardpoints.Count(hardpoint => hardpoint.Visible);

    void Rearm()
    {
        Count = FullCount;
        foreach (var hardpoint in _hardpoints)
            hardpoint.Visible = true;
    }

    Missile Launch(Node3D hardpoint)
    {
        var missile = GD.Load<PackedScene>("res://combat/missile.tscn").Instantiate<Missile>();
        missile.Type = Packs.Base.WeaponTypes[WeaponTypeId];
        missile.Team = _jet.Team;
        missile.Target = _targeting.Locked ? _targeting.Target : null;
        missile.Speed = _jet.Speed;
        _jet.GetParent().AddChild(missile);
        missile.GlobalTransform = new Transform3D(_jet.GlobalBasis, hardpoint.GlobalPosition);  // the Hardpoint's own basis is turned to lay its missile along the jet
        return missile;
    }
}
