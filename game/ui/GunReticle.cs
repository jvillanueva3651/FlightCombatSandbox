using System.Linq;
using Godot;

/// <summary>
/// The gun reticle: a small circle with a dot in the middle where the gun's rounds will be at a
/// hostile's distance if the jet keeps turning as it is (<see cref="Gun.AimPoint"/>), so it stays by
/// the nose line in straight flight and drops in a pull-up, easing into each change in the turn. It
/// shows for the Target while it's within gun range; while Cross is held, and for a second after, for
/// the hostile in front within range most on the nose instead, the Target or not. Hidden otherwise,
/// while the jet is shot down, and when the spot is behind the camera.
/// </summary>
public partial class GunReticle : Control
{
    const float ShowRange = 1500;  // meters: hostiles this close get the reticle
    const float AfterFiring = 1;   // seconds it keeps following the hostile fired at after Cross is let go
    const float Radius = 12;       // pixels

    Jet _jet;
    Targeting _targeting;
    float _sinceFiring = AfterFiring;  // seconds since Cross was last held

    public override void _Ready()
    {
        _jet = ((Hud)Owner).Jet;
        _targeting = _jet.Targeting;
    }

    public override void _Process(double delta)
    {
        _sinceFiring = Input.IsActionPressed(Controls.FireGun) ? 0 : _sinceFiring + (float)delta;
        var hostile = _sinceFiring < AfterFiring
            ? Unit.Hostiles(GetTree(), _jet.Team).Where(InRange).Where(_targeting.InFront).MinBy(_targeting.OffNose)
            : null;
        hostile ??= _targeting.Target is { } target && InRange(target) ? target : null;

        var camera = GetViewport().GetCamera3D();
        var aim = hostile != null ? _jet.Gun.AimPoint(_jet.Gun.GlobalPosition.DistanceTo(hostile.GlobalPosition)) : Vector3.Zero;
        Visible = hostile != null && !_jet.ShotDown && !camera.IsPositionBehind(aim);
        if (Visible)
            Position = camera.UnprojectPosition(aim);
    }

    // Drawn around the reticle's position, which is the rounds' spot on screen.
    public override void _Draw()
    {
        DrawArc(Vector2.Zero, Radius, 0, Mathf.Tau, 32, Hud.Green, 2);
        DrawCircle(Vector2.Zero, 2, Hud.Green);
    }

    bool InRange(Unit unit) => _jet.Gun.GlobalPosition.DistanceTo(unit.GlobalPosition) <= ShowRange;
}
