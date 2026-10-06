using System.Text.Json;
using Core;
using Godot;

/// <summary>
/// A developer panel, toggled with F1, with a slider for every Handling value of the jet's Unit Type.
/// Moving a slider changes the value at once, mid-flight. Its process mode is Always, so it also
/// works while the game is paused.
/// </summary>
public partial class TuningPanel : CanvasLayer
{
    /// <summary>The jet whose Unit Type's Handling the sliders change.</summary>
    [Export] public Jet Jet { get; set; }

    public override void _Ready()
    {
        var handling = Packs.Base.UnitTypes[Jet.UnitTypeId].Handling;
        var rows = GetNode<GridContainer>("Panel/Rows");
        foreach (var property in typeof(Handling).GetProperties())
        {
            var start = (float)property.GetValue(handling);
            // ponytail: each slider spans 0 to twice its starting value; give each value its own range if that's too narrow.
            var max = Mathf.Max(2 * start, 1);
            var shown = new Label { Text = Format(start), CustomMinimumSize = new Vector2(60, 0) };
            var slider = new HSlider
            {
                MaxValue = max,
                Step = max / 200,
                Value = start,
                CustomMinimumSize = new Vector2(150, 0),
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                FocusMode = Control.FocusModeEnum.None, // so the stick and D-pad never move a slider
            };
            slider.ValueChanged += value =>
            {
                property.SetValue(handling, (float)value);
                shown.Text = Format((float)value);
            };
            rows.AddChild(new Label { Text = JsonNamingPolicy.CamelCase.ConvertName(property.Name) }); // as in the jet's file
            rows.AddChild(slider);
            rows.AddChild(shown);
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!e.IsActionPressed(Controls.TuningPanel))
            return;
        Visible = !Visible;
        GetViewport().SetInputAsHandled();
    }

    static string Format(float value) => value.ToString("0.##");
}
