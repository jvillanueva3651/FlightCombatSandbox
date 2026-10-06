using Core;
using Godot;

/// <summary>
/// Flies a jet straight from its Handling, with no aerodynamics: R2 and L2 set its speed, and
/// the stick and L1/R1 turn it at the Handling rates.
/// </summary>
public partial class Jet : Node3D
{
    /// <summary>The Unit Type whose Handling this jet flies with.</summary>
    [Export] public string UnitTypeId { get; set; } = "light-fighter";

    /// <summary>Current speed in km/h.</summary>
    public float Speed { get; private set; }

    /// <summary>R2 (+) against L2 (-), from -1 to 1.</summary>
    public float Throttle { get; private set; }

    /// <summary>How hard the jet is pulling, in G: 1 in level flight, more when pitching up.</summary>
    public float GLoad { get; private set; }

    Handling _handling;

    public override void _Ready()
    {
        _handling = Packs.Base.UnitTypes[UnitTypeId].Handling;
        Speed = _handling.CruiseSpeed;
    }

    public override void _Process(double delta)
    {
        var dt = (float)delta;
        var pitch = Input.GetAxis(Controls.PitchDown, Controls.PitchUp);  // + = nose up
        var roll = Input.GetAxis(Controls.RollLeft, Controls.RollRight);  // + = right wing down
        var yaw = Input.GetAxis(Controls.YawLeft, Controls.YawRight);     // + = nose right
        Throttle = Input.GetAxis(Controls.Brake, Controls.Throttle);
        Speed = _handling.NextSpeed(Speed, Throttle, dt);

        var pitchRate = Mathf.DegToRad(_handling.PitchRate * pitch);      // radians per second
        RotateObjectLocal(Vector3.Right, pitchRate * dt);
        RotateObjectLocal(Vector3.Forward, Mathf.DegToRad(_handling.RollRate * roll * dt));
        RotateObjectLocal(Vector3.Down, Mathf.DegToRad(_handling.YawRate * yaw * dt));
        Transform = Transform.Orthonormalized(); // stops rounding errors slowly skewing the jet

        var metersPerSecond = Speed / 3.6f;
        Translate(Vector3.Forward * metersPerSecond * dt);

        // Pitching bends the flight path toward the jet's top side; gravity adds its share along that axis.
        GLoad = metersPerSecond * pitchRate / 9.81f + GlobalTransform.Basis.Y.Y;
    }
}
