using System.Diagnostics;
using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.World.Graphics;
using Rin.Engine.World.Graphics.Default;

namespace Sponza;

public class SponzaMeshMaterial : IMeshMaterial
{
    public Vector4 Color { get; set; } = Vector4.One;
    public ImageHandle ColorImageId { get; set; }
    public ImageHandle NormalImageId { get; set; }
    public ImageHandle MetallicRoughnessImageId { get; set; }

    public bool Translucent => false;
    public IMaterialPass ColorPass { get; } = new ColorMeshPass();
    public IMaterialPass DepthPass { get; } = new DepthMeshPass();

    private struct PushConstant
    {
        public ulong SceneAddress;
        public ulong DataAddress;
    }

    private class ColorMeshPass : SimpleMaterialPass
    {
        public override IShader Shader { get; } = SGraphicsModule.Get()
            .MakeGraphics(@"Sponza/mesh.slang");

        public override ulong GetRequiredMemory()
        {
            return Utils.ByteSizeOf<DefaultMaterialProperties>();
        }

        public override bool BindAndPush(WorldFrame frame, IDeviceBufferView? groupMaterialBuffer)
        {
            var ctx = frame.ExecutionContext;
            if (Shader.Bind(ctx))
            {
                var pushData = new PushConstant
                {
                    SceneAddress = frame.SceneInfo.GetAddress(),
                    DataAddress = groupMaterialBuffer!.GetAddress()
                };

                Shader.Push(frame.ExecutionContext, pushData);
                return true;
            }

            return false;
        }

        protected override IMaterialPass GetPass(ProcessedMesh mesh)
        {
            return mesh.Material.ColorPass;
        }

        protected override ulong ExecuteBatch(IShader shader, WorldFrame frame, IDeviceBufferView? data,
            ProcessedMesh[] meshes)
        {
            ulong memoryUsed = 0;
            foreach (var item in meshes)
            {
                var pass = GetPass(item);
                var materialData = data!.GetView(memoryUsed, pass.GetRequiredMemory());
                pass.Write(materialData, item);
                memoryUsed += materialData.Size;
            }

            var first = meshes.First();

            var pushData = new PushConstant
            {
                SceneAddress = frame.SceneInfo.GetAddress(),
                DataAddress = data!.GetAddress()
            };

            var ctx = frame.ExecutionContext;
            Shader.Push(ctx, pushData);
            ctx
                .DrawIndexed(first.IndicesCount, (uint)meshes.Length,
                    first.IndicesStart, first.VertexStart);
            return memoryUsed;
        }

        public override void Write(IDeviceBufferView view, ProcessedMesh mesh)
        {
            Debug.Assert(mesh.Material is SponzaMeshMaterial);
            var meshMaterial = (SponzaMeshMaterial)mesh.Material;
            var data = new DefaultMaterialProperties
            {
                Transform = mesh.Transform,
                VertexAddress = mesh.VertexBuffer.GetAddress(),
                Color = meshMaterial.Color,
                ColorImageId = meshMaterial.ColorImageId,
                NormalImageId = meshMaterial.NormalImageId,
                MetallicRoughnessImageId = meshMaterial.MetallicRoughnessImageId

                // BaseColorTextureId = (int)meshMaterial.ColorTextureId,
                // BaseColor = meshMaterial.Color,
                // NormalTextureId = (int)meshMaterial.NormalTextureId,
                // Metallic = meshMaterial.Metallic,
                // MetallicTextureId = (int)meshMaterial.MetallicTextureId,
                // Specular = meshMaterial.Specular,
                // SpecularTextureId = (int)meshMaterial.SpecularTextureId,
                // Roughness = meshMaterial.Roughness,
                // RoughnessTextureId = (int)meshMaterial.RoughnessTextureId,
                // Emissive = meshMaterial.Emissive,
                // EmissiveTextureId = (int)meshMaterial.EmissiveTextureId
            };
            view.Write(data);
        }

        private struct DefaultMaterialProperties()
        {
            [PublicAPI] public ulong VertexAddress = 0;
            [PublicAPI] public Matrix4x4 Transform = Matrix4x4.Identity;
            public Vector4 Color;
            public ImageHandle ColorImageId;
            public ImageHandle NormalImageId;
            public ImageHandle MetallicRoughnessImageId;
        }
    }

    private class DepthMeshPass : SimpleMaterialPass
    {
        public override IShader Shader { get; } = SGraphicsModule.Get()
            .MakeGraphics("World/Shaders/Mesh/mesh_depth.slang");

        public override ulong GetRequiredMemory()
        {
            return Utils.ByteSizeOf<DepthMaterialData>();
        }

        public override bool BindAndPush(WorldFrame frame, IDeviceBufferView? groupMaterialBuffer)
        {
            var ctx = frame.ExecutionContext;
            if (Shader.Bind(ctx))
            {
                var pushData = new PushConstant
                {
                    SceneAddress = frame.SceneInfo.GetAddress(),
                    DataAddress = groupMaterialBuffer!.GetAddress()
                };

                Shader.Push(frame.ExecutionContext, pushData);
                return true;
            }

            return false;
        }

        protected override IMaterialPass GetPass(ProcessedMesh mesh)
        {
            return mesh.Material.DepthPass;
        }

        protected override ulong ExecuteBatch(IShader shader, WorldFrame frame, IDeviceBufferView? data,
            ProcessedMesh[] meshes)
        {
            ulong memoryUsed = 0;
            foreach (var item in meshes)
            {
                var pass = GetPass(item);
                var materialData = data!.GetView(memoryUsed, pass.GetRequiredMemory());
                pass.Write(materialData, item);
                memoryUsed += materialData.Size;
            }

            var first = meshes.First();

            var pushData = new PushConstant
            {
                SceneAddress = frame.SceneInfo.GetAddress(),
                DataAddress = data!.GetAddress()
            };
            var ctx = frame.ExecutionContext;
            Shader.Push(ctx, pushData);
            ctx
                .DrawIndexed(first.IndicesCount, (uint)meshes.Length,
                    first.IndicesStart, first.VertexStart);
            return memoryUsed;
        }

        public override void Write(IDeviceBufferView view, ProcessedMesh mesh)
        {
            view.Write(new DepthMaterialData
            {
                Transform = mesh.Transform,
                VertexAddress = mesh.VertexBuffer.GetAddress()
            });
        }

        private struct DepthMaterialData
        {
            public Matrix4x4 Transform;
            public ulong VertexAddress;
        }
    }
}