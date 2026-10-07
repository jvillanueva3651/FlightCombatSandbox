using System.Text.Json;
using System.Text.Json.Serialization;

namespace Core;

/// <summary>
/// Reads a Pack folder: pack.json, feel.json, one file per Unit Type in unit-types/ and one per
/// Weapon Type in weapons/. Saves the Feel Settings and Unit Types back the same way.
/// </summary>
public static class PackLoader
{
    // Pack files are edited by hand, so a misspelled key is an error rather than ignored, and Roles
    // are written as words ("antiAir"), never numbers.
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    static readonly JsonSerializerOptions SaveOptions = new(JsonOptions) { WriteIndented = true };

    public static Pack Load(string folder)
    {
        var pack = Read<Pack>(File.ReadAllText(Path.Combine(folder, "pack.json")));
        pack.Feel = Read<FeelSettings>(File.ReadAllText(Path.Combine(folder, "feel.json")));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(folder, "unit-types"), "*.json"))
            pack.UnitTypes[Path.GetFileNameWithoutExtension(file)] = Read<UnitType>(File.ReadAllText(file));
        foreach (var file in Directory.EnumerateFiles(Path.Combine(folder, "weapons"), "*.json"))
            pack.WeaponTypes[Path.GetFileNameWithoutExtension(file)] = Read<WeaponType>(File.ReadAllText(file));
        return pack;
    }

    /// <summary>
    /// Writes the Pack's Feel Settings and Unit Types into the folder's feel.json and unit-types/, shaped
    /// like the hand-written files, so saving unchanged values leaves them as they were.
    /// </summary>
    public static void Save(Pack pack, string folder)
    {
        File.WriteAllText(Path.Combine(folder, "feel.json"), Write(pack.Feel));
        foreach (var (id, unitType) in pack.UnitTypes)
            File.WriteAllText(Path.Combine(folder, "unit-types", id + ".json"), Write(unitType));
    }

    // Two-space indents and \n line endings on every system, ending with a newline.
    static string Write<T>(T value) => JsonSerializer.Serialize(value, SaveOptions).ReplaceLineEndings("\n") + "\n";

    internal static T Read<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions) ?? throw new JsonException($"{typeof(T).Name} JSON is null.");
}
