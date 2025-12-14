using Rin.Framework.Graphics.Shaders;

namespace Rin.Engine.World.Graphics;

public interface IBillboardMaterial
{
    public IShader Shader { get; }
    
}