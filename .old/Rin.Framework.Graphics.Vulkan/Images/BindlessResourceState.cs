namespace Rin.Framework.Graphics.Vulkan.Images;

public enum BindlessResourceState
{
    Invalid,
    Uploading,
    PendingBind,
    Ready
}