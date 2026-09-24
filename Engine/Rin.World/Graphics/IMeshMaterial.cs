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

    public MaterialIdentity GetColorIdentity()
    {
        return new MaterialIdentity(GetType(), ColorPass.Shader);
    }

    public MaterialIdentity GetDepthIdentity()
    {
        return new MaterialIdentity(GetType(), DepthPass.Shader);
    }
}

// A record struct so two identities compare by actual type/shader equality - a plain combined
// hash code can collide between unrelated materials and silently merge them into one draw batch.
public readonly record struct MaterialIdentity(Type MaterialType, IGraphicsShader Shader);