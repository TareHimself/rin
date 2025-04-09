using Rin.Engine.Curves;
using Rin.Engine.Math;
using Rin.Engine.World.Math;

namespace Rin.Engine.World.Skinning;

public class BoneCurve : ICurve<Transform>
{
    public string BoneName = string.Empty;

    public readonly Vector3Curve LocationCurve = new Vector3Curve();
    public readonly QuaternionCurve RotationCurve = new QuaternionCurve();
    public readonly Vector3Curve ScaleCurve = new Vector3Curve();

    public Transform Evaluate(float time)
    {
        return new Transform()
        {
            Location = LocationCurve.Evaluate(time),
            Rotation = RotationCurve.Evaluate(time),
            Scale = ScaleCurve.Evaluate(time),
        };
    }
}