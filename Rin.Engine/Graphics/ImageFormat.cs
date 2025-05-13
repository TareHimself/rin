// ReSharper disable InconsistentNaming

namespace Rin.Engine.Graphics;

public enum ImageFormat
{
    R8,
    R16,
    R32,
    RG8,
    RG16,
    RG32,

    // RGB formats aren't really supported in vulkan
    //RGB8,
    //RGB16,
    //RGB32,
    RGBA8,
    RGBA16,
    RGBA32,
    Depth,
    Stencil,
    Swapchain
}