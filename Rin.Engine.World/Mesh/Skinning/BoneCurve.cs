using System.Numerics;
using Rin.Engine.Curves;
using Rin.Engine.Math;

namespace Rin.Engine.World.Mesh.Skinning;

public class BoneCurve : ICurve<Transform>
{
    public readonly Vector3Curve PositionCurve = new();
    public readonly QuaternionCurve RotationCurve = new();
    public readonly Vector3Curve ScaleCurve = new();
    public string BoneName = string.Empty;

    public Transform Sample(float time)
    {
        return new Transform
        {
            Position = PositionCurve.Sample(time),
            Orientation = RotationCurve.Sample(time),
            Scale = ScaleCurve.Sample(time)
        };
    }
    
    public void AddPositionLinear(float time, in Vector3 value) => PositionCurve.AddLinear(time, value);
    public void AddPositionStep(float time, in Vector3 value)  => PositionCurve.AddStep(time, value);
    public void AddPositionCubic(float time, in Vector3 value,float tangent)  => PositionCurve.AddCubic(time, value, tangent);
    
    public void AddRotationLinear(float time, in Quaternion value) => RotationCurve.AddLinear(time, value);
    public void AddRotationStep(float time, in Quaternion value)  => RotationCurve.AddStep(time, value);
    public void AddRotationCubic(float time, in Quaternion value,float tangent)  => RotationCurve.AddCubic(time, value, tangent);
    
    public void AddScaleLinear(float time, in Vector3 value) => ScaleCurve.AddLinear(time, value);
    public void AddScaleStep(float time, in Vector3 value)  => ScaleCurve.AddStep(time, value);
    public void AddScaleCubic(float time, in Vector3 value,float tangent)  => ScaleCurve.AddCubic(time, value, tangent);
    
    public void AddLinear(float time, in Transform value)
    {
        AddPositionLinear(time,value.Position);
        AddRotationLinear(time,value.Orientation);
        AddScaleLinear(time,value.Scale);
    }
    
    public void AddStep(float time, in Transform value)
    {
        AddPositionStep(time,value.Position);
        AddRotationStep(time,value.Orientation);
        AddScaleStep(time,value.Scale);
    }
    
    public void AddCubic(float time, in Transform value,float tangent)
    {
        AddPositionCubic(time,value.Position,tangent);
        AddRotationCubic(time,value.Orientation,tangent);
        AddScaleCubic(time,value.Scale,tangent);
    }
}