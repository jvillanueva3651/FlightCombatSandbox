using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Godot;

/// <summary>
/// A Unit placed in the scene. It belongs to a Team and starts with its Unit Type's health; when
/// damage takes that to zero it explodes and disappears. Its hit box, the Area3D that gun rounds
/// hit, is its direct child. <see cref="All"/> lists every Unit in the scene.
/// </summary>
public partial class Unit : Node3D
{
    // The Godot node group every Unit joins so All can list them: Godot's word, not a Mission's Group.
    const string NodeGroup = "units";

    /// <summary>The Unit Type this Unit is, which sets its health.</summary>
    [Export] public string UnitTypeId { get; set; } = "light-fighter";

    /// <summary>The Team this Unit belongs to; the Player's is 1.</summary>
    [Export] public int Team { get; set; } = 1;

    /// <summary>Damage the Unit can still take before it's destroyed.</summary>
    public int Health { get; protected set; }

    /// <summary>The health it starts with: its Unit Type's.</summary>
    public int MaxHealth => UnitType.Health;

    protected UnitType UnitType => Packs.Base.UnitTypes[UnitTypeId];

    /// <summary>Every Unit in the scene, including any already at zero health.</summary>
    public static IEnumerable<Unit> All(SceneTree tree) => tree.GetNodesInGroup(NodeGroup).Cast<Unit>();

    /// <summary>
    /// The Units hostile to <paramref name="team"/> that aren't destroyed: those on every other Team,
    /// until Alliances arrive.
    /// </summary>
    public static IEnumerable<Unit> Hostiles(SceneTree tree, int team) =>
        All(tree).Where(unit => unit.Team != team && unit.Health > 0);

    public override void _Ready()
    {
        Health = UnitType.Health;
        AddToGroup(NodeGroup);
    }

    public void Damage(int amount)
    {
        if (Health == 0)
            return;  // already destroyed, so a second hit can't explode it twice
        Health = Math.Max(Health - amount, 0);
        if (Health > 0)
            return;

        Explosion.Spawn(GetParent(), GlobalPosition);
        Destroyed();
    }

    /// <summary>What happens after the Unit explodes: it disappears.</summary>
    protected virtual void Destroyed() => QueueFree();
}
