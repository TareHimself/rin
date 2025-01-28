using System.Runtime.InteropServices;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;
using TerraFX.Interop.Vulkan;
using static TerraFX.Interop.Vulkan.Vulkan;
using Utils = rin.Framework.Core.Utils;

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
    // ReSharper disable once InconsistentNaming
    struct DepthMaterialData
    {
        public Mat4 Transform;
        public ulong VertexAddress;
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct PushConstant
    {
        public ulong SceneAddress;
        public ulong DataAddress;
    }

    public Vec4<float> Color { get; set; } = 1.0f;
    public float Metallic { get; set; } = 0.5f;
    public float Specular { get; set; } = 0.5f;
    public float Roughness { get; set; } = 0.5f;
    public float Emissive { get; set; } = 0.0f;
 
    private readonly IShader _shader  = SGraphicsModule.Get()
        .GraphicsShaderFromPath(Path.Join(SGraphicsModule.ShadersDirectory, "scene","forward", "mesh.slang"));
    private readonly IShader _depthShader = SGraphicsModule.Get()
        .GraphicsShaderFromPath(Path.Join(SGraphicsModule.ShadersDirectory, "scene","forward", "mesh_depth.slang"));
    
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

            var materialInstanceData = data.GetView(bufferOffset,requiredMemorySize * (ulong)meshes.Length);
            {
                ulong writeOffset = 0;
                foreach (var item in meshes)
                {
                    var materialData = materialInstanceData.GetView(writeOffset, requiredMemorySize);
                    item.Material.Write(materialData,item, depth);
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
            vkCmdBindIndexBuffer(cmd,first.Geometry.IndexBuffer.NativeBuffer,offset,VkIndexType.VK_INDEX_TYPE_UINT32);
            vkCmdDrawIndexed(cmd,first.Surface.Count,(uint)meshes.Length,0,0,0);
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
                    VertexAddress = mesh.Geometry.VertexBuffer.GetAddress() + (ulong)(sizeof(StaticMesh.Vertex) * mesh.Surface.StartIndex),
                });
            }
            else
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
                    Emissive =  Emissive,
                });
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