#pragma once
#include "aerox/graphics/Frame.hpp"

namespace aerox::widgets
{
    class Surface;
}

namespace aerox::widgets
{
    struct SurfaceFrame
    {
        Surface * surface;
        graphics::Frame * raw;
        std::string activePass{};
    };
}
