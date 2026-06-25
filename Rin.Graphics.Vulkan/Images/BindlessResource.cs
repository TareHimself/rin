using Rin.Core.Graphics.Images;

namespace Rin.Graphics.Vulkan.Images;

public class BindlessResource : IBindlessResource
{
    public ImageHandle Handle { get; set; }
    public BindlessResourceState State { get; set; }
}