#pragma once
#include <cstdint>

namespace rin::graphics
{
    enum class ImageFormat : uint8_t
    {
        Rgba8,
        Rgba16,
        Rgba32,
        Depth,
        Stencil
    };
}
