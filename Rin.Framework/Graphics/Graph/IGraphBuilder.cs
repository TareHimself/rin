using Rin.Framework.Graphics.Images;

namespace Rin.Framework.Graphics.Graph;

public interface IGraphBuilder
{
    public uint AddPass(IPass pass);

    public ICompiledGraph? Compile(Frame frame);
    public uint AddExternalTexture(ITexture texture, Action? onDispose = null);
    
    public uint AddDestinationTexture(ITexture texture, Action? onDispose = null);

    public void Reset();
}