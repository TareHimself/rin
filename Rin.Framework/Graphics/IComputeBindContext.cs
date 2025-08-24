using Rin.Framework.Graphics.Shaders;

namespace Rin.Framework.Graphics;

public interface IComputeBindContext : IBindContext<IComputeBindContext>
{
    public IComputeShader Shader { get; }

    public IBindContext Dispatch(uint x, uint y = 1, uint z = 1);

    public IBindContext Invoke(uint x, uint y = 1, uint z = 1)
    {
        return Dispatch((uint)float.Ceiling(x / (float)Shader.GroupSizeX),
            (uint)float.Ceiling(y / (float)Shader.GroupSizeY),
            (uint)float.Ceiling(z / (float)Shader.GroupSizeZ));
    }
}