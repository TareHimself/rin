using System.Numerics;

namespace Rin.Engine.World.Graphics;

/// <summary>
/// Packed info about a light in the scene
/// </summary>

public struct LightInfo
{
    // float4 locationRadius;
    // float4 directionType;
    // float4 colorRadiance;
    private Vector4 _locationRadius;
    private Vector4 _directionType;
    private Vector4 _colorRadiance;

    public LightInfo()
    {
    }

    public required Vector3 Location
    {
        get =>  new(_locationRadius.X, _locationRadius.Y, _locationRadius.Z);
        set
        {
            _locationRadius.X = value.X;
            _locationRadius.Y = value.Y;
            _locationRadius.Z = value.Z;
        }
    }

    public required Vector3 Direction  {
        get =>  new(_directionType.X, _directionType.Y, _directionType.Z);
        set
        {
            _directionType.X = value.X;
            _directionType.Y = value.Y;
            _directionType.Z = value.Z;
        }
    }
    public required Vector3 Color {
        get => new(_colorRadiance.X, _colorRadiance.Y, _colorRadiance.Z);
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