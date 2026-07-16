using Rin.Core.Graphics;

namespace Rin.Graphics.Vulkan.Images;

public class BindlessResource : IBindlessResource
{
    public ResourceHandle Handle { get; set; }
    public BindlessResourceState State { get; set; }
}