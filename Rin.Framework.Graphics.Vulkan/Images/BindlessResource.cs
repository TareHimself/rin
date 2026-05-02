using Rin.Framework.Graphics.Images;

namespace Rin.Framework.Graphics.Vulkan.Images;

public class BindlessResource : IBindlessResource
{
    public ImageHandle Handle { get; set; }
    public BindlessResourceState State { get; set; }
}