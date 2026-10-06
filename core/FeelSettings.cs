namespace Core;

/// <summary>
/// Settings shared by every aircraft that shape how flying feels, from the base Pack's feel.json.
/// The dead zone is a fraction of the stick's travel, the response curve runs from 0 (straight) to 1
/// (softest), the multipliers apply on top of every aircraft's Handling (1 leaves it as it is), the
/// camera lag is in seconds, and the speed zoom and shakes are in degrees.
/// </summary>
public sealed record FeelSettings
{
    public required float DeadZone { get; set; }
    public required float ResponseCurve { get; set; }
    public required float SpeedMultiplier { get; set; }
    public required float PitchMultiplier { get; set; }
    public required float RollMultiplier { get; set; }
    public required float YawMultiplier { get; set; }
    public required float ResponseMultiplier { get; set; }
    public required float CameraLag { get; set; }
    public required float SpeedZoom { get; set; }
    public required float HighGShake { get; set; }
    public required float StallShake { get; set; }

    public static FeelSettings FromJson(string json) => PackLoader.Read<FeelSettings>(json);

    /// <summary>
    /// <paramref name="handling"/> with the multipliers on top: the speed multiplier scales the cruise,
    /// top, minimum, stall and corner speeds but not how fast speed changes, which stays each aircraft's
    /// own; the pitch, roll and yaw multipliers scale their turn rates; the response multiplier scales
    /// the response time.
    /// </summary>
    public Handling Apply(Handling handling) => handling with
    {
        CruiseSpeed = handling.CruiseSpeed * SpeedMultiplier,
        TopSpeed = handling.TopSpeed * SpeedMultiplier,
        MinSpeed = handling.MinSpeed * SpeedMultiplier,
        StallSpeed = handling.StallSpeed * SpeedMultiplier,
        CornerSpeed = handling.CornerSpeed * SpeedMultiplier,
        PitchRate = handling.PitchRate * PitchMultiplier,
        RollRate = handling.RollRate * RollMultiplier,
        YawRate = handling.YawRate * YawMultiplier,
        ResponseTime = handling.ResponseTime * ResponseMultiplier,
    };

    /// <summary>
    /// The stick position (-1 to 1) after the response curve: a blend of a straight line and a cube, so
    /// the middle of the stick gets finer as the curve grows while small nudges still respond, and a
    /// full push stays full.
    /// </summary>
    public float Curve(float stick) => stick * (1 - ResponseCurve + ResponseCurve * stick * stick);

    /// <summary>
    /// How much of the way the chase camera turns toward the jet in <paramref name="seconds"/>: all of it
    /// with no camera lag, otherwise about two thirds in one camera lag, and the same at any frame rate.
    /// </summary>
    public float CameraCatchUp(float seconds) => 1 - MathF.Exp(-seconds / CameraLag);

    /// <summary>
    /// Degrees the camera's field of view widens at <paramref name="speed"/>: none at the aircraft's
    /// cruise speed, the speed zoom at its top speed, and narrower at the same rate below cruise speed.
    /// </summary>
    public float ExtraFieldOfView(Handling handling, float speed) =>
        SpeedZoom * (speed - handling.CruiseSpeed) / (handling.TopSpeed - handling.CruiseSpeed);

    /// <summary>
    /// Degrees the camera shakes in a high-G turn as hard as <paramref name="highG"/> and a stall as deep
    /// as <paramref name="stallDepth"/> (both 0 to 1): the high-G shake in a full high-G turn, the stall
    /// shake in a full stall, and the stronger of the two when both happen at once.
    /// </summary>
    public float Shake(float highG, float stallDepth) => Math.Max(HighGShake * highG, StallShake * stallDepth);
}
