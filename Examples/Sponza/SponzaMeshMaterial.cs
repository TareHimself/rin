using System.Diagnostics;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.World.Graphics;
using Rin.World.Graphics.Default;

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

    [NoReorder]
    private struct PushConstant
    {
        public ulong SceneAddress;
        public ulong DataAddress;
    }

    private class ColorMeshPass : SimpleMaterialPass
    {
        public override IGraphicsShader Shader { get; } = IGraphicsModule.Get()
            .MakeGraphics(@"Sponza/mesh.slang");

        public override ulong GetRequiredMemory()
        {
            return Utils.ByteSizeOf<DefaultMaterialProperties>();
        }

        public override IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer)
        {
            var ctx = frame.ExecutionContext;
            if (Shader.Bind(ctx) is { } bindContext)
                return bindContext
                    .Push(new PushConstant
                    {
                        SceneAddress = frame.SceneInfo.GetAddress(),
                        DataAddress = groupMaterialBuffer!.GetAddress()
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

        public override void Write(in DeviceBufferView view, ProcessedMesh mesh)
        {
            Debug.Assert(mesh.Material is SponzaMeshMaterial);
            var meshMaterial = (SponzaMeshMaterial)mesh.Material;
            var data = new DefaultMaterialProperties
            {
                Transform = mesh.Transform,
                VertexAddress = mesh.VertexBuffer.GetAddress(),
                Color = meshMaterial.Color,
                ColorHandle = meshMaterial.ColorImageId,
                NormalHandle = meshMaterial.NormalImageId,
                MetallicRoughnessHandle = meshMaterial.MetallicRoughnessImageId
            };
            view.WriteSingle(data);
        }

        // Field order/types mirror Examples/Sponza/Content/mesh.slang's PerMeshData exactly.
        [NoReorder]
        private struct DefaultMaterialProperties()
        {
            [PublicAPI] public ulong VertexAddress = 0;
            [PublicAPI] public Matrix4x4 Transform = Matrix4x4.Identity;
            public Vector4 Color;
            public DeviceHandle ColorHandle;
            public DeviceHandle NormalHandle;
            public DeviceHandle MetallicRoughnessHandle;
        }
    }

    private class DepthMeshPass : SimpleMaterialPass
    {
        public override IGraphicsShader Shader { get; } = IGraphicsModule.Get()
            .MakeGraphics("Shaders/World/Mesh/mesh_depth.slang");

        public override ulong GetRequiredMemory()
        {
            return Utils.ByteSizeOf<DepthMaterialData>();
        }

        public override IGraphicsBindContext? BindGroup(WorldFrame frame, in DeviceBufferView groupMaterialBuffer)
        {
            var ctx = frame.ExecutionContext;
            if (Shader.Bind(ctx) is { } bindContext)
                return bindContext
                    .Push(new PushConstant
                    {
                        SceneAddress = frame.SceneInfo.GetAddress(),
                        DataAddress = groupMaterialBuffer!.GetAddress()
                    });

            return null;
        }

        protected override IMaterialPass GetPass(ProcessedMesh mesh)
        {
            return mesh.Material.DepthPass;
        }

        public override void Write(in DeviceBufferView view, ProcessedMesh mesh)
        {
            view.WriteSingle(new DepthMaterialData
            {
                Transform = mesh.Transform,
                VertexAddress = mesh.VertexBuffer.GetAddress()
            });
        }

        [NoReorder]
        private struct DepthMaterialData
        {
            public Matrix4x4 Transform;
            public ulong VertexAddress;
        }
    }
}