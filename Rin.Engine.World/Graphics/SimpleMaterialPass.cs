using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.World.Graphics.Default;

namespace Rin.Engine.World.Graphics;

/// <summary>
///     Interface for a material pass
/// </summary>
public abstract class SimpleMaterialPass : IMaterialPass
{
    public abstract IShader Shader { get; }
    public abstract ulong GetRequiredMemory();

    /// <summary>
    ///     Execute this pass for all <see cref="meshes" />. All <see cref="meshes" /> use the same index buffer, and the same
    ///     material
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="data">
    ///     A buffer containing the data written by all instances of this pass will be size of
    ///     <see cref="GetRequiredMemory" /> * <see cref="meshes" />
    /// </param>
    /// <param name="meshes">The meshes to draw</param>
    public void Execute(WorldFrame frame, IDeviceBufferView? data, ProcessedMesh[] meshes)
    {
        var requiredMemorySize = GetRequiredMemory();
        var ctx = frame.ExecutionContext;
        if (requiredMemorySize > 0 && data == null) throw new Exception("Missing buffer");
        if (Shader.Bind(ctx)) ExecuteBatch(Shader, frame, data, meshes);
    }

    public abstract void Write(IDeviceBufferView view, ProcessedMesh mesh);
    public abstract bool BindAndPush(WorldFrame frame, IDeviceBufferView? groupMaterialBuffer);

    protected abstract IMaterialPass GetPass(ProcessedMesh mesh);

    /// <summary>
    ///     Execute this pass for all <see cref="meshes" />. The index buffer and shader are already bound
    /// </summary>
    /// <param name="shader">The bound shader</param>
    /// <param name="frame"></param>
    /// <param name="data"></param>
    /// <param name="meshes"></param>
    /// <returns>The total memory used</returns>
    protected abstract ulong ExecuteBatch(IShader shader, WorldFrame frame, IDeviceBufferView? data,
        ProcessedMesh[] meshes);
}