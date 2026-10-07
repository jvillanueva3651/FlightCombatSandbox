using System.Text.Json;

namespace Core.Tests;

public class UnitTypeTests
{
    // One key per line, so a test can drop or change a key with a plain Replace.
    const string LightFighterJson = $$"""
        {
          "role": "fighter",
          "health": 100,
          "points": 150,
          "handling": {{HandlingTests.LightFighterJson}}
        }
        """;

    [Fact]
    public void Reads_role_health_and_points_from_json()
    {
        var lightFighter = UnitType.FromJson(LightFighterJson);

        Assert.Equal(Role.Fighter, lightFighter.Role);
        Assert.Equal(100, lightFighter.Health);
        Assert.Equal(150, lightFighter.Points);
    }

    [Fact]
    public void Reads_a_two_word_role_in_camel_case()
    {
        Assert.Equal(Role.AntiAir, UnitType.FromJson(LightFighterJson.Replace("\"fighter\"", "\"antiAir\"")).Role);
    }

    [Fact]
    public void Rejects_json_missing_a_value()
    {
        Assert.Throws<JsonException>(() => UnitType.FromJson(LightFighterJson.Replace("\"health\": 100,", "")));
    }

    [Fact]
    public void Rejects_an_unknown_role()
    {
        Assert.Throws<JsonException>(() => UnitType.FromJson(LightFighterJson.Replace("\"fighter\"", "\"interceptor\"")));
    }

    [Fact]
    public void Rejects_a_role_given_as_a_number()
    {
        Assert.Throws<JsonException>(() => UnitType.FromJson(LightFighterJson.Replace("\"fighter\"", "0")));
    }
}
