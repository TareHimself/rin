using System.Diagnostics;
using Rin.Framework.Graphics;
using Rin.Framework.Graphics.Shaders;
using Rin.Engine.World.Graphics.Default;

namespace Rin.Engine.World.Graphics;

/// <summary>
///     Interface for a material pass
/// </summary>
public abstract class SimpleMaterialPass : IMaterialPass
{
    public abstract IGraphicsShader Shader { get; }
    public abstract ulong GetRequiredMemory();

    public abstract void Write(in DeviceBufferView view, ProcessedMesh mesh);

    /// <summary>
    /// Bind the shader for this material, push any constants, bind any descriptors
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="groupMaterialBuffer"></param>
    /// <returns></returns>
    public abstract IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer);
    protected abstract IMaterialPass GetPass(ProcessedMesh mesh);
}