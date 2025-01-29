using System.Runtime.InteropServices;
using JetBrains.Annotations;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
using Utils = rin.Framework.Core.Utils;

namespace rin.Framework.Scene.Graphics;

public class DefaultMaterial : IMaterial
{

    
    // ReSharper disable once InconsistentNaming
    struct PBRMaterialProperties()
    {
        public ulong VertexAddress = 0;
        public Mat4 Transform = Mat4.Identity;
        public Vec4<float> _color_textureId = 0.0f;
        public int NormalTextureId = 0;
        public Vec4<float> _msre = 0.0f;
        public Vec4<int> _msreTextureId = 0;

        public Vec3<float> BaseColor
        {
            get => new Vec3<float>(_color_textureId.X, _color_textureId.Y, _color_textureId.Z);
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

    
    // ReSharper disable once InconsistentNaming
    struct DepthMaterialData
    {
        public Mat4 Transform;
        public ulong VertexAddress;
    }

    
    struct PushConstant
    {
        public ulong SceneAddress;
        public ulong DataAddress;
    }

    [PublicAPI] public Vec3<float> Color { get; set; } = 1.0f;

    [PublicAPI] public int ColorTextureId { get; set; } = 2;

    [PublicAPI] public int NormaTextureId { get; set; }
    [PublicAPI] public float Metallic { get; set; }
    [PublicAPI] public int MetallicTextureId { get; set; }
    public float Specular { get; set; } = 1.0f;
    [PublicAPI] public int SpecularTextureId { get; set; }
    [PublicAPI] public float Roughness { get; set; }
    [PublicAPI] public int RoughnessTextureId { get; set; }
    [PublicAPI] public float Emissive { get; set; } = 0.0f;
    [PublicAPI] public int EmissiveTextureId { get; set; }

    private readonly IShader _shader = SGraphicsModule.Get()
        .GraphicsShaderFromPath(Path.Join(SGraphicsModule.ShadersDirectory, "scene", "forward", "mesh.slang"));

    private readonly IShader _depthShader = SGraphicsModule.Get()
        .GraphicsShaderFromPath(Path.Join(SGraphicsModule.ShadersDirectory, "scene", "forward", "mesh_depth.slang"));

    public bool Translucent => false;

    public void Execute(SceneFrame frame, IDeviceBufferView? data, GeometryInfo[] meshes, bool depth)
    {
        var requiredMemorySize = GetRequiredMemory(depth);

        var cmd = frame.GetCommandBuffer();
        if (data == null) throw new Exception("Missing buffer");
        var shader = depth ? _depthShader : _shader;
        if (shader.Bind(cmd))
        {
            ulong bufferOffset = 0;
            var push = _shader.PushConstants.Values.First();

            if (!depth)
            {
                var set = SGraphicsModule.Get().GetTextureManager().GetDescriptorSet();
                cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _shader.GetPipelineLayout(),
                    [set]);
            }

            var materialInstanceData = data.GetView(bufferOffset, requiredMemorySize * (ulong)meshes.Length);
            {
                ulong writeOffset = 0;
                foreach (var item in meshes)
                {
                    var materialData = materialInstanceData.GetView(writeOffset, requiredMemorySize);
                    item.Material.Write(materialData, item, depth);
                    writeOffset += materialData.Size;
                }
            }

            var first = meshes.First();
            var offset = ((ulong)first.Surface.StartIndex) * sizeof(uint);
            var pushData = new PushConstant()
            {
                SceneAddress = frame.SceneInfo.GetAddress(),
                DataAddress = materialInstanceData.GetAddress()
            };
            cmd.PushConstant(_shader.GetPipelineLayout(), push.Stages, pushData);
            vkCmdBindIndexBuffer(cmd, first.Geometry.IndexBuffer.NativeBuffer, offset,
                VkIndexType.VK_INDEX_TYPE_UINT32);
            vkCmdDrawIndexed(cmd, first.Surface.Count, (uint)meshes.Length, 0, 0, 0);
        }
    }

    public void Write(IDeviceBufferView view, GeometryInfo mesh, bool depth)
    {
        unsafe
        {
            if (depth)
            {
                view.Write(new DepthMaterialData
                {
                    Transform = mesh.Transform,
                    VertexAddress = mesh.Geometry.VertexBuffer.GetAddress() +
                                    (ulong)(sizeof(StaticMesh.Vertex) * mesh.Surface.StartIndex),
                });
            }
            else
            {
                var data = new PBRMaterialProperties
                {
                    Transform = mesh.Transform,
                    VertexAddress = mesh.Geometry.VertexBuffer.GetAddress() +
                                    (ulong)(sizeof(StaticMesh.Vertex) * mesh.Surface.StartIndex),
                    BaseColorTextureId = ColorTextureId,
                    BaseColor = Color,
                    NormalTextureId = NormaTextureId,
                    Metallic = Metallic,
                    MetallicTextureId = MetallicTextureId,
                    Specular = Specular,
                    SpecularTextureId = SpecularTextureId,
                    Roughness = Roughness,
                    RoughnessTextureId = RoughnessTextureId,
                    Emissive = Emissive,
                    EmissiveTextureId = EmissiveTextureId
                };
                view.Write(data);
            }
        }
    }

    public unsafe ulong GetRequiredMemory(bool depth)
    {
        return depth ? Utils.ByteSizeOf<DepthMaterialData>() : Utils.ByteSizeOf<PBRMaterialProperties>();
    }


    private static DefaultMaterial? _defaultInstance;

    public static DefaultMaterial Default => _defaultInstance ??= new DefaultMaterial();
}