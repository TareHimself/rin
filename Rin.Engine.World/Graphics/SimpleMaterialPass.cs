﻿using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.World.Graphics;

/// <summary>
///     Interface for a material pass
/// </summary>
public abstract class SimpleMaterialPass : IMaterialPass
{
    protected abstract IShader Shader { get; }
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
        var cmd = frame.GetCommandBuffer();
        var first = meshes.First();
        if (requiredMemorySize > 0 && data == null) throw new Exception("Missing buffer");
        if (Shader.Bind(cmd))
        {
            vkCmdBindIndexBuffer(cmd, first.IndexBuffer.NativeBuffer, 0, VkIndexType.VK_INDEX_TYPE_UINT32);

            ulong offset = 0;
            foreach (var groupedMeshes in meshes.GroupBy(c => new
                     {
                         c.VertexCount,
                         c.VertexStart
                     }))
            {
                var groupArray = groupedMeshes.ToArray();

                offset += ExecuteBatch(Shader, frame, data!.GetView(offset, data.Size - offset), groupArray);
            }
        }
    }

    public abstract void Write(IDeviceBufferView view, ProcessedMesh mesh);

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