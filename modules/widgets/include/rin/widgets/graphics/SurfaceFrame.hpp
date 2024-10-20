#pragma once
#include "rin/graphics/Frame.hpp"


class WidgetSurface;

struct SurfaceFrame
{
    WidgetSurface* surface;
    Frame* raw;
    std::string activePass{};
};
