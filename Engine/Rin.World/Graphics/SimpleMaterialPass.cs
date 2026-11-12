using System.Diagnostics;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.World.Graphics.Default;

namespace Rin.World.Graphics;

/// <summary>
///     Base for material passes that write a fixed-size block of data per draw.
/// </summary>
public abstract class SimpleMaterialPass : IMaterialPass
{
    public abstract IGraphicsShader Shader { get; }
    public abstract ulong GetRequiredMemory();

    public abstract void Write(Span<byte> destination, ProcessedMesh mesh);

    /// <summary>
    ///     Bind the shader for this material, push any constants, bind any descriptors
    /// </summary>
    public abstract IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer);

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