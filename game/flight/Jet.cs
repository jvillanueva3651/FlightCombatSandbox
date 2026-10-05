using Core;
using Godot;

/// <summary>
/// Flies a jet straight from its Handling, with no aerodynamics: it always moves forward at
/// cruise speed, and the stick and L1/R1 turn it at the Handling rates.
/// </summary>
public partial class Jet : Node3D
{
    /// <summary>The Unit Type whose Handling this jet flies with.</summary>
    [Export] public string UnitTypeId { get; set; } = "light-fighter";

    Handling _handling;

    public override void _Ready() => _handling = Packs.Base.UnitTypes[UnitTypeId].Handling;

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        var pitch = Input.GetAxis(Controls.PitchDown, Controls.PitchUp);  // + = nose up
        var roll = Input.GetAxis(Controls.RollLeft, Controls.RollRight);  // + = right wing down
        var yaw = Input.GetAxis(Controls.YawLeft, Controls.YawRight);     // + = nose right

        RotateObjectLocal(Vector3.Right, Mathf.DegToRad(_handling.PitchRate * pitch * dt));
        RotateObjectLocal(Vector3.Forward, Mathf.DegToRad(_handling.RollRate * roll * dt));
        RotateObjectLocal(Vector3.Down, Mathf.DegToRad(_handling.YawRate * yaw * dt));
        Transform = Transform.Orthonormalized(); // stops rounding errors slowly skewing the jet

        Translate(Vector3.Forward * (_handling.CruiseSpeed / 3.6f) * dt); // km/h to m/s
    }
}
