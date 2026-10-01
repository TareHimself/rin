using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.Mesh;
using Sponza.Shaders;

namespace Sponza;

public class SponzaMeshMaterial : IMeshMaterial
{
    public Vector4 Color { get; set; } = Vector4.One;
    public ResourceHandle ColorImageId { get; set; }
    public ResourceHandle NormalImageId { get; set; }
    public ResourceHandle MetallicRoughnessImageId { get; set; }

    public bool Translucent => false;
    public IMaterialPass ColorPass { get; } = new ColorMeshPass();
    public IMaterialPass DepthPass { get; } = new DepthMeshPass();

    private class ColorMeshPass : SimpleMaterialPass
    {
        public override IGraphicsShader Shader { get; } = IGraphicsModule.Get().MakeGraphics(SponzaMeshShader.Descriptor);

        public override ulong GetRequiredMemory()
        {
            return Utils.ByteSizeOf<SponzaMeshData>();
        }

        public override IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer)
        {
            var ctx = frame.ExecutionContext;
            if (Shader.Bind(ctx) is { } bindContext)
                return bindContext
                    .Push(new SponzaMeshShader.PushConstants
                    {
                        Scene = new BufferRef<WorldInfo>(frame.SceneInfo.GetAddress()),
                        Data = new BufferRef<SponzaMeshData>(groupMaterialBuffer!.GetAddress())
                    });

            return null;
        }

        protected override IMaterialPass GetPass(ProcessedMesh mesh)
        {
            return mesh.Material.ColorPass;
        }

        public override void DeclareResources(IGraphConfig config, ProcessedMesh mesh)
        {
            var meshMaterial = (SponzaMeshMaterial)mesh.Material;
            ReadTextures(config, meshMaterial.ColorImageId, meshMaterial.NormalImageId,
                meshMaterial.MetallicRoughnessImageId);
        }

        public override void Write(Span<byte> destination, ProcessedMesh mesh)
        {
            Debug.Assert(mesh.Material is SponzaMeshMaterial);
            var meshMaterial = (SponzaMeshMaterial)mesh.Material;
            var data = new SponzaMeshData
            {
                Vertices = new BufferRef<Vertex>(mesh.VertexBuffer.GetAddress()),
                Transform = mesh.Transform,
                Color = meshMaterial.Color,
                ColorTexture = meshMaterial.ColorImageId,
                NormalTexture = meshMaterial.NormalImageId,
                MetallicRoughnessTexture = meshMaterial.MetallicRoughnessImageId
            };
            MemoryMarshal.Write(destination, in data);
        }
    }

    private class DepthMeshPass : SimpleMaterialPass
    {
        public override IGraphicsShader Shader { get; } = IGraphicsModule.Get().MakeGraphics(MeshDepthShader.Descriptor);

        public override ulong GetRequiredMemory()
        {
            return Utils.ByteSizeOf<DepthMaterialData>();
        }

        public override IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer)
        {
            var ctx = frame.ExecutionContext;
            if (Shader.Bind(ctx) is { } bindContext)
                return bindContext
                    .Push(new MeshDepthShader.PushConstants
                    {
                        Scene = new BufferRef<DepthSceneInfo>(frame.SceneInfo.GetAddress()),
                        Data = new BufferRef<DepthMaterialData>(groupMaterialBuffer!.GetAddress())
                    });

            return null;
        }

        protected override IMaterialPass GetPass(ProcessedMesh mesh)
        {
            return mesh.Material.DepthPass;
        }

        public override void Write(Span<byte> destination, ProcessedMesh mesh)
        {
            MemoryMarshal.Write(destination, new DepthMaterialData
            {
                Transform = mesh.Transform,
                Vertices = new BufferRef<Vertex>(mesh.VertexBuffer.GetAddress())
            });
        }
    }
}
