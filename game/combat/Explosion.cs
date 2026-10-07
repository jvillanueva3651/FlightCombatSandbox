using Godot;

/// <summary>
/// A placeholder explosion: a fireball that swells and fades with its light, and debris thrown out
/// in every direction that falls away. It removes itself once the last piece of debris is gone.
/// </summary>
public partial class Explosion : Node3D
{
    const float FlashTime = 0.6f;  // seconds for the fireball and its light to fade out
    const float FlashGrowth = 3;   // how many times its starting size the fireball swells to

    public override void _Ready()
    {
        var flash = GetNode("Flash");
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(flash, "scale", Vector3.One * FlashGrowth, FlashTime);
        tween.TweenProperty(flash, "transparency", 1f, FlashTime);  // the node's, not the material's, so explosions don't share a fade
        tween.TweenProperty(GetNode("Light"), "light_energy", 0f, FlashTime);

        var debris = GetNode<GpuParticles3D>("Debris");
        debris.Finished += QueueFree;
        debris.Emitting = true;
    }
}
