using rin.Framework.Graphics;
using rin.Framework.Graphics.Shaders;

namespace rin.Framework.Scene.Graphics;

public interface IMaterial
{
    public bool Dynamic { get; }
    public bool Translucent { get; }
    public IShader DrawShader { get; }
    public IShader ShadowShader { get; }
    public ulong RequiredMemory { get; }
    public void Run(SceneFrame frame, MeshInfo[] meshes);
    public void ApplyShaderData(IDeviceBuffer buffer);
}