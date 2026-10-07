namespace Core;

/// <summary>
/// What kind of weapon something is: how much damage it deals, how fast it flies and turns, and how
/// its seeker locks on. The damage goes to each hostile Unit within the blast radius, in meters, of
/// where it explodes; it explodes when a hostile Unit comes that close, or after its lifetime in
/// seconds. Its speed is in km/h, reached from the launching jet's speed at the acceleration in km/h
/// per second; the turn rate is in degrees per second. A Target locks once it has stayed within the
/// lock range, in meters, and the seeker cone, in degrees either side of the nose, for the lock time
/// in seconds; in flight, a Target that slips outside the seeker cone is lost.
/// </summary>
public sealed record WeaponType
{
    public required int Damage { get; init; }
    public required float Speed { get; init; }
    public required float Acceleration { get; init; }
    public required float TurnRate { get; init; }
    public required float Lifetime { get; init; }
    public required float BlastRadius { get; init; }
    public required float LockRange { get; init; }
    public required float SeekerCone { get; init; }
    public required float LockTime { get; init; }

    public static WeaponType FromJson(string json) => PackLoader.Read<WeaponType>(json);
}
