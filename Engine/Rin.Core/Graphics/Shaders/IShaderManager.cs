using Rin.Shade;

namespace Rin.Core.Graphics.Shaders;

public interface IShaderManager : IDisposable
{
    public Task Compile(IShader shader);
    public IGraphicsShader MakeGraphics(IGraphicsDescriptor descriptor);
    public IComputeShader MakeCompute(IComputeDescriptor descriptor);
}