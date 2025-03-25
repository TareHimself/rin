using Rin.Engine.Graphics;

namespace Rin.Engine.Scene.Graphics;
/// <summary>
/// Interface for a material pass
/// </summary>
public interface IMaterialPass
{
    /// <summary>
    /// The memory required for a single draw using this pass
    /// </summary>
    /// <returns></returns>
    public ulong GetRequiredMemory();

    /// <summary>
    /// Execute this pass for all <see cref="meshes"/>. All <see cref="meshes"/> use the same index buffer, and the same material
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="data">A buffer containing the data written by all instances of this pass will be size of <see cref="GetRequiredMemory"/> * <see cref="meshes"/></param>
    /// <param name="meshes">The meshes to draw</param>
    public void Execute(SceneFrame frame, IDeviceBufferView? data, GeometryInfo[] meshes);

    /// <summary>
    /// Write to the <see cref="IDeviceBuffer"/> that will be the size returned from <see cref="GetRequiredMemory"/>
    /// </summary>
    /// <param name="view"></param>
    /// <param name="mesh">The mesh this write is for</param>
    public void Write(IDeviceBufferView view, GeometryInfo mesh);
}