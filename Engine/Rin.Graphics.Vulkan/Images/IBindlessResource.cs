namespace Rin.Graphics.Vulkan.Images;

public interface IBindlessResource
{
    public BindlessResourceState State { get; set; }
}