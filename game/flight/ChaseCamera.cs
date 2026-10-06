using Godot;

/// <summary>
/// Follows the jet from behind, turning after it with the smoothing set by the Feel Settings' camera
/// lag: the jet visibly rolls and pitches on screen while the view catches up, but stays the same
/// distance away. Where the camera sits relative to the jet in the scene sets how far behind and above
/// it flies.
/// </summary>
public partial class ChaseCamera : Camera3D
{
    /// <summary>The jet to follow.</summary>
    [Export] public Jet Jet { get; set; }

    Transform3D _offset;  // the camera relative to the jet, as placed in the scene
    Basis _turn;          // the jet's orientation as the camera follows it, a little behind

    public override void _Ready()
    {
        _offset = Jet.GlobalTransform.AffineInverse() * GlobalTransform;
        _turn = Jet.GlobalBasis;
    }

    // Runs after the jet's own _Process, since the camera comes after the jet in the scene.
    public override void _Process(double delta)
    {
        _turn = _turn.Slerp(Jet.GlobalBasis, Packs.Base.Feel.CameraCatchUp((float)delta));
        GlobalTransform = new Transform3D(_turn, Jet.GlobalPosition) * _offset;
    }
}
