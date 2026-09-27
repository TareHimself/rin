using System.Numerics;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Rin.Core;
using Rin.Core.Graphics;
using Rin.Core.Graphics.Graph;
using Rin.Core.Graphics.Shaders;
using Rin.Core.Shared.Math;

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

    [NoReorder]
    private struct PushConstant
    {
        public ulong SceneAddress;
        public ulong DataAddress;
    }

    private class DefaultColorPass(DefaultMeshMaterial meshMaterial) : SimpleMaterialPass
    {
        public override IGraphicsShader Shader { get; } = IGraphicsModule.Get()
            .MakeGraphics("Shaders/World/Mesh/mesh.slang");

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
            ReadTextures(config, meshMaterial.ColorImageId, meshMaterial.NormalImageId, meshMaterial.MetallicImageId,
                meshMaterial.SpecularImageId, meshMaterial.RoughnessImageId, meshMaterial.EmissiveImageId);
        }

        public override void Write(Span<byte> destination, ProcessedMesh mesh)
        {
            var data = new DefaultMaterialProperties
            {
                Transform = mesh.Transform,
                VertexAddress = mesh.VertexBuffer.GetAddress(),
                BaseColorTextureId = meshMaterial.ColorImageId,
                BaseColor = meshMaterial.Color,
                NormalTextureId = meshMaterial.NormalImageId,
                Metallic = meshMaterial.Metallic,
                MetallicTextureId = meshMaterial.MetallicImageId,
                Specular = meshMaterial.Specular,
                SpecularTextureId = meshMaterial.SpecularImageId,
                Roughness = meshMaterial.Roughness,
                RoughnessTextureId = meshMaterial.RoughnessImageId,
                Emissive = meshMaterial.Emissive,
                EmissiveTextureId = meshMaterial.EmissiveImageId
            };
            MemoryMarshal.Write(destination, in data);
        }

        [NoReorder]
        private struct DefaultMaterialProperties()
        {
            [PublicAPI] public ulong VertexAddress = 0;
            [PublicAPI] public Matrix4x4 Transform = Matrix4x4.Identity;
            private Vector4 _color_textureId;
            [PublicAPI] public DeviceHandle NormalTextureId = default;
            private Vector4 _msre;
            private Int4 _msreTextureId;

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

            // Packed into the color vec4's alpha lane rather than a dedicated int slot (mirrors
            // mesh.slang's PerMeshData.color_textureId) - stored as a numeric float, not bit-reinterpreted.
            public DeviceHandle BaseColorTextureId
            {
                get => (DeviceHandle)(uint)_color_textureId.W;
                set => _color_textureId.W = (uint)value;
            }

            public float Metallic
            {
                get => _msre.X;
                set => _msre.X = value;
            }

            public DeviceHandle MetallicTextureId
            {
                get => (DeviceHandle)(uint)_msreTextureId.X;
                set => _msreTextureId.X = (int)(uint)value;
            }

            public float Specular
            {
                get => _msre.Y;
                set => _msre.Y = value;
            }

            public DeviceHandle SpecularTextureId
            {
                get => (DeviceHandle)(uint)_msreTextureId.Y;
                set => _msreTextureId.Y = (int)(uint)value;
            }

            public float Roughness
            {
                get => _msre.Z;
                set => _msre.Z = value;
            }

            public DeviceHandle RoughnessTextureId
            {
                get => (DeviceHandle)(uint)_msreTextureId.Z;
                set => _msreTextureId.Z = (int)(uint)value;
            }

            public float Emissive
            {
                get => _msre.W;
                set => _msre.W = value;
            }

            public DeviceHandle EmissiveTextureId
            {
                get => (DeviceHandle)(uint)_msreTextureId.W;
                set => _msreTextureId.W = (int)(uint)value;
            }
        }
    }


    private class DefaultDepthPass : SimpleMaterialPass
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

        public override void Write(Span<byte> destination, ProcessedMesh mesh)
        {
            MemoryMarshal.Write(destination, new DepthMaterialData
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