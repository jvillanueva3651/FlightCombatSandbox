using Godot;

/// <summary>
/// Flies its jet, its parent, to a point through the same controls the Player uses: it rolls to put
/// the point straight above the canopy and pulls toward it, the way an Ace Combat turn goes, and near
/// the nose it levels the wings and finishes with small stick and L1/R1 corrections. It leaves the
/// throttle alone, so the jet holds cruise speed. Close to the point it flies straight on through it,
/// and some way past it turns back for another pass, rather than circling it in a tight turn that
/// bleeds the jet down to stall speed.
/// </summary>
public partial class AiPilot : Node
{
    /// <summary>The point the jet flies to.</summary>
    [Export] public Node3D Point { get; set; }

    const float NearNose = 15;        // degrees off the nose inside which it eases toward wings level instead of banking at the point
    const float RollGain = 2;         // roll stick per radian of bank still to go
    const float PitchGain = 2;        // stick per radian the point sits above the nose
    const float YawGain = 3;          // L1/R1 per radian the point sits beside the nose
    const float PassRange = 150;      // meters from the point inside which it stops steering and flies straight on through it
    const float TurnBackRange = 1000; // meters from the point at which, flying on past it, it turns back

    Jet _jet;
    bool _passing;  // flying straight on through the point until TurnBackRange past it

    public override void _Ready() => _jet = GetParent<Jet>();

    /// <summary>Where it puts the controls this frame. Its jet asks once a frame, before moving.</summary>
    public FlightInput Fly()
    {
        var fromJet = _jet.GlobalBasis.Inverse();
        var offset = Point.GlobalPosition - _jet.GlobalPosition;
        _passing = offset.Length() < PassRange || (_passing && offset.Length() < TurnBackRange);
        var to = _passing ? Vector3.Forward : (fromJet * offset).Normalized();  // the point's direction seen from the jet (-Z ahead, +Y above, +X right); dead ahead while passing
        var sky = fromJet * Vector3.Up;
        var far = Mathf.Min(Vector3.Forward.AngleTo(to) / Mathf.DegToRad(NearNose), 1);  // 0 on the nose, 1 from NearNose out
        var bank = Mathf.LerpAngle(Mathf.Atan2(sky.X, sky.Y), Mathf.Atan2(to.X, to.Y), far);  // roll still to go: to wings level on the nose, to the point above the canopy further out
        return new FlightInput(
            Pitch: Mathf.Clamp(Mathf.Atan2(to.Y, -to.Z) * PitchGain, far - 1, 1),  // pushes only near the nose
            Roll: Mathf.Clamp(bank * RollGain, -1, 1),
            Yaw: Mathf.Clamp(Mathf.Atan2(to.X, -to.Z) * YawGain, -1, 1) * (1 - far),
            Throttle: 0,
            Squeeze: 0);
    }
}
