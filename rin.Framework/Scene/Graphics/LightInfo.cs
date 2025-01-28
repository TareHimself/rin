using System.Runtime.InteropServices;
using rin.Framework.Core.Math;

namespace rin.Framework.Scene.Graphics;

/// <summary>
/// Packed info about a light in the scene
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LightInfo
{
    // float4 locationRadius;
    // float4 directionType;
    // float4 colorRadiance;
    private Vec4<float> _locationRadius = new Vec4<float>();
    private Vec4<float> _directionType = new Vec4<float>();
    private Vec4<float> _colorRadiance = new Vec4<float>();

    public LightInfo()
    {
    }

    public required Vec3<float> Location
    {
        get
        {
            var (x, y, z,w) = _locationRadius;
            return new Vec3<float>(x, y, z);
        }
        set
        {
            _locationRadius.X = value.X;
            _locationRadius.Y = value.Y;
            _locationRadius.Z = value.Z;
        }
    }

    public required Vec3<float> Direction  {
        get
        {
            var (x, y, z,w) = _directionType;
            return new Vec3<float>(x, y, z);
        }
        set
        {
            _directionType.X = value.X;
            _directionType.Y = value.Y;
            _directionType.Z = value.Z;
        }
    }
    public required Vec3<float> Color {
        get
        {
            var (x, y, z,w) = _colorRadiance;
            return new Vec3<float>(x, y, z);
        }
        set
        {
            _colorRadiance.X = value.X;
            _colorRadiance.Y = value.Y;
            _colorRadiance.Z = value.Z;
        }
    }

    public required float Radiance
    {
        get => _colorRadiance.W;
        set => _colorRadiance.W = value;
    }
    public required LightType LightType
    {
        get => (LightType)_directionType.W;
        set => _directionType.W = (float)value;
    }
}