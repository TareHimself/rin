using System.Numerics;
using JetBrains.Annotations;
using Rin.Shade;

namespace Rin.World.Graphics;

/// <summary>
///     Packed info about a light in the scene
/// </summary>
[ShadeExport]
[NoReorder]
public struct LightInfo
{
    private Vector4 _locationRadius;
    private Vector4 _directionType;
    private Vector4 _colorIntensity;

    public LightInfo()
    {
    }

    public required Vector3 Location
    {
        get => new(_locationRadius.X, _locationRadius.Y, _locationRadius.Z);
        set
        {
            _locationRadius.X = value.X;
            _locationRadius.Y = value.Y;
            _locationRadius.Z = value.Z;
        }
    }

    public required float Radius
    {
        get => _locationRadius.W;
        set => _locationRadius.W = value;
    }

    public required Vector3 Direction
    {
        get => new(_directionType.X, _directionType.Y, _directionType.Z);
        set
        {
            _directionType.X = value.X;
            _directionType.Y = value.Y;
            _directionType.Z = value.Z;
        }
    }

    public required Vector3 Color
    {
        get => new(_colorIntensity.X, _colorIntensity.Y, _colorIntensity.Z);
        set
        {
            _colorIntensity.X = value.X;
            _colorIntensity.Y = value.Y;
            _colorIntensity.Z = value.Z;
        }
    }

    /// <summary>
    ///     Candela for a point light, lux for a directional light.
    /// </summary>
    public required float Intensity
    {
        get => _colorIntensity.W;
        set => _colorIntensity.W = value;
    }

    public required LightType LightType
    {
        get => (LightType)_directionType.W;
        set => _directionType.W = (float)value;
    }
}