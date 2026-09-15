namespace Rin.Graphics.Vulkan.Images;

public enum BindlessResourceState
{
    Invalid,
    Uploading,
    PendingBind,
    Ready
}