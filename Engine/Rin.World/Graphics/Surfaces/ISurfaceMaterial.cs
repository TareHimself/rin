using Rin.Core.Graphics;
using Rin.Shade;

namespace Rin.World.Graphics.Surfaces;

/// <summary>
///     A material as a plain struct: its fields are the parameters and <see cref="Evaluate" /> describes the surface.
///     Its shader is a one-line <c>[Shader]</c> class deriving <see cref="SurfaceShader{TMaterial}" />, and
///     <see cref="SurfaceMaterial{TMaterial}" /> draws it.
/// </summary>
/// <remarks>
///     Fields must be scalars, vectors, matrices, fixed-size arrays of those, or <see cref="DeviceHandle" /> textures.
/// </remarks>
[ShadeExport]
public interface ISurfaceMaterial<TSelf> where TSelf : unmanaged, ISurfaceMaterial<TSelf>
{
    static abstract Surface Evaluate(in TSelf material, in SurfaceInput input, BindlessData textures);

    static abstract IGraphicsDescriptor Shader { get; }

    /// <summary>
    ///     Every texture the material samples, so the frame keeps them alive and orders the reads after their writers.
    /// </summary>
    static virtual void GetTextures(in TSelf material, ref TextureList textures)
    {
    }
}
