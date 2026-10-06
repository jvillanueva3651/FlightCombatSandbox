using System.Text.Json.Serialization;

namespace Core;

/// <summary>A bundle of content the game loads. The base game is itself the first Pack.</summary>
public sealed class Pack
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }

    /// <summary>Unit Types by id, which is the file name in the Pack's unit-types folder.</summary>
    [JsonIgnore] public Dictionary<string, UnitType> UnitTypes { get; } = new();

    /// <summary>Feel Settings shared by every aircraft, from the Pack's feel.json; set by PackLoader.</summary>
    [JsonIgnore] public FeelSettings Feel { get; internal set; } = null!;
}
