using System.Numerics;
using JetBrains.Annotations;
using Rin.Engine.Graphics;
using Rin.Engine.Graphics.Shaders;
using Rin.Engine.Math;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
using Utils = Rin.Engine.Utils;

namespace Rin.Engine.World.Graphics;

public class DefaultMeshMaterial : IMeshMaterial
{
    private static DefaultMeshMaterial? _defaultInstance;

    public DefaultMeshMaterial()
    {
        ColorPass = new DefaultColorPass(this);
    }

    [PublicAPI] public Vector3 Color { get; set; } = new(1.0f);

    [PublicAPI] public int ColorTextureId { get; set; }

    [PublicAPI] public int NormaTextureId { get; set; }
    [PublicAPI] public float Metallic { get; set; }
    [PublicAPI] public int MetallicTextureId { get; set; }

    [PublicAPI] public float Specular { get; set; } = 1.0f;

    [PublicAPI] public int SpecularTextureId { get; set; }
    [PublicAPI] public float Roughness { get; set; }
    [PublicAPI] public int RoughnessTextureId { get; set; }
    [PublicAPI] public float Emissive { get; set; }
    [PublicAPI] public int EmissiveTextureId { get; set; }

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

        protected override IMaterialPass GetPass(GeometryInfo mesh)
        {
            return mesh.MeshMaterial.ColorPass;
        }

        protected override ulong ExecuteBatch(IShader shader, SceneFrame frame, IDeviceBufferView? data,
            GeometryInfo[] meshes)
        {
            var cmd = frame.GetCommandBuffer();
            var push = Shader.PushConstants.Values.First();

            var set = SGraphicsModule.Get().GetTextureFactory().GetDescriptorSet();
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
            var firstSurface = first.Mesh.GetSurface(first.SurfaceIndex);
            vkCmdDrawIndexed(cmd, firstSurface.IndicesCount, (uint)meshes.Length, 0, (int)firstSurface.VertexStart, 0);
            return memoryUsed;
        }

        public override void Write(IDeviceBufferView view, GeometryInfo mesh)
        {
            var data = new DefaultMaterialProperties
            {
                Transform = mesh.Transform,
                VertexAddress = mesh.Mesh.GetVertices(mesh.SurfaceIndex).GetAddress(),
                BaseColorTextureId = meshMaterial.ColorTextureId,
                BaseColor = meshMaterial.Color,
                NormalTextureId = meshMaterial.NormaTextureId,
                Metallic = meshMaterial.Metallic,
                MetallicTextureId = meshMaterial.MetallicTextureId,
                Specular = meshMaterial.Specular,
                SpecularTextureId = meshMaterial.SpecularTextureId,
                Roughness = meshMaterial.Roughness,
                RoughnessTextureId = meshMaterial.RoughnessTextureId,
                Emissive = meshMaterial.Emissive,
                EmissiveTextureId = meshMaterial.EmissiveTextureId
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
            private Vec4<int> _msreTextureId;

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

        protected override IMaterialPass GetPass(GeometryInfo mesh)
        {
            return mesh.MeshMaterial.DepthPass;
        }

        protected override ulong ExecuteBatch(IShader shader, SceneFrame frame, IDeviceBufferView? data,
            GeometryInfo[] meshes)
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
            var firstSurface = first.Mesh.GetSurface(first.SurfaceIndex);
            vkCmdDrawIndexed(cmd, firstSurface.IndicesCount, (uint)meshes.Length, 0, (int)firstSurface.VertexStart, 0);
            return memoryUsed;
        }

        public override void Write(IDeviceBufferView view, GeometryInfo mesh)
        {
            view.Write(new DepthMaterialData
            {
                Transform = mesh.Transform,
                VertexAddress = mesh.Mesh.GetVertices(mesh.SurfaceIndex).GetAddress()
            });
        }

        private struct DepthMaterialData
        {
            public Matrix4x4 Transform;
            public ulong VertexAddress;
        }
    }
}