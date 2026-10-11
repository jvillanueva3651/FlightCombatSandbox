using System.Text.Json;

namespace Core.Tests;

public class FeelSettingsTests
{
    // One key per line, so a test can drop or add a key with a plain Replace.
    const string FeelJson = """
        {
          "deadZone": 0.1,
          "responseCurve": 0.5,
          "speedMultiplier": 1.5,
          "pitchMultiplier": 2,
          "rollMultiplier": 0.5,
          "yawMultiplier": 1.25,
          "responseMultiplier": 0.75,
          "cameraLag": 0.1,
          "speedZoom": 15,
          "highGShake": 0.4,
          "stallShake": 0.8,
          "windStreakStartSpeed": 800
        }
        """;

    static readonly FeelSettings Feel = FeelSettings.FromJson(FeelJson);

    static readonly FeelSettings NoMultipliers = Feel with
    {
        SpeedMultiplier = 1, PitchMultiplier = 1, RollMultiplier = 1, YawMultiplier = 1, ResponseMultiplier = 1,
    };

    static readonly Handling LightFighter = HandlingTests.LightFighter;

    [Fact]
    public void Reads_every_value_from_json()
    {
        Assert.Equal(0.1f, Feel.DeadZone);
        Assert.Equal(0.5f, Feel.ResponseCurve);
        Assert.Equal(1.5f, Feel.SpeedMultiplier);
        Assert.Equal(2f, Feel.PitchMultiplier);
        Assert.Equal(0.5f, Feel.RollMultiplier);
        Assert.Equal(1.25f, Feel.YawMultiplier);
        Assert.Equal(0.75f, Feel.ResponseMultiplier);
        Assert.Equal(0.1f, Feel.CameraLag);
        Assert.Equal(15f, Feel.SpeedZoom);
        Assert.Equal(0.4f, Feel.HighGShake);
        Assert.Equal(0.8f, Feel.StallShake);
        Assert.Equal(800f, Feel.WindStreakStartSpeed);
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

    [Fact]
    public void Multipliers_of_one_leave_the_Handling_as_it_is()
    {
        Assert.Equal(LightFighter, NoMultipliers.Apply(LightFighter));
    }

    [Fact]
    public void The_speed_multiplier_scales_every_speed_but_not_how_fast_speed_changes()
    {
        var faster = NoMultipliers with { SpeedMultiplier = 1.5f };
        var expected = LightFighter with { CruiseSpeed = 825, TopSpeed = 1950, MinSpeed = 300, StallSpeed = 450, CornerSpeed = 750 };
        Assert.Equal(expected, faster.Apply(LightFighter));
    }

    [Fact]
    public void The_turn_multipliers_each_scale_their_own_turn_rate()
    {
        var turns = NoMultipliers with { PitchMultiplier = 2, RollMultiplier = 0.5f, YawMultiplier = 1.25f };
        var expected = LightFighter with { PitchRate = 180, RollRate = 75, YawRate = 25 };
        Assert.Equal(expected, turns.Apply(LightFighter));
    }

    [Fact]
    public void The_response_multiplier_scales_the_response_time()
    {
        var heavier = NoMultipliers with { ResponseMultiplier = 2 };
        Assert.Equal(LightFighter with { ResponseTime = 0.5f }, heavier.Apply(LightFighter));
    }

    [Fact]
    public void With_no_camera_lag_the_camera_turns_with_the_jet_at_once()
    {
        Assert.Equal(1f, (Feel with { CameraLag = 0 }).CameraCatchUp(seconds: 1 / 60f));
    }

    [Fact]
    public void The_camera_covers_about_two_thirds_of_the_way_in_one_camera_lag()
    {
        Assert.Equal(0.632, Feel.CameraCatchUp(seconds: 0.1f), precision: 3);
    }

    [Fact]
    public void The_camera_follows_the_same_way_at_any_frame_rate()
    {
        var left = 1 - Feel.CameraCatchUp(seconds: 1 / 120f);
        Assert.Equal(Feel.CameraCatchUp(seconds: 1 / 60f), 1 - left * left, precision: 5);
    }

    [Fact]
    public void The_view_widens_by_the_speed_zoom_from_cruise_to_top_speed()
    {
        Assert.Equal(0f, Feel.ExtraFieldOfView(LightFighter, speed: 550));
        Assert.Equal(15f, Feel.ExtraFieldOfView(LightFighter, speed: 1300));
    }

    [Fact]
    public void Below_cruise_speed_the_view_narrows_at_the_same_rate()
    {
        Assert.Equal(-7f, Feel.ExtraFieldOfView(LightFighter, speed: 200));
    }

    [Fact]
    public void A_full_high_G_turn_shakes_the_camera_by_the_high_G_shake()
    {
        Assert.Equal(0.4f, Feel.Shake(highG: 1, stallDepth: 0));
    }

    [Fact]
    public void A_deep_stall_shakes_the_camera_by_the_stall_shake()
    {
        Assert.Equal(0.8f, Feel.Shake(highG: 0, stallDepth: 1));
    }

    [Fact]
    public void In_a_high_G_turn_and_a_stall_at_once_the_stronger_shake_wins()
    {
        Assert.Equal(0.4f, Feel.Shake(highG: 1, stallDepth: 0.25f));
        Assert.Equal(0.8f, Feel.Shake(highG: 1, stallDepth: 1));
    }
}
