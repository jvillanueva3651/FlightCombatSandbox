using Godot;

/// <summary>
/// Follows the jet from behind, turning after it with the smoothing set by the Feel Settings' camera
/// lag: the jet visibly rolls and pitches on screen while the view catches up, but stays the same
/// distance away. Where the camera sits relative to the jet in the scene sets how far behind and above
/// it flies. The view widens with speed and shakes in high-G turns and stalls, by the Feel Settings'
/// amounts, and the right stick swings the camera around the jet. R3 switches to a view from the jet's
/// nose, fixed to the jet with the jet hidden, and back.
/// </summary>
public partial class ChaseCamera : Camera3D
{
    /// <summary>The jet to follow.</summary>
    [Export] public Jet Jet { get; set; }

    // How far the right stick swings the view, in degrees: all the way behind to either side, and just
    // short of straight up or down, where the view would spin.
    const float MaxLookSideways = 180, MaxLookUpDown = 85;

    Transform3D _offset;  // the camera relative to the jet, as placed in the scene
    Transform3D _nose;    // the nose view relative to the jet: the jet's NoseView marker
    bool _noseView;       // looking from the nose instead of chasing the jet
    bool _flareChord;     // L3 joined R3 for flares since R3 went down, so letting go of R3 keeps the view
    Basis _turn;          // the jet's orientation as the camera follows it, a little behind
    Vector2 _look;        // degrees the right stick has swung the view: to the left (X) and up (Y)
    float _fov;           // the field of view set in the scene, kept at cruise speed
    float _time;

    // Smooth randomness for the shake, changing about 15 times a second.
    readonly FastNoiseLite _noise = new() { Frequency = 15, FractalType = FastNoiseLite.FractalTypeEnum.None };

    public override void _Ready()
    {
        _offset = Jet.GlobalTransform.AffineInverse() * GlobalTransform;
        _nose = Jet.GetNode<Node3D>("NoseView").Transform;
        _turn = Jet.GlobalBasis;
        _fov = Fov;
    }

    // Runs after the jet's own _Process, since the camera comes after the jet in the scene.
    public override void _Process(double delta)
    {
        var feel = Packs.Base.Feel;
        var dt = (float)delta;
        var catchUp = feel.CameraCatchUp(dt);
        // Orthonormalized stops rounding errors building up until Slerp refuses _turn, which froze the camera.
        _turn = _turn.Slerp(Jet.GlobalBasis, catchUp).Orthonormalized();

        // The right stick swings the camera around the jet, as far as it's pushed; let go and the
        // view eases back behind the jet.
        var stick = new Vector2(-Input.GetAxis(Controls.LookLeft, Controls.LookRight), Input.GetAxis(Controls.LookDown, Controls.LookUp));
        _look = _look.Lerp(stick * new Vector2(MaxLookSideways, MaxLookUpDown), catchUp);
        var look = new Transform3D(new Basis(Vector3.Up, Mathf.DegToRad(_look.X)) * new Basis(Vector3.Right, Mathf.DegToRad(_look.Y)), Vector3.Zero);

        // R3 changes the view when it's let go, unless L3 joined it for flares while it was down.
        if (Input.IsActionJustPressed(Controls.ChangeView))
            _flareChord = false;
        _flareChord |= Controls.IsFlarePressed();
        if (Input.IsActionJustReleased(Controls.ChangeView) && !_flareChord)
            _noseView = !_noseView;
        Jet.Visible = !_noseView;

        // The chase view swings around the jet; the nose view turns in place, like a head.
        GlobalTransform = _noseView
            ? Jet.GlobalTransform * _nose * look
            : new Transform3D(_turn, Jet.GlobalPosition) * look * _offset;
        Fov = _fov + feel.ExtraFieldOfView(Jet.Handling, Jet.Speed);

        // The shake turns the view up to its amount up and down and side to side, on top of the follow,
        // which is set afresh every frame, so it never builds up.
        _time += dt;
        var shake = Mathf.DegToRad(feel.Shake(Jet.HighG, Jet.Handling.StallDepth(Jet.Speed)));
        RotateObjectLocal(Vector3.Right, shake * _noise.GetNoise2D(_time, 0));
        RotateObjectLocal(Vector3.Up, shake * _noise.GetNoise2D(_time, 100));
    }
}
