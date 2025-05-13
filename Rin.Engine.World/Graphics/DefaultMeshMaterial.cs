using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Graphics.Textures;
using Rin.Engine.Math;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace Rin.Engine.World.Graphics;

public class DefaultMeshMaterial : IMeshMaterial
{
    private static DefaultMeshMaterial? _defaultInstance;

    public DefaultMeshMaterial()
    {
        ColorPass = new DefaultColorPass(this);
    }

    [PublicAPI] public Vector3 Color { get; set; } = new(1.0f);

    [PublicAPI] public ImageHandle ColorImageId { get; set; }

    [PublicAPI] public ImageHandle NormalImageId { get; set; } = ImageHandle.InvalidImage;
    [PublicAPI] public float Metallic { get; set; }
    [PublicAPI] public ImageHandle MetallicImageId { get; set; } = ImageHandle.InvalidImage;

    [PublicAPI] public float Specular { get; set; }

    [PublicAPI] public ImageHandle SpecularImageId { get; set; } = ImageHandle.InvalidImage;
    [PublicAPI] public float Roughness { get; set; }
    [PublicAPI] public ImageHandle RoughnessImageId { get; set; } = ImageHandle.InvalidImage;
    [PublicAPI] public float Emissive { get; set; }
    [PublicAPI] public ImageHandle EmissiveImageId { get; set; } = ImageHandle.InvalidImage;

    public static DefaultMeshMaterial DefaultMesh => _defaultInstance ??= new DefaultMeshMaterial();


    public bool Translucent => false;
    public IMaterialPass ColorPass { get; }
    public IMaterialPass DepthPass { get; } = new DefaultDepthPass();

    private struct PushConstant
    {
        public ulong SceneAddress;
        public ulong DataAddress;
    }

    private class DefaultColorPass(DefaultMeshMaterial meshMaterial) : SimpleMaterialPass
    {
        protected override IShader Shader { get; } = SGraphicsModule.Get()
            .MakeGraphics("World/Shaders/Mesh/mesh.slang");

        public override ulong GetRequiredMemory()
        {
            return Utils.ByteSizeOf<DefaultMaterialProperties>();
        }

        protected override IMaterialPass GetPass(ProcessedMesh mesh)
        {
            return mesh.Material.ColorPass;
        }

        protected override ulong ExecuteBatch(IShader shader, SceneFrame frame, IDeviceBufferView? data,
            ProcessedMesh[] meshes)
        {
            var cmd = frame.GetCommandBuffer();
            var push = Shader.PushConstants.Values.First();

            var set = SGraphicsModule.Get().GetImageFactory().GetDescriptorSet();
            cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, Shader.GetPipelineLayout(),
                [set]);

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

            cmd.PushConstant(Shader.GetPipelineLayout(), push.Stages, pushData);
            vkCmdDrawIndexed(cmd, first.IndicesCount, (uint)meshes.Length, first.IndicesStart, (int)first.VertexStart,
                0);
            return memoryUsed;
        }

        public override void Write(IDeviceBufferView view, ProcessedMesh mesh)
        {
            var data = new DefaultMaterialProperties
            {
                Transform = mesh.Transform,
                VertexAddress = mesh.VertexBuffer.GetAddress(),
                BaseColorTextureId = (int)meshMaterial.ColorImageId,
                BaseColor = meshMaterial.Color,
                NormalTextureId = (int)meshMaterial.NormalImageId,
                Metallic = meshMaterial.Metallic,
                MetallicTextureId = (int)meshMaterial.MetallicImageId,
                Specular = meshMaterial.Specular,
                SpecularTextureId = (int)meshMaterial.SpecularImageId,
                Roughness = meshMaterial.Roughness,
                RoughnessTextureId = (int)meshMaterial.RoughnessImageId,
                Emissive = meshMaterial.Emissive,
                EmissiveTextureId = (int)meshMaterial.EmissiveImageId
            };
            view.Write(data);
        }

        private struct DefaultMaterialProperties()
        {
            [PublicAPI] public ulong VertexAddress = 0;
            [PublicAPI] public Matrix4x4 Transform = Matrix4x4.Identity;
            private Vector4 _color_textureId;
            [PublicAPI] public int NormalTextureId = 0;
            private Vector4 _msre;
            private Vector4<int> _msreTextureId;

            public Vector3 BaseColor
            {
                get => new(_color_textureId.X, _color_textureId.Y, _color_textureId.Z);
                set
                {
                    _color_textureId.X = value.X;
                    _color_textureId.Y = value.Y;
                    _color_textureId.Z = value.Z;
                }
            }

            public int BaseColorTextureId
            {
                get => (int)_color_textureId.W;
                set => _color_textureId.W = value;
            }

            public float Metallic
            {
                get => _msre.X;
                set => _msre.X = value;
            }

            public int MetallicTextureId
            {
                get => _msreTextureId.X;
                set => _msreTextureId.X = value;
            }

            public float Specular
            {
                get => _msre.Y;
                set => _msre.Y = value;
            }

            public int SpecularTextureId
            {
                get => _msreTextureId.Y;
                set => _msreTextureId.Y = value;
            }

            public float Roughness
            {
                get => _msre.Z;
                set => _msre.Z = value;
            }

            public int RoughnessTextureId
            {
                get => _msreTextureId.Z;
                set => _msreTextureId.Z = value;
            }

            public float Emissive
            {
                get => _msre.W;
                set => _msre.W = value;
            }

            public int EmissiveTextureId
            {
                get => _msreTextureId.W;
                set => _msreTextureId.W = value;
            }
        }
    }


    private class DefaultDepthPass : SimpleMaterialPass
    {
        protected override IShader Shader { get; } = SGraphicsModule.Get()
            .MakeGraphics("World/Shaders/Mesh/mesh_depth.slang");

        public override ulong GetRequiredMemory()
        {
            return Utils.ByteSizeOf<DepthMaterialData>();
        }

        protected override IMaterialPass GetPass(ProcessedMesh mesh)
        {
            return mesh.Material.DepthPass;
        }

        protected override ulong ExecuteBatch(IShader shader, SceneFrame frame, IDeviceBufferView? data,
            ProcessedMesh[] meshes)
        {
            var cmd = frame.GetCommandBuffer();
            var push = Shader.PushConstants.Values.First();

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

            cmd.PushConstant(Shader.GetPipelineLayout(), push.Stages, pushData);
            vkCmdDrawIndexed(cmd, first.IndicesCount, (uint)meshes.Length, first.IndicesStart, (int)first.VertexStart,
                0);
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