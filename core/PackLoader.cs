using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core;

/// <summary>Reads a Pack folder: pack.json, feel.json, and one file per Unit Type in unit-types/.</summary>
public static class PackLoader
{
    // Pack files are edited by hand, so a misspelled key is an error rather than ignored.
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static Pack Load(string folder)
    {
        var pack = Read<Pack>(File.ReadAllText(Path.Combine(folder, "pack.json")));
        pack.Feel = Read<FeelSettings>(File.ReadAllText(Path.Combine(folder, "feel.json")));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(folder, "unit-types"), "*.json"))
            pack.UnitTypes[Path.GetFileNameWithoutExtension(file)] = Read<UnitType>(File.ReadAllText(file));
        return pack;
    }

    internal static T Read<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new JsonException($"{typeof(T).Name} JSON is null.");
}
