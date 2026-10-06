using Godot;

/// <summary>
/// Follows the jet from behind, turning after it with the smoothing set by the Feel Settings' camera
/// lag: the jet visibly rolls and pitches on screen while the view catches up, but stays the same
/// distance away. Where the camera sits relative to the jet in the scene sets how far behind and above
/// it flies. The view widens with speed and shakes in high-G turns and stalls, by the Feel Settings'
/// amounts.
/// </summary>
public partial class ChaseCamera : Camera3D
{
    /// <summary>The jet to follow.</summary>
    [Export] public Jet Jet { get; set; }

    Transform3D _offset;  // the camera relative to the jet, as placed in the scene
    Basis _turn;          // the jet's orientation as the camera follows it, a little behind
    float _fov;           // the field of view set in the scene, kept at cruise speed
    float _time;

    // Smooth randomness for the shake, changing about 15 times a second.
    readonly FastNoiseLite _noise = new() { Frequency = 15, FractalType = FastNoiseLite.FractalTypeEnum.None };

    public override void _Ready()
    {
        _offset = Jet.GlobalTransform.AffineInverse() * GlobalTransform;
        _turn = Jet.GlobalBasis;
        _fov = Fov;
    }

    // Runs after the jet's own _Process, since the camera comes after the jet in the scene.
    public override void _Process(double delta)
    {
        var feel = Packs.Base.Feel;
        var dt = (float)delta;
        _turn = _turn.Slerp(Jet.GlobalBasis, feel.CameraCatchUp(dt));
        GlobalTransform = new Transform3D(_turn, Jet.GlobalPosition) * _offset;
        Fov = _fov + feel.ExtraFieldOfView(Jet.Handling, Jet.Speed);

        // The shake turns the view up to its amount up and down and side to side, on top of the follow,
        // which is set afresh every frame, so it never builds up.
        _time += dt;
        var shake = Mathf.DegToRad(feel.Shake(Jet.HighG, Jet.Handling.StallDepth(Jet.Speed)));
        RotateObjectLocal(Vector3.Right, shake * _noise.GetNoise2D(_time, 0));
        RotateObjectLocal(Vector3.Up, shake * _noise.GetNoise2D(_time, 100));
    }
}
