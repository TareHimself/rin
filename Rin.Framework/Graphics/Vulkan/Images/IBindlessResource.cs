using Rin.Framework.Graphics.Images;

namespace Rin.Framework.Graphics.Vulkan.Images;

public interface IBindlessResource
{
    public BindlessResourceState State { get; set; }
}