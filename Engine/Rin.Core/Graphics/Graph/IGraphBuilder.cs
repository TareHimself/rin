namespace Rin.Core.Graphics.Graph;

public interface IGraphBuilder
{
    public uint AddPass(IPass pass);

    public uint AddExternalImage(ResourceHandle handle, Action? onDispose = null);

    public uint AddDestinationImage(ResourceHandle handle, Action? onDispose = null);
}