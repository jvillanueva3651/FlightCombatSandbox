using System;
using Godot;

/// <summary>
/// A Unit placed in the scene. It belongs to a Team and starts with its Unit Type's health; when
/// damage takes that to zero it explodes and disappears. Its hit box, the Area3D that gun rounds
/// hit, is its direct child.
/// </summary>
public partial class Unit : Node3D
{
    /// <summary>The Unit Type this Unit is, which sets its health.</summary>
    [Export] public string UnitTypeId { get; set; } = "light-fighter";

    /// <summary>The Team this Unit belongs to; the Player's is 1.</summary>
    [Export] public int Team { get; set; } = 1;

    /// <summary>Damage the Unit can still take before it's destroyed.</summary>
    public int Health { get; private set; }

    public override void _Ready() => Health = Packs.Base.UnitTypes[UnitTypeId].Health;

    public void Damage(int amount)
    {
        if (Health == 0)
            return;  // already destroyed this frame, so a second hit can't explode it twice
        Health = Math.Max(Health - amount, 0);
        if (Health > 0)
            return;

        var explosion = GD.Load<PackedScene>("res://combat/explosion.tscn").Instantiate<Node3D>();
        GetParent().AddChild(explosion);
        explosion.GlobalPosition = GlobalPosition;
        QueueFree();
    }
}
