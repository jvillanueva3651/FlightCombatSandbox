using System.Text.Json;

namespace Core.Tests;

public class FeelSettingsTests
{
    // One key per line, so a test can drop or add a key with a plain Replace.
    const string FeelJson = """
        {
          "deadZone": 0.1,
          "responseCurve": 0.5
        }
        """;

    static readonly FeelSettings Feel = FeelSettings.FromJson(FeelJson);

    [Fact]
    public void Reads_every_value_from_json()
    {
        Assert.Equal(0.1f, Feel.DeadZone);
        Assert.Equal(0.5f, Feel.ResponseCurve);
    }

    [Fact]
    public void Rejects_json_missing_a_value()
    {
        Assert.Throws<JsonException>(() => FeelSettings.FromJson(FeelJson.Replace("\"deadZone\": 0.1,", "")));
    }

    [Fact]
    public void Rejects_json_with_an_unknown_key()
    {
        Assert.Throws<JsonException>(() => FeelSettings.FromJson(FeelJson.Replace("{", "{ \"deadZones\": 0.2,")));
    }

    [Fact]
    public void A_straight_curve_leaves_the_stick_as_it_is()
    {
        var straight = Feel with { ResponseCurve = 0 };
        Assert.Equal(0.5f, straight.Curve(0.5f));
    }

    [Fact]
    public void The_softest_curve_turns_half_stick_into_an_eighth()
    {
        var softest = Feel with { ResponseCurve = 1 };
        Assert.Equal(0.125f, softest.Curve(0.5f));
    }

    [Fact]
    public void A_full_push_stays_full_whatever_the_curve()
    {
        Assert.Equal(1f, Feel.Curve(1));
        Assert.Equal(-1f, (Feel with { ResponseCurve = 1 }).Curve(-1));
    }

    [Fact]
    public void A_half_curve_lands_between_the_straight_line_and_the_cube()
    {
        Assert.Equal(0.3125f, Feel.Curve(0.5f));
        Assert.Equal(-0.3125f, Feel.Curve(-0.5f));
    }
}
