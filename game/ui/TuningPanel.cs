using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Core;
using Godot;

/// <summary>
/// A developer panel, toggled with F1, with a slider for every Handling value of the jet's Unit Type
/// on one tab and every Feel Setting on another. Moving a slider changes the value at once,
/// mid-flight. Save writes the values into the base Pack's files and Reset reads them back from
/// there. Its process mode is Always, so it also works while the game is paused.
/// </summary>
public partial class TuningPanel : CanvasLayer
{
    /// <summary>The jet whose Unit Type's Handling the sliders change.</summary>
    [Export] public Jet Jet { get; set; }

    readonly List<Action<Pack>> _resets = new();  // one per slider: takes its value from a freshly read Pack
    Label _status;

    public override void _Ready()
    {
        AddSliders(GetNode<GridContainer>("Panel/Box/Tabs/Handling"), pack => pack.UnitTypes[Jet.UnitTypeId].Handling);
        AddSliders(GetNode<GridContainer>("Panel/Box/Tabs/Feel Settings"), pack => pack.Feel);
        _status = GetNode<Label>("Panel/Box/Status");
        GetNode<Button>("Panel/Box/Buttons/Save").Pressed += Save;
        GetNode<Button>("Panel/Box/Buttons/Reset").Pressed += Reset;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!e.IsActionPressed(Controls.TuningPanel))
            return;
        Visible = !Visible;
        GetViewport().SetInputAsHandled();
    }

    // Both show what went wrong on the panel, not just in Godot's log, so a failure can't pass for success.
    void Save()
    {
        try
        {
            PackLoader.Save(Packs.Base, Packs.BaseFolder());
            _status.Text = "Saved.";
        }
        catch (Exception e)
        {
            _status.Text = $"Not saved: {e.Message}";
        }
    }

    void Reset()
    {
        try
        {
            var saved = PackLoader.Load(Packs.BaseFolder());  // reads everything before changing anything
            foreach (var reset in _resets)
                reset(saved);
            Controls.SetDeadZone(Packs.Base.Feel.DeadZone);
            _status.Text = "Reset to the saved values.";
        }
        catch (Exception e)
        {
            _status.Text = $"Not reset: {e.Message}";  // a typo in a hand-edited file, say
        }
    }

    // A row for each number in the picked values: its name as in its file, a slider, and the number itself.
    void AddSliders(GridContainer rows, Func<Pack, object> pick)
    {
        var values = pick(Packs.Base);
        foreach (var property in values.GetType().GetProperties())
        {
            var start = (float)property.GetValue(values);
            // ponytail: each slider spans 0 to twice its starting value; give each value its own range if that's too narrow.
            var max = Mathf.Max(2 * start, 1);
            // The step is the power of ten just below 1/200 of the range, so every value is a round number.
            var power = (int)Math.Floor(Math.Log10(max / 200.0));
            var decimals = Math.Max(0, -power);
            var shown = new Label { Text = Format(start), CustomMinimumSize = new Vector2(60, 0) };
            var slider = new HSlider
            {
                MaxValue = max,
                Step = Math.Pow(10, power),
                Value = start,
                CustomMinimumSize = new Vector2(150, 0),
                SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                FocusMode = Control.FocusModeEnum.None, // so the stick and D-pad never move a slider
            };
            slider.ValueChanged += value =>
            {
                var rounded = (float)Math.Round(value, decimals);  // without the step's leftovers, which Save would write
                property.SetValue(values, rounded);
                shown.Text = Format(rounded);
                if (property.Name == nameof(FeelSettings.DeadZone))
                    Controls.SetDeadZone(rounded);  // the controls only read it at startup
            };
            rows.AddChild(new Label { Text = JsonNamingPolicy.CamelCase.ConvertName(property.Name) }); // as in the file
            rows.AddChild(slider);
            rows.AddChild(shown);

            // Exactly the saved number, which the slider would round to its step.
            _resets.Add(saved =>
            {
                var value = (float)property.GetValue(pick(saved));
                property.SetValue(values, value);
                slider.SetValueNoSignal(value);
                shown.Text = Format(value);
            });
        }
    }

    // Every decimal, written the way the Pack's files write it, so the number shown is the number Save writes.
    static string Format(float value) => value.ToString(CultureInfo.InvariantCulture);
}
