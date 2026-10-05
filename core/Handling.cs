using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core;

/// <summary>
/// The tunable numbers that define how an aircraft flies, stored in its Unit Type.
/// Speeds are in km/h and turn rates in degrees per second.
/// </summary>
public sealed class Handling
{
    public required float CruiseSpeed { get; set; }
    public required float PitchRate { get; set; }
    public required float RollRate { get; set; }
    public required float YawRate { get; set; }

    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static Handling FromJson(string json) =>
        JsonSerializer.Deserialize<Handling>(json, JsonOptions) ?? throw new JsonException("Handling JSON is null.");
}
