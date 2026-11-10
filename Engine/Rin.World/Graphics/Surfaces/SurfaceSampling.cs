using System.Numerics;
using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Surfaces;

/// <summary>
///     Texture-or-constant lookups for <see cref="ISurfaceMaterial{TSelf}.Evaluate" />: an unset handle (id 0) falls
///     back to the constant.
/// </summary>
[ShadeExport]
public static class SurfaceSampling
{
    public static Vector3 ColorOr(DeviceHandle handle, Vector2 uv, Vector3 fallback)
    {
        return handle.Id > 0u
            ? BindlessData.SampleTexture(handle, uv, ImageTiling.Repeat, ImageFilter.Linear).XYZ
            : fallback;
    }

    public static float ValueOr(DeviceHandle handle, Vector2 uv, float fallback)
    {
        return handle.Id > 0u
            ? BindlessData.SampleTexture(handle, uv, ImageTiling.Repeat, ImageFilter.Linear).X
            : fallback;
    }
}
