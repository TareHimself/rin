using System.Runtime.InteropServices;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;

namespace rin.Framework.Scene.Graphics;

public class DefaultMaterial : IMaterial
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    // ReSharper disable once InconsistentNaming
    struct PBRMaterialProperties
    {
        public Mat4 Transform;

        public ulong VertexAddress;
       // public Vec4<float> 
       public int BaseColorTextureId;
       public Vec4<float> BaseColor;
       public int NormalTextureId;
       public float Metallic;
       public float Specular;
       public float Roughness;
       public float Emissive;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PushConstant
    {
        public Mat4 View;
        public Mat4 Projection;
        public ulong DataAddress;
    }

    public Vec4<float> Color { get; set; } = 1.0f;
    public float Metallic { get; set; } = 0.5f;
    public float Specular { get; set; } = 0.5f;
    public float Roughness { get; set; } = 0.5f;
    public float Emissive { get; set; } = 0.0f;
 
    private readonly IShader _shader  = SGraphicsModule.Get()
        .GraphicsShaderFromPath(Path.Join(SGraphicsModule.ShadersDirectory, "scene","mesh", "default.slang"));
    private readonly IShader _depthShader = SGraphicsModule.Get()
        .GraphicsShaderFromPath(Path.Join(SGraphicsModule.ShadersDirectory, "scene","mesh", "default_depth.slang"));
    
    public bool Translucent => false;
    public IShader GetShader() => _shader;

    public IShader GetDepthShader() => _depthShader;

    public void Execute(bool depth, SceneFrame frame, IDeviceBuffer? data, GeometryInfo[] meshes)
    {
        var requiredMemorySize = GetRequiredMemory(depth);
        
        var cmd = frame.GetCommandBuffer();
        if (data == null) throw new Exception("Missing buffer");
        if (_shader.Bind(cmd))
        {

            var bufferOffset = 0;
            var push = _shader.PushConstants.First().Value;
            var set = SGraphicsModule.Get().GetTextureManager().GetDescriptorSet();
            cmd.BindDescriptorSets(VkPipelineBindPoint.VK_PIPELINE_BIND_POINT_GRAPHICS, _shader.GetPipelineLayout(),
                [set]);

            using var view = data.GetView(bufferOffset,requiredMemorySize * meshes.Length);
            {
                var writeOffset = 0;
                foreach (var item in meshes)
                {
                    using var mem = view.GetView(writeOffset, requiredMemorySize);
                    item.Material.Write(depth, mem,item);
                    writeOffset += requiredMemorySize;
                }
            }
                
            var first = meshes.First();
            var offset = ((ulong)first.Surface.StartIndex) * sizeof(uint);
            var pushData = new PushConstant()
            {
                View = frame.View,
                Projection = frame.Projection,
                DataAddress = view.GetAddress()
            };
            cmd.PushConstant(_shader.GetPipelineLayout(), push.Stages, pushData);
            vkCmdBindIndexBuffer(cmd,first.Geometry.IndexBuffer.NativeBuffer,offset,VkIndexType.VK_INDEX_TYPE_UINT32);
            vkCmdDrawIndexed(cmd,first.Surface.Count,(uint)meshes.Length,0,0,0);
        }
    }

    public void Write(bool depth, IDeviceBuffer view, GeometryInfo mesh)
    {
        unsafe
        {
            view.Write(new PBRMaterialProperties
            {
                Transform = mesh.Transform,
                VertexAddress = mesh.Geometry.VertexBuffer.GetAddress() + (ulong)(sizeof(StaticMesh.Vertex) * mesh.Surface.StartIndex),
                BaseColorTextureId = 0,
                BaseColor = Color,
                NormalTextureId = 0,
                Metallic = Metallic,
                Specular = Specular,
                Roughness = Roughness,
                Emissive = Emissive,
            });
        }
    }

    public unsafe int GetRequiredMemory(bool depth)
    {
        return sizeof(PBRMaterialProperties);
    }


    private static DefaultMaterial? _defaultInstance;
    
    public static DefaultMaterial Default => _defaultInstance ??= new DefaultMaterial();
}