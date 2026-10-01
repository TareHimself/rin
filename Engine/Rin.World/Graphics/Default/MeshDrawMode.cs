namespace Rin.World.Graphics.Default;

/// <summary>
///     How <see cref="DefaultRenderSystem" /> issues mesh draws.
/// </summary>
public enum MeshDrawMode
{
    /// <summary>
    ///     Indirect when the device supports it, direct draws otherwise.
    /// </summary>
    Auto,

    /// <summary>
    ///     Always use GPU-built indirect draws. Requires device support.
    /// </summary>
    Indirect,

    /// <summary>
    ///     Always issue one draw call per mesh from the CPU, with no culling.
    /// </summary>
    Direct
}
