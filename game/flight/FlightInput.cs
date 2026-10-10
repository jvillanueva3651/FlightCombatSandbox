using Godot;

/// <summary>
/// Where a jet's controls are this frame, whoever is flying it: the stick's pitch and roll and L1/R1's
/// yaw from -1 to 1 (+ = nose up, right wing down, nose right), R2 against L2 from -1 to 1, and how
/// hard both triggers are squeezed together for a high-G turn, from 0 to 1.
/// </summary>
public readonly record struct FlightInput(float Pitch, float Roll, float Yaw, float Throttle, float Squeeze)
{
    /// <summary>The Player's controller, with the stick through the Feel Settings' response curve.</summary>
    public static FlightInput FromController() => new(
        Stick(Controls.PitchDown, Controls.PitchUp),
        Stick(Controls.RollLeft, Controls.RollRight),
        Stick(Controls.YawLeft, Controls.YawRight),
        Input.GetAxis(Controls.Brake, Controls.Throttle),
        Mathf.Min(Input.GetActionStrength(Controls.Brake), Input.GetActionStrength(Controls.Throttle)));

    // An axis's position after the Feel Settings' response curve.
    static float Stick(StringName negative, StringName positive) => Packs.Base.Feel.Curve(Input.GetAxis(negative, positive));
}
