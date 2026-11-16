using System.Numerics;
using JetBrains.Annotations;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics;

/// <summary>
///     One environment cubemap as a shader reads it. An unset <see cref="Cubemap" /> (id 0) means none.
/// </summary>
[ShadeExport]
[NoReorder]
public struct EnvironmentReflection
{
    public DeviceHandle Cubemap;
    public float MaxMip;
    public float Intensity;
}

/// <summary>
///     The nine spherical-harmonic coefficients of an environment's diffuse light, already scaled by its intensity.
/// </summary>
[ShadeExport]
[NoReorder]
public struct IrradianceCoefficients
{
    public Vector3 L00;
    public Vector3 L1M1;
    public Vector3 L10;
    public Vector3 L11;
    public Vector3 L2M2;
    public Vector3 L2M1;
    public Vector3 L20;
    public Vector3 L21;
    public Vector3 L22;
}

/// <summary>
///     The environment that reaches everywhere in one view, and what the view shows behind the scene. Box volumes
///     are looked up per pixel through the view's grid instead.
/// </summary>
[ShadeExport]
[NoReorder]
public struct EnvironmentInfo
{
    public IrradianceCoefficients Irradiance;

    public EnvironmentReflection Fallback;

    /// <summary>
    ///     The box the camera is inside, which the backdrop blends towards by <see cref="VolumeWeight" />.
    /// </summary>
    public EnvironmentReflection Volume;

    public float VolumeWeight;

    /// <summary>
    ///     The table of reflectivity scale and bias by viewing angle and roughness. Unset when there is no environment.
    /// </summary>
    public DeviceHandle ReflectionLut;
}

/// <summary>
///     One box volume as a shader reads it. The box is centred on the origin of its local space, which is also the
///     point its cubemap is treated as captured from.
/// </summary>
[ShadeExport]
[NoReorder]
public struct EnvironmentVolumeInfo
{
    public Matrix4x4 WorldToLocal;
    public Matrix4x4 LocalToWorld;
    public Vector3 Extent;
    public float BlendDistance;
    public IrradianceCoefficients Irradiance;
    public EnvironmentReflection Reflection;
    public int Priority;
}
