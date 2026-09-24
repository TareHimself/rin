namespace Rin.Graphics.Vulkan.Graph;

public interface IExternalResourceDescriptor : IResourceDescriptor
{
    public IDisposable Resource { get; }
}
