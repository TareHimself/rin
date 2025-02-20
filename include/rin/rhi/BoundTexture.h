#pragma once

namespace rin::rhi
{
    struct BoundTexture
    {
        Shared<IDeviceImage> image{};
        ImageFilter filter{};
        ImageTiling tiling{};
        bool mips = false;
    };

}
