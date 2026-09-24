using System.Diagnostics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.World.Graphics.Default;

namespace Rin.World.Graphics;

/// <summary>
///     Interface for a material pass
/// </summary>
public abstract class SimpleMaterialPass : IMaterialPass
{
    public abstract IGraphicsShader Shader { get; }
    public abstract ulong GetRequiredMemory();

    public abstract void Write(in DeviceBufferView view, ProcessedMesh mesh);

    /// <summary>
    ///     Bind the shader for this material, push any constants, bind any descriptors
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="groupMaterialBuffer"></param>
    /// <returns></returns>
    public abstract IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer);

    protected abstract IMaterialPass GetPass(ProcessedMesh mesh);

    public virtual void DeclareResources(IGraphConfig config, ProcessedMesh mesh)
    {
    }

    protected static void ReadTextures(IGraphConfig config, params ReadOnlySpan<ResourceHandle> textures)
    {
        foreach (var texture in textures)
        {
            if (!texture.IsValid()) continue;
            var id = config.AddExternalImage(texture);
            Debug.Assert(id != 0, "Texture was freed while a material still references it");
            if (id != 0) config.ReadTexture(id, ImageLayout.ShaderReadOnly);
        }
    }
}