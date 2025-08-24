using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Shaders;
using Rin.Engine.World.Graphics.Default;

namespace Rin.Engine.World.Graphics;

/// <summary>
///     Interface for a material pass
/// </summary>
public interface IMaterialPass
{
    public IGraphicsShader Shader { get; }

    /// <summary>
    ///     The memory required for a single draw using this pass
    /// </summary>
    /// <returns></returns>
    public ulong GetRequiredMemory();
    
    /// <summary>
    ///     Write to the <see cref="IDeviceBuffer" /> that will be the size returned from <see cref="GetRequiredMemory" />
    /// </summary>
    /// <param name="view"></param>
    /// <param name="mesh">The mesh this write is for</param>
    public void Write(in DeviceBufferView view, ProcessedMesh mesh);

    public IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer);
}