using Rin.Framework.Graphics.Images;

namespace Rin.Framework.Graphics.Graph;

public interface ICompiledGraph : IDisposable
{
    public ITexture GetTexture(uint id);
    public ITextureArray GetTextureArray(uint id);
    public ICubemap GetCubemap(uint id);

    public DeviceBufferView GetBuffer(uint id);

    public void Execute(IExecutionContext context);
}