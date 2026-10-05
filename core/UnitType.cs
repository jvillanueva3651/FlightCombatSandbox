namespace Core;

/// <summary>What kind of Unit something is. For now, only how it flies.</summary>
public sealed class UnitType
{
    public required Handling Handling { get; init; }
}
