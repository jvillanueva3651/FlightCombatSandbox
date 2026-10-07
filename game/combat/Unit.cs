using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Godot;

/// <summary>
/// A Unit placed in the scene. It belongs to a Team and starts with its Unit Type's health; when
/// damage takes that to zero it explodes and disappears. Its hit box, the Area3D that gun rounds
/// hit, is its direct child. Every Unit joins the Group group, so anything can list them all.
/// </summary>
public partial class Unit : Node3D
{
    public const string Group = "units";

    /// <summary>The Unit Type this Unit is, which sets its health.</summary>
    [Export] public string UnitTypeId { get; set; } = "light-fighter";

    /// <summary>The Team this Unit belongs to; the Player's is 1.</summary>
    [Export] public int Team { get; set; } = 1;

    /// <summary>Damage the Unit can still take before it's destroyed.</summary>
    public int Health { get; protected set; }

    /// <summary>The health it starts with: its Unit Type's.</summary>
    public int MaxHealth => UnitType.Health;

    protected UnitType UnitType => Packs.Base.UnitTypes[UnitTypeId];

    /// <summary>
    /// The Units hostile to <paramref name="team"/> that aren't destroyed: those on every other Team,
    /// until Alliances arrive.
    /// </summary>
    public static IEnumerable<Unit> Hostiles(SceneTree tree, int team) =>
        tree.GetNodesInGroup(Group).Cast<Unit>().Where(unit => unit.Team != team && unit.Health > 0);

    public override void _Ready()
    {
        Health = UnitType.Health;
        AddToGroup(Group);
    }

    public void Damage(int amount)
    {
        if (Health == 0)
            return;  // already destroyed, so a second hit can't explode it twice
        Health = Math.Max(Health - amount, 0);
        if (Health > 0)
            return;

        var explosion = GD.Load<PackedScene>("res://combat/explosion.tscn").Instantiate<Node3D>();
        GetParent().AddChild(explosion);
        explosion.GlobalPosition = GlobalPosition;
        Destroyed();
    }

    /// <summary>What happens after the Unit explodes: it disappears.</summary>
    protected virtual void Destroyed() => QueueFree();
}
