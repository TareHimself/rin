using System.Numerics;
using Rin.World.Graphics.Default.Shaders;

namespace Rin.World.Graphics.ImageBasedLighting;

public struct EnvironmentVolume
{
    public required DeviceEnvironmentMap Map;
    public required float Intensity;
    public required Matrix4x4 Transform;
    public Vector3 Extent;
    public float BlendDistance;
    public int Priority;

    public readonly bool IsUnbounded => Extent == Vector3.Zero;

    public readonly Vector3 Center => Transform.Translation;

    public readonly float BoundingRadius
    {
        get
        {
            var corner = Vector3.Abs(Vector3.TransformNormal(new Vector3(Extent.X, 0f, 0f), Transform))
                         + Vector3.Abs(Vector3.TransformNormal(new Vector3(0f, Extent.Y, 0f), Transform))
                         + Vector3.Abs(Vector3.TransformNormal(new Vector3(0f, 0f, Extent.Z), Transform));
            return corner.Length();
        }
    }

    public readonly EnvironmentVolumeInfo ToInfo()
    {
        Matrix4x4.Invert(Transform, out var worldToLocal);
        return new EnvironmentVolumeInfo
        {
            WorldToLocal = worldToLocal,
            LocalToWorld = Transform,
            Extent = Extent,
            BlendDistance = BlendDistance,
            Irradiance = Map.Irradiance.Scaled(Intensity).ToCoefficients(),
            Reflection = ToReflection(),
            Priority = Priority
        };
    }

    public readonly EnvironmentReflection ToReflection()
    {
        return new EnvironmentReflection
        {
            Cubemap = Map.Cubemap,
            MaxMip = Map.MipCount - 1f,
            Intensity = Intensity
        };
    }

    /// <summary>
    ///     How much this volume applies at a world position: 0 outside its box, rising to 1 once
    ///     <see cref="BlendDistance" /> inside the border. Always 1 when unbounded.
    /// </summary>
    public readonly float WeightAt(Vector3 position)
    {
        if (IsUnbounded) return 1f;
        if (!Matrix4x4.Invert(Transform, out var toLocal)) return 0f;

        return EnvironmentLighting.BoxWeight(Vector3.Transform(position, toLocal), Extent, BlendDistance);
    }
}
