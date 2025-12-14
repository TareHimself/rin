namespace Rin.Engine.World.Graphics;

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

    public bool IsBatchable(bool depth, IMeshMaterial other)
    {
        return GetType() == other.GetType() && (depth ? DepthPass.Shader == other.DepthPass.Shader : ColorPass.Shader == other.ColorPass.Shader);
    }
}