using Godot;

/// <summary>
/// Options (or Escape) pauses the game and shows Resume and Quit. Its process mode is Always,
/// so it keeps running while everything else is paused and can unpause the game.
/// </summary>
public partial class PauseMenu : CanvasLayer
{
    Button _resume;

    public override void _Ready()
    {
        _resume = GetNode<Button>("Buttons/Resume");
        _resume.Pressed += Resume;
        GetNode<Button>("Buttons/Quit").Pressed += () => GetTree().Quit();
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed(Controls.Pause))
            SetPaused(!GetTree().Paused);
        else if (GetTree().Paused && e.IsActionPressed("ui_cancel"))
            Resume(); // Circle backs out, as in PlayStation menus
        else
            return;
        GetViewport().SetInputAsHandled();
    }

    void Resume() => SetPaused(false);

    void SetPaused(bool paused)
    {
        GetTree().Paused = paused;
        Visible = paused;
        if (paused)
            _resume.GrabFocus(); // so the D-pad and Cross work without a mouse
    }
}
