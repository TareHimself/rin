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
    static abstract Surface Evaluate(in TSelf material, in SurfaceInput input);

    static abstract IGraphicsDescriptor Shader { get; }

    /// <summary>
    ///     Moves a vertex in world space before it is projected. A material that overrides this must also return its
    ///     own <see cref="DepthShader" />, or the shared depth prepass would not match the color pass.
    /// </summary>
    static virtual WorldVertex ProcessVertex(in TSelf material, in WorldVertex vertex, float time) => vertex;

    /// <summary>
    ///     The furthest <see cref="ProcessVertex" /> moves a vertex, so culling bounds grow to cover it.
    /// </summary>
    static virtual float MaxVertexOffset => 0f;

    /// <summary>
    ///     A <see cref="SurfaceDepthShader{TMaterial}" /> for materials that move vertices, or null for the shared one.
    /// </summary>
    static virtual IGraphicsDescriptor? DepthShader => null;

    /// <summary>
    ///     Every texture the material samples, so the frame keeps them alive and orders the reads after their writers.
    /// </summary>
    static virtual void GetTextures(in TSelf material, ref TextureList textures)
    {
    }
}
