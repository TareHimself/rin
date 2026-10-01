using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Shade;
using Rin.World.Graphics.Default.Shaders;
using Rin.World.Graphics.Mesh;

namespace Rin.World.Graphics.Default;

public class DefaultMeshMaterial : IMeshMaterial
{
    private static DefaultMeshMaterial? _defaultInstance;

    public DefaultMeshMaterial()
    {
        ColorPass = new DefaultColorPass(this);
    }

    [PublicAPI] public Vector3 Color { get; set; } = new(1.0f);

    [PublicAPI] public ResourceHandle ColorImageId { get; set; }

    [PublicAPI] public ResourceHandle NormalImageId { get; set; } = ResourceHandle.InvalidTexture;
    [PublicAPI] public float Metallic { get; set; }
    [PublicAPI] public ResourceHandle MetallicImageId { get; set; } = ResourceHandle.InvalidTexture;

    [PublicAPI] public float Specular { get; set; }

    [PublicAPI] public ResourceHandle SpecularImageId { get; set; } = ResourceHandle.InvalidTexture;
    [PublicAPI] public float Roughness { get; set; }
    [PublicAPI] public ResourceHandle RoughnessImageId { get; set; } = ResourceHandle.InvalidTexture;
    [PublicAPI] public float Emissive { get; set; }
    [PublicAPI] public ResourceHandle EmissiveImageId { get; set; } = ResourceHandle.InvalidTexture;

    public static DefaultMeshMaterial DefaultMesh => _defaultInstance ??= new DefaultMeshMaterial();


    public bool Translucent => false;
    public IMaterialPass ColorPass { get; }
    public IMaterialPass DepthPass { get; } = new DefaultDepthPass();

    private class DefaultColorPass(DefaultMeshMaterial meshMaterial) : SimpleMaterialPass
    {
        public override IGraphicsShader Shader { get; } = IGraphicsModule.Get().MakeGraphics(MeshShader.Descriptor);

        public override ulong GetRequiredMemory()
        {
            return Utils.ByteSizeOf<MeshMaterialData>();
        }

        public override IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer)
        {
            var ctx = frame.ExecutionContext;
            if (Shader.Bind(ctx) is { } bindContext)
                return bindContext
                    .Push(new MeshShader.PushConstants
                    {
                        Scene = new BufferRef<WorldInfo>(frame.SceneInfo.GetAddress()),
                        Data = new BufferRef<MeshMaterialData>(groupMaterialBuffer!.GetAddress())
                    });

            return null;
        }

        protected override IMaterialPass GetPass(ProcessedMesh mesh)
        {
            return mesh.Material.ColorPass;
        }

        public override void DeclareResources(IGraphConfig config, ProcessedMesh mesh)
        {
            ReadTextures(config, meshMaterial.ColorImageId, meshMaterial.NormalImageId, meshMaterial.MetallicImageId,
                meshMaterial.SpecularImageId, meshMaterial.RoughnessImageId, meshMaterial.EmissiveImageId);
        }

        public override void Write(Span<byte> destination, ProcessedMesh mesh)
        {
            var data = new MeshMaterialData
            {
                Vertices = new BufferRef<Vertex>(mesh.VertexBuffer.GetAddress()),
                Transform = mesh.Transform,
                BaseColor = meshMaterial.Color,
                BaseColorTexture = meshMaterial.ColorImageId,
                NormalTexture = meshMaterial.NormalImageId,
                Metallic = meshMaterial.Metallic,
                MetallicTexture = meshMaterial.MetallicImageId,
                Specular = meshMaterial.Specular,
                SpecularTexture = meshMaterial.SpecularImageId,
                Roughness = meshMaterial.Roughness,
                RoughnessTexture = meshMaterial.RoughnessImageId,
                Emissive = meshMaterial.Emissive,
                EmissiveTexture = meshMaterial.EmissiveImageId
            };
            MemoryMarshal.Write(destination, in data);
        }
    }


    private class DefaultDepthPass : SimpleMaterialPass
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
