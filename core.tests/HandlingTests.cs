using System.Text.Json;

namespace Core.Tests;

public class HandlingTests
{
    [Fact]
    public void Reads_cruise_speed_and_turn_rates_from_json()
    {
        var handling = Handling.FromJson("""
            { "cruiseSpeed": 600, "pitchRate": 90, "rollRate": 180, "yawRate": 20 }
            """);

        Assert.Equal(600f, handling.CruiseSpeed);
        Assert.Equal(90f, handling.PitchRate);
        Assert.Equal(180f, handling.RollRate);
        Assert.Equal(20f, handling.YawRate);
    }

    [Fact]
    public void Rejects_json_missing_a_value()
    {
        Assert.Throws<JsonException>(() => Handling.FromJson("""
            { "cruiseSpeed": 600, "pitchRate": 90, "rollRate": 180 }
            """));
    }

    [Fact]
    public void Rejects_json_with_an_unknown_key()
    {
        Assert.Throws<JsonException>(() => Handling.FromJson("""
            { "cruiseSpeed": 600, "pitchRate": 90, "rollRate": 180, "yawRate": 20, "yawRat": 25 }
            """));
    }
}
