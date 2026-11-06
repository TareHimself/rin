using System.Runtime.InteropServices;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default;

/// <summary>
///     The depth pass for any material that does not move vertices. Every such material returns <see cref="Shared" />,
///     so their depth draws batch together.
/// </summary>
public sealed class MeshDepthPass : SimpleMaterialPass
{
    private static MeshDepthPass? _shared;

    private MeshDepthPass()
    {
    }

    public static MeshDepthPass Shared => _shared ??= new MeshDepthPass();

    public override IGraphicsShader Shader { get; } = IGraphicsModule.Get().MakeGraphics(MeshDepthShader.Descriptor);

    public override ulong GetRequiredMemory()
    {
        return Utils.ByteSizeOf<MeshInstance>();
    }

    public override IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer)
    {
        return Shader.Bind(frame.ExecutionContext)?.Push(new MeshDepthShader.PushConstants
        {
            Scene = new BufferRef<DepthSceneInfo>(frame.SceneInfo.GetAddress()),
            Instances = new BufferRef<MeshInstance>(groupMaterialBuffer.GetAddress())
        });
    }

    public override void Write(Span<byte> destination, ProcessedMesh mesh)
    {
        MemoryMarshal.Write(destination, new MeshInstance
        {
            Transform = mesh.Transform,
            Vertices = new BufferRef<Vertex>(mesh.VertexBuffer.GetAddress())
        });
    }
}
