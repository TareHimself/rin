using Rin.Core.Graphics.Images;

namespace Rin.Core.Graphics.Graph;

public interface ICompiledGraph : IDisposable
{
    public ITexture GetTexture(uint id);
    public ITextureArray GetTextureArray(uint id);
    public ICubemap GetCubemap(uint id);

    public DeviceBufferView GetBuffer(uint id);

    public void Execute(IExecutionContext context);
}