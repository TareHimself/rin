using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.World.Graphics.Default;

namespace Rin.World.Graphics;

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
    ///     WriteSingle to the <see cref="IDeviceBuffer" /> that will be the size returned from <see cref="GetRequiredMemory" />
    /// </summary>
    /// <param name="view"></param>
    /// <param name="mesh">The mesh this write is for</param>
    public void Write(in DeviceBufferView view, ProcessedMesh mesh);

    public IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer);

    /// <summary>
    ///     Registers and reads every graph resource this pass's material uses (e.g. textures), so they stay alive
    ///     until the frame has rendered. Called once per material per frame.
    /// </summary>
    public void DeclareResources(IGraphConfig config, ProcessedMesh mesh);
}