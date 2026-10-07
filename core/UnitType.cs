namespace Core;

/// <summary>What kind of Unit something is: its Role, how much damage it takes, its points and how it flies.</summary>
public sealed class UnitType
{
    public required Role Role { get; init; }

    /// <summary>How much damage the Unit takes before it's destroyed.</summary>
    public required int Health { get; init; }

    /// <summary>What destroying the Unit adds to the score.</summary>
    public required int Points { get; init; }

    public required Handling Handling { get; init; }

    public static UnitType FromJson(string json) => PackLoader.Read<UnitType>(json);
}

/// <summary>
/// The job a Unit Type is built for. New Roles arrive with the Unit Types that need them.
/// </summary>
public enum Role { Fighter, Multirole, Attacker, Bomber, Armor, AntiAir }
