using Rin.Framework.Graphics.Images;

namespace Rin.Framework.Graphics.Vulkan.Images;

public class BindlessResource : IBindlessResource
{
    public BindlessResourceState State { get; set; }
    
    public ImageHandle Handle { get; set; }
}