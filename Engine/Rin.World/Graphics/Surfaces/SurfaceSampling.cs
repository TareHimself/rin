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
    public static Vector3 ColorOr(this BindlessData textures, DeviceHandle handle, Vector2 uv, Vector3 fallback)
    {
        return handle.Id > 0u
            ? textures.SampleTexture(handle, uv, ImageTiling.Repeat, ImageFilter.Linear).XYZ
            : fallback;
    }

    public static float ValueOr(this BindlessData textures, DeviceHandle handle, Vector2 uv, float fallback)
    {
        return handle.Id > 0u
            ? textures.SampleTexture(handle, uv, ImageTiling.Repeat, ImageFilter.Linear).X
            : fallback;
    }
}
