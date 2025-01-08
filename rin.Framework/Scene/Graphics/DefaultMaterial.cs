using System.Runtime.InteropServices;
using rin.Framework.Core.Math;
using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;

namespace rin.Framework.Scene.Graphics;

public class DefaultMaterial : IMaterial
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct MaterialProperties
    {
        public Mat4 transform;
       // public Vec4<float> 
    }
    
    public bool Dynamic => false;
    public bool Translucent => false;

    public IShader DrawShader { get; } = SGraphicsModule.Get()
        .GraphicsShaderFromPath(Path.Join(SGraphicsModule.ShadersDirectory, "scene", "default.slang"));
    public IShader ShadowShader { get; } = SGraphicsModule.Get()
        .GraphicsShaderFromPath(Path.Join(SGraphicsModule.ShadersDirectory, "scene", "default_shadow.slang"));

    public ulong RequiredMemory => 0;
    
    public void Run(SceneFrame frame, MeshInfo[] meshes)
    {
        throw new NotImplementedException();
    }

    public void ApplyShaderData(IDeviceBuffer buffer)
    {
        
    }
}