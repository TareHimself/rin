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
    /// The memory required to store this materials data
    /// </summary>
    /// <param name="depth"></param>
    /// <returns></returns>
    public ulong GetRequiredMemory(bool depth = false);

    /// <summary>
    /// Draw this material. <see cref="meshes"/> all use the same index buffer, the same surface and the same material
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="data">A buffer containing the data written by all instances of this material, will be size of <see cref="GetRequiredMemory"/> * <see cref="meshes"/></param>
    /// <param name="meshes">The meshes to draw</param>
    /// <param name="depth"></param>
    public void Execute(SceneFrame frame, IDeviceBufferView? data, GeometryInfo[] meshes, bool depth = false);

    /// <summary>
    /// Write to the <see cref="IDeviceBuffer"/> that will be the size returned from <see cref="GetRequiredMemory"/>
    /// </summary>
    /// <param name="view"></param>
    /// <param name="mesh">The mesh this write is for</param>
    /// <param name="depth"></param>
    public void Write(IDeviceBufferView view, GeometryInfo mesh, bool depth = false);
    
}