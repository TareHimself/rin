using Rin.Core.Graphics.Images;

namespace Rin.Core.Graphics.Graph;

public interface IGraphBuilder
{
    public uint AddPass(IPass pass);
    
    public uint AddExternalTexture(ITexture texture, Action? onDispose = null);

    public uint AddDestinationTexture(ITexture texture, Action? onDispose = null);
}