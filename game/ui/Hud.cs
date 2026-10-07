using Godot;

/// <summary>The Player's HUD. Its parts read the Player's jet from here.</summary>
public partial class Hud : CanvasLayer
{
    /// <summary>The Player's jet.</summary>
    [Export] public Jet Jet { get; set; }
}
