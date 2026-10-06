namespace Core;

/// <summary>
/// Settings shared by every aircraft that shape how flying feels, from the base Pack's feel.json.
/// The dead zone is a fraction of the stick's travel, the response curve runs from 0 (straight) to 1
/// (softest), and the multipliers apply on top of every aircraft's Handling (1 leaves it as it is).
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
}
