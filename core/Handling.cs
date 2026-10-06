namespace Core;

/// <summary>
/// The tunable numbers that define how an aircraft flies, stored in its Unit Type.
/// Speeds are in km/h, strengths in km/h per second, turn rates in degrees per second, and the
/// response time in seconds.
/// </summary>
public sealed record Handling
{
    public required float CruiseSpeed { get; set; }
    public required float TopSpeed { get; set; }
    public required float ThrottleStrength { get; set; }
    public required float MinSpeed { get; set; }
    public required float BrakeStrength { get; set; }
    public required float CruiseReturnStrength { get; set; }
    public required float PitchRate { get; set; }
    public required float RollRate { get; set; }
    public required float YawRate { get; set; }
    public required float ResponseTime { get; set; }

    public static Handling FromJson(string json) => PackLoader.Read<Handling>(json);

    /// <summary>
    /// Speed after <paramref name="seconds"/> with the throttle (R2 +, L2 -, from -1 to 1). Full R2 aims
    /// for top speed and full L2 for minimum speed, a lighter press part of the way from cruise speed,
    /// and with neither the jet settles back to cruise speed.
    /// </summary>
    public float NextSpeed(float speed, float throttle, float seconds) => throttle switch
    {
        > 0 => MoveToward(speed, CruiseSpeed + (TopSpeed - CruiseSpeed) * throttle, ThrottleStrength * seconds),
        < 0 => MoveToward(speed, CruiseSpeed + (MinSpeed - CruiseSpeed) * -throttle, BrakeStrength * seconds),
        _ => MoveToward(speed, CruiseSpeed, CruiseReturnStrength * seconds),
    };

    /// <summary>
    /// How much of its full turn rate one axis uses after <paramref name="seconds"/>, from -1 to 1 like
    /// the stick it follows. It moves toward the stick at a steady pace that takes the response time to
    /// go from no turn to a full one, so turns ease in and out and small corrections arrive sooner.
    /// </summary>
    public float NextTurn(float turn, float stick, float seconds) => MoveToward(turn, stick, seconds / ResponseTime);

    static float MoveToward(float from, float to, float maxStep) =>
        Math.Abs(to - from) <= maxStep ? to : from + Math.Sign(to - from) * maxStep;
}
