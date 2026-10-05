namespace Core;

/// <summary>
/// The tunable numbers that define how an aircraft flies, stored in its Unit Type.
/// Speeds are in km/h and turn rates in degrees per second.
/// </summary>
public sealed record Handling
{
    public required float CruiseSpeed { get; set; }
    public required float PitchRate { get; set; }
    public required float RollRate { get; set; }
    public required float YawRate { get; set; }

    public static Handling FromJson(string json) => PackLoader.Read<Handling>(json);
}
