using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Framework.Scene.Graphics;
/// <summary>
/// Interface for a material pass
/// </summary>
public abstract class SimpleMaterialPass : IMaterialPass
{
    public abstract ulong GetRequiredMemory();

    protected abstract IShader Shader { get; }

    protected abstract IMaterialPass GetPass(GeometryInfo mesh);

    /// <summary>
    /// Execute this pass for all <see cref="meshes"/>. The index buffer and shader are already bound
    /// </summary>
    /// <param name="shader">The bound shader</param>
    /// <param name="frame"></param>
    /// <param name="data"></param>
    /// <param name="meshes"></param>
    /// <returns>The total memory used</returns>
    protected abstract ulong ExecuteBatch(IShader shader,SceneFrame frame, IDeviceBufferView? data, GeometryInfo[] meshes);
    
    /// <summary>
    /// Execute this pass for all <see cref="meshes"/>. All <see cref="meshes"/> use the same index buffer, and the same material
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="data">A buffer containing the data written by all instances of this pass will be size of <see cref="GetRequiredMemory"/> * <see cref="meshes"/></param>
    /// <param name="meshes">The meshes to draw</param>
    public void Execute(SceneFrame frame, IDeviceBufferView? data, GeometryInfo[] meshes)
    {
        var requiredMemorySize = GetRequiredMemory();
        var cmd = frame.GetCommandBuffer();
        var first = meshes.First();
        if (requiredMemorySize > 0 && data == null) throw new Exception("Missing buffer");
        if (Shader.Bind(cmd))
        {
            vkCmdBindIndexBuffer(cmd, first.Geometry.IndexBuffer.NativeBuffer, 0, VkIndexType.VK_INDEX_TYPE_UINT32);

            ulong offset = 0;
            foreach (var groupedMeshes in meshes.GroupBy(c => new
                     {
                         c.Surface.StartIndex,
                         c.Surface.Count
                     }))
            {
                var groupArray = groupedMeshes.ToArray();

                offset += ExecuteBatch(Shader, frame, data!.GetView(offset, data.Size - offset), groupArray);
            }
        }
    }

    public abstract void Write(IDeviceBufferView view, GeometryInfo mesh);
}