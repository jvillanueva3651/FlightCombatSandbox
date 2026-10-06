namespace Core;

/// <summary>
/// The tunable numbers that define how an aircraft flies, stored in its Unit Type.
/// Speeds are in km/h, strengths and the turn speed loss in km/h per second, turn rates and the stall
/// drop rate in degrees per second, the response time in seconds, and the turn at stall and top speed
/// in fractions of the full turn rates.
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
    public required float StallSpeed { get; set; }
    public required float CornerSpeed { get; set; }
    public required float TurnAtStallSpeed { get; set; }
    public required float TurnAtTopSpeed { get; set; }
    public required float TurnSpeedLoss { get; set; }
    public required float GravityStrength { get; set; }
    public required float StallDropRate { get; set; }

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

    /// <summary>
    /// How much of its full turn rates the jet has at <paramref name="speed"/>: all of them at corner
    /// speed, falling in straight lines to the turn at stall speed and the turn at top speed, and held
    /// there below stall speed and above top speed.
    /// </summary>
    public float TurnRateScale(float speed) => speed < CornerSpeed
        ? float.Lerp(TurnAtStallSpeed, 1, Math.Clamp((speed - StallSpeed) / (CornerSpeed - StallSpeed), 0, 1))
        : float.Lerp(1, TurnAtTopSpeed, Math.Clamp((speed - CornerSpeed) / (TopSpeed - CornerSpeed), 0, 1));

    /// <summary>
    /// Speed after <paramref name="seconds"/> of pitching with the stick at <paramref name="pitch"/>
    /// (-1 to 1). A full pull either way loses the turn speed loss each second and a half pull a quarter
    /// as much, so gentle turns barely cost speed. Turning alone never slows the jet below stall speed,
    /// but leaves one already braked below it as it is.
    /// </summary>
    public float SpeedAfterTurn(float speed, float pitch, float seconds) =>
        Math.Max(speed - TurnSpeedLoss * pitch * pitch * seconds, Math.Min(speed, StallSpeed));

    /// <summary>
    /// Speed after <paramref name="seconds"/> with the nose at <paramref name="climb"/>, the sine of its
    /// angle above the horizon (1 straight up, -1 straight down). Straight up loses the gravity strength
    /// each second and straight down gains it, gentler slopes less. Climbing alone never slows the jet
    /// below stall speed, but leaves one already braked below it as it is, and diving never speeds it
    /// past top speed.
    /// </summary>
    public float SpeedAfterClimb(float speed, float climb, float seconds) =>
        Math.Clamp(speed - GravityStrength * climb * seconds, Math.Min(speed, StallSpeed), Math.Max(speed, TopSpeed));

    /// <summary>
    /// How fast the nose falls toward the ground at <paramref name="speed"/>, in degrees per second: not at
    /// all at stall speed and above, then harder the slower the jet, reaching the stall drop rate at
    /// minimum speed.
    /// </summary>
    public float NoseDropRate(float speed) =>
        StallDropRate * Math.Clamp((StallSpeed - speed) / (StallSpeed - MinSpeed), 0, 1);

    static float MoveToward(float from, float to, float maxStep) =>
        Math.Abs(to - from) <= maxStep ? to : from + Math.Sign(to - from) * maxStep;
}
