using System.Runtime.InteropServices;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics.Default;

namespace Rin.World.Graphics.Surfaces;

/// <summary>
///     The depth pass for a material type with its own <see cref="ISurfaceMaterial{TSelf}.DepthShader" />.
/// </summary>
public sealed class SurfaceDepthPass<TMaterial> : SimpleMaterialPass
    where TMaterial : unmanaged, ISurfaceMaterial<TMaterial>
{
    private static SurfaceDepthPass<TMaterial>? _shared;

    private SurfaceDepthPass(IGraphicsDescriptor depthShader)
    {
        Shader = IGraphicsModule.Get().MakeGraphics(depthShader);
    }

    public static SurfaceDepthPass<TMaterial> Shared => _shared ??= new SurfaceDepthPass<TMaterial>(
        TMaterial.DepthShader ?? throw new InvalidOperationException($"{typeof(TMaterial).Name} has no DepthShader."));

    public override IGraphicsShader Shader { get; }

    public override ulong GetRequiredMemory()
    {
        return Utils.ByteSizeOf<SurfaceDraw<TMaterial>>();
    }

    public override IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer)
    {
        return Shader.Bind(frame.ExecutionContext)?.Push(new SurfaceDepthShader<TMaterial>.PushConstants
        {
            Scene = new BufferRef<DepthSceneInfo>(frame.SceneInfo.GetAddress()),
            Draws = new BufferRef<SurfaceDraw<TMaterial>>(groupMaterialBuffer.GetAddress())
        });
    }

    public override void Write(Span<byte> destination, ProcessedMesh mesh)
    {
        MemoryMarshal.Write(destination, ((SurfaceMaterial<TMaterial>)mesh.Material).ToDraw(mesh));
    }
}
