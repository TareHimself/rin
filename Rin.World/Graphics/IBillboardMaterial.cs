using Rin.Core.Graphics.Shaders;

namespace Rin.World.Graphics;

public interface IBillboardMaterial
{
    public IShader Shader { get; }
}