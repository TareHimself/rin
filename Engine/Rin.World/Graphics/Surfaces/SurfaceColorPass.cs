using System.Runtime.InteropServices;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics.Default;

namespace Rin.World.Graphics.Surfaces;

/// <summary>
///     The color pass every <see cref="SurfaceMaterial{TMaterial}" /> of one material type shares.
/// </summary>
public sealed class SurfaceColorPass<TMaterial> : SimpleMaterialPass
    where TMaterial : unmanaged, ISurfaceMaterial<TMaterial>
{
    private static SurfaceColorPass<TMaterial>? _shared;

    private SurfaceColorPass()
    {
    }

    public static SurfaceColorPass<TMaterial> Shared => _shared ??= new SurfaceColorPass<TMaterial>();

    public override IGraphicsShader Shader { get; } = IGraphicsModule.Get().MakeGraphics(TMaterial.Shader);

    public override ulong GetRequiredMemory()
    {
        return Utils.ByteSizeOf<SurfaceDraw<TMaterial>>();
    }

    public override IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer)
    {
        return Shader.Bind(frame.ExecutionContext)?.Push(new SurfaceShader<TMaterial>.PushConstants
        {
            Scene = new BufferRef<WorldInfo>(frame.SceneInfo.GetAddress()),
            Draws = new BufferRef<SurfaceDraw<TMaterial>>(groupMaterialBuffer.GetAddress())
        });
    }

    public override void DeclareResources(IGraphConfig config, ProcessedMesh mesh)
    {
        ReadTextures(config, Material(mesh).Textures);
    }

    public override void Write(Span<byte> destination, ProcessedMesh mesh)
    {
        MemoryMarshal.Write(destination, Material(mesh).ToDraw(mesh));
    }

    private static SurfaceMaterial<TMaterial> Material(ProcessedMesh mesh) => (SurfaceMaterial<TMaterial>)mesh.Material;
}
