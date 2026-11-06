using Rin.Core.Graphics.Shaders;

namespace Rin.World.Graphics;

/// <summary>
///     Interface for all Materials
/// </summary>
public interface IMeshMaterial
{
    /// <summary>
    ///     Is this material translucent ?
    /// </summary>
    public bool Translucent { get; }

    /// <summary>
    ///     Main rendering pass
    /// </summary>
    public IMaterialPass ColorPass { get; }

    /// <summary>
    ///     The pass used for the depth pre-pass
    /// </summary>
    public IMaterialPass DepthPass { get; }

    /// <summary>
    ///     The furthest the material's vertex stage moves a vertex, so culling bounds grow to cover it.
    /// </summary>
    public float MaxVertexOffset => 0f;
}

/// <summary>
///     What makes draws batchable: the pass implementation writes and binds the data, the shader consumes it. Keyed
///     on the pass rather than the material type so materials sharing a pass (e.g. <c>MeshDepthPass.Shared</c>) batch.
/// </summary>
public readonly record struct MaterialIdentity(Type PassType, IGraphicsShader Shader)
{
    public static MaterialIdentity Of(IMaterialPass pass) => new(pass.GetType(), pass.Shader);
}