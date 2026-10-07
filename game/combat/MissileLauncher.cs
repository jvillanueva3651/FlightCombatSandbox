using Godot;

/// <summary>
/// Fires the jet's standard missiles from under its fuselage: each press of Circle launches one,
/// homing on the Target if it's locked and flying straight if not.
/// </summary>
public partial class MissileLauncher : Node3D
{
    /// <summary>The Weapon Type the launcher fires.</summary>
    public const string WeaponTypeId = "standard-missile";

    Jet _jet;
    Targeting _targeting;

    public override void _Ready()
    {
        _jet = GetParent<Jet>();
        _targeting = _jet.GetNode<Targeting>("Targeting");
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed(Controls.FireMissile) && !_jet.ShotDown)
            Launch();
    }

    void Launch()
    {
        var missile = GD.Load<PackedScene>("res://combat/missile.tscn").Instantiate<Missile>();
        missile.Type = Packs.Base.WeaponTypes[WeaponTypeId];
        missile.Team = _jet.Team;
        missile.Target = _targeting.Locked ? _targeting.Target : null;
        missile.Speed = _jet.Speed;
        _jet.GetParent().AddChild(missile);
        missile.GlobalTransform = GlobalTransform;
    }
}
