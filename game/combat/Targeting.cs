using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// The jet's current Target, among the hostile Units: those on another Team (every other Team is
/// hostile until Alliances arrive). With no Target, the nearest hostile Unit in front of the jet
/// becomes it; Triangle switches to the next one out by distance, and from the farthest back to the
/// nearest. A Target stays until it's destroyed or switched, even once the jet flies past it.
/// </summary>
public partial class Targeting : Node
{
    Jet _jet;
    Unit _target;

    /// <summary>The current Target, or null when there's none.</summary>
    public Unit Target => IsInstanceValid(_target) && _target.Health > 0 ? _target : null;

    public override void _Ready() => _jet = GetParent<Jet>();

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed(Controls.SwitchTarget))
        {
            var byDistance = Hostiles().OrderBy(Distance).ToList();
            if (byDistance.Count > 0)
                _target = byDistance[(byDistance.IndexOf(Target) + 1) % byDistance.Count];  // no Target: IndexOf is -1, so the nearest
        }
        else if (Target == null)
            _target = Hostiles().Where(InFront).MinBy(Distance);
    }

    IEnumerable<Unit> Hostiles() =>
        GetTree().GetNodesInGroup(Unit.Group).Cast<Unit>().Where(unit => unit.Team != _jet.Team && unit.Health > 0);

    float Distance(Unit unit) => _jet.GlobalPosition.DistanceTo(unit.GlobalPosition);

    // In the half of the sky the jet's nose points into.
    bool InFront(Unit unit) => (unit.GlobalPosition - _jet.GlobalPosition).Dot(-_jet.GlobalBasis.Z) > 0;
}
