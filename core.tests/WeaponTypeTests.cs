using System.Text.Json;

namespace Core.Tests;

public class WeaponTypeTests
{
    // One key per line, so a test can drop or add a key with a plain Replace.
    const string MissileJson = """
        {
          "damage": 60,
          "speed": 2500,
          "acceleration": 2000,
          "turnRate": 50,
          "lifetime": 6,
          "blastRadius": 15,
          "lockRange": 2000,
          "seekerCone": 30,
          "lockTime": 0.5
        }
        """;

    [Fact]
    public void Reads_every_value_from_json()
    {
        var missile = WeaponType.FromJson(MissileJson);

        Assert.Equal(60, missile.Damage);
        Assert.Equal(2500f, missile.Speed);
        Assert.Equal(2000f, missile.Acceleration);
        Assert.Equal(50f, missile.TurnRate);
        Assert.Equal(6f, missile.Lifetime);
        Assert.Equal(15f, missile.BlastRadius);
        Assert.Equal(2000f, missile.LockRange);
        Assert.Equal(30f, missile.SeekerCone);
        Assert.Equal(0.5f, missile.LockTime);
    }

    [Fact]
    public void Rejects_json_missing_a_value()
    {
        Assert.Throws<JsonException>(() => WeaponType.FromJson(MissileJson.Replace("\"turnRate\": 50,", "")));
    }

    [Fact]
    public void Rejects_json_with_an_unknown_key()
    {
        Assert.Throws<JsonException>(() => WeaponType.FromJson(MissileJson.Replace("{", "{ \"turnRat\": 40,")));
    }
}
