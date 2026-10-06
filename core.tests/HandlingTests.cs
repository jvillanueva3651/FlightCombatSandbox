using System.Text.Json;

namespace Core.Tests;

public class HandlingTests
{
    // One key per line, so a test can drop or add a key with a plain Replace.
    const string LightFighterJson = """
        {
          "cruiseSpeed": 550,
          "topSpeed": 1300,
          "throttleStrength": 100,
          "minSpeed": 200,
          "brakeStrength": 150,
          "cruiseReturnStrength": 60,
          "pitchRate": 90,
          "rollRate": 150,
          "yawRate": 20,
          "responseTime": 0.25,
          "stallSpeed": 300,
          "cornerSpeed": 500,
          "turnAtStallSpeed": 0.5,
          "turnAtTopSpeed": 0.6,
          "turnSpeedLoss": 100,
          "gravityStrength": 150
        }
        """;

    static readonly Handling LightFighter = Handling.FromJson(LightFighterJson);

    [Fact]
    public void Reads_every_value_from_json()
    {
        Assert.Equal(550f, LightFighter.CruiseSpeed);
        Assert.Equal(1300f, LightFighter.TopSpeed);
        Assert.Equal(100f, LightFighter.ThrottleStrength);
        Assert.Equal(200f, LightFighter.MinSpeed);
        Assert.Equal(150f, LightFighter.BrakeStrength);
        Assert.Equal(60f, LightFighter.CruiseReturnStrength);
        Assert.Equal(90f, LightFighter.PitchRate);
        Assert.Equal(150f, LightFighter.RollRate);
        Assert.Equal(20f, LightFighter.YawRate);
        Assert.Equal(0.25f, LightFighter.ResponseTime);
        Assert.Equal(300f, LightFighter.StallSpeed);
        Assert.Equal(500f, LightFighter.CornerSpeed);
        Assert.Equal(0.5f, LightFighter.TurnAtStallSpeed);
        Assert.Equal(0.6f, LightFighter.TurnAtTopSpeed);
        Assert.Equal(100f, LightFighter.TurnSpeedLoss);
        Assert.Equal(150f, LightFighter.GravityStrength);
    }

    [Fact]
    public void Rejects_json_missing_a_value()
    {
        Assert.Throws<JsonException>(() => Handling.FromJson(LightFighterJson.Replace("\"cruiseSpeed\": 550,", "")));
    }

    [Fact]
    public void Rejects_json_with_an_unknown_key()
    {
        Assert.Throws<JsonException>(() => Handling.FromJson(LightFighterJson.Replace("{", "{ \"yawRat\": 25,")));
    }

    [Fact]
    public void Throttle_accelerates_toward_top_speed_at_throttle_strength()
    {
        Assert.Equal(650f, LightFighter.NextSpeed(550, throttle: 1, seconds: 1));
        Assert.Equal(1300f, LightFighter.NextSpeed(1290, throttle: 1, seconds: 1));
    }

    [Fact]
    public void Brake_slows_toward_minimum_speed_at_brake_strength()
    {
        Assert.Equal(400f, LightFighter.NextSpeed(550, throttle: -1, seconds: 1));
        Assert.Equal(200f, LightFighter.NextSpeed(210, throttle: -1, seconds: 1));
    }

    [Fact]
    public void Letting_go_returns_to_cruise_speed_at_cruise_return_strength()
    {
        Assert.Equal(740f, LightFighter.NextSpeed(800, throttle: 0, seconds: 1));
        Assert.Equal(360f, LightFighter.NextSpeed(300, throttle: 0, seconds: 1));
    }

    [Fact]
    public void Half_throttle_aims_halfway_between_cruise_and_top_speed()
    {
        Assert.Equal(925f, LightFighter.NextSpeed(920, throttle: 0.5f, seconds: 1));
    }

    [Fact]
    public void Full_stick_turns_in_at_a_steady_pace_over_the_response_time()
    {
        Assert.Equal(0.5f, LightFighter.NextTurn(0, stick: 1, seconds: 0.125f));
        Assert.Equal(1f, LightFighter.NextTurn(0, stick: 1, seconds: 0.25f));
    }

    [Fact]
    public void Letting_go_of_the_stick_eases_the_turn_out_over_the_response_time()
    {
        Assert.Equal(0.5f, LightFighter.NextTurn(1, stick: 0, seconds: 0.125f));
        Assert.Equal(0f, LightFighter.NextTurn(1, stick: 0, seconds: 0.25f));
    }

    [Fact]
    public void A_longer_response_time_turns_in_more_slowly()
    {
        var heavier = LightFighter with { ResponseTime = 0.5f };
        Assert.Equal(0.25f, heavier.NextTurn(0, stick: 1, seconds: 0.125f));
    }

    [Fact]
    public void A_small_stick_movement_arrives_sooner_than_a_full_one()
    {
        Assert.Equal(0.3f, LightFighter.NextTurn(0, stick: 0.3f, seconds: 0.125f));
    }

    [Fact]
    public void Turn_rates_are_full_at_corner_speed()
    {
        Assert.Equal(1f, LightFighter.TurnRateScale(speed: 500));
    }

    [Fact]
    public void Turn_rates_weaken_in_a_straight_line_toward_stall_speed()
    {
        Assert.Equal(0.75f, LightFighter.TurnRateScale(speed: 400));
        Assert.Equal(0.5f, LightFighter.TurnRateScale(speed: 300));
    }

    [Fact]
    public void Turn_rates_weaken_in_a_straight_line_toward_top_speed()
    {
        Assert.Equal(0.8f, LightFighter.TurnRateScale(speed: 900));
        Assert.Equal(0.6f, LightFighter.TurnRateScale(speed: 1300));
    }

    [Fact]
    public void Turn_rates_hold_their_end_values_below_stall_and_above_top_speed()
    {
        Assert.Equal(0.5f, LightFighter.TurnRateScale(speed: 200));
        Assert.Equal(0.6f, LightFighter.TurnRateScale(speed: 1400));
    }

    [Fact]
    public void A_full_pull_loses_the_turn_speed_loss_each_second()
    {
        Assert.Equal(500f, LightFighter.SpeedAfterTurn(550, pitch: 1, seconds: 0.5f));
    }

    [Fact]
    public void Pushing_the_nose_down_loses_speed_too()
    {
        Assert.Equal(500f, LightFighter.SpeedAfterTurn(550, pitch: -1, seconds: 0.5f));
    }

    [Fact]
    public void A_half_pull_loses_a_quarter_as_much()
    {
        Assert.Equal(525f, LightFighter.SpeedAfterTurn(550, pitch: 0.5f, seconds: 1));
    }

    [Fact]
    public void Turning_never_slows_the_jet_below_stall_speed()
    {
        Assert.Equal(300f, LightFighter.SpeedAfterTurn(310, pitch: 1, seconds: 1));
    }

    [Fact]
    public void Turning_leaves_a_jet_braked_below_stall_speed_as_it_is()
    {
        Assert.Equal(250f, LightFighter.SpeedAfterTurn(250, pitch: 1, seconds: 1));
    }

    [Fact]
    public void Climbing_straight_up_loses_the_gravity_strength_each_second()
    {
        Assert.Equal(475f, LightFighter.SpeedAfterClimb(550, climb: 1, seconds: 0.5f));
    }

    [Fact]
    public void Diving_straight_down_gains_the_gravity_strength_each_second()
    {
        Assert.Equal(625f, LightFighter.SpeedAfterClimb(550, climb: -1, seconds: 0.5f));
    }

    [Fact]
    public void A_gentler_slope_changes_speed_less()
    {
        Assert.Equal(475f, LightFighter.SpeedAfterClimb(550, climb: 0.5f, seconds: 1));
    }

    [Fact]
    public void Climbing_never_slows_the_jet_below_stall_speed()
    {
        Assert.Equal(300f, LightFighter.SpeedAfterClimb(310, climb: 1, seconds: 1));
    }

    [Fact]
    public void Climbing_leaves_a_jet_braked_below_stall_speed_as_it_is()
    {
        Assert.Equal(250f, LightFighter.SpeedAfterClimb(250, climb: 1, seconds: 1));
    }

    [Fact]
    public void Diving_never_speeds_the_jet_past_top_speed()
    {
        Assert.Equal(1300f, LightFighter.SpeedAfterClimb(1290, climb: -1, seconds: 1));
    }
}
