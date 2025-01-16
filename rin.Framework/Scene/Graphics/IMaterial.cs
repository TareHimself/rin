using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;

namespace rin.Framework.Scene.Graphics;
/// <summary>
/// Interface for all Materials
/// </summary>
public interface IMaterial
{
    /// <summary>
    /// Is this material translucent ?
    /// </summary>
    public bool Translucent { get; }
    
    /// <summary>
    /// Get the shader used for draws
    /// </summary>
    /// <returns></returns>
    public IShader GetShader();
    
    /// <summary>
    /// Get the shader used for basic draws where fragment shading will not be needed or seen i.e. shadows, depth prepass
    /// </summary>
    /// <returns></returns>
    public IShader GetDepthShader();

    /// <summary>
    /// Draw this material. <see cref="meshes"/> all use the same index buffer, the same surface and the same material
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="data">A buffer containing the data written by all instances of this material, will be size of <see cref="GetRequiredMemory"/> * <see cref="meshes"/></param>
    /// <param name="meshes">The meshes to draw</param>
    public void Execute(SceneFrame frame, IDeviceBuffer? data, GeometryInfo[] meshes);

    /// <summary>
    /// Write to the <see cref="IDeviceBuffer"/> that will be the size returned from <see cref="GetRequiredMemory"/>
    /// </summary>
    /// <param name="view"></param>
    /// <param name="mesh">The mesh this write is for</param>
    public void WriteToBuffer(IDeviceBuffer view, GeometryInfo mesh);
    
    /// <summary>
    /// The memory required by this material for per mesh execution
    /// </summary>
    /// <returns></returns>
    public ulong GetRequiredMemory();
}