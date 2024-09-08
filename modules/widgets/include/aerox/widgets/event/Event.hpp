#pragma once
#include "aerox/core/Disposable.hpp"
#include "aerox/core/memory.hpp"

namespace aerox::widgets
{
    class Surface;

    class Event : public Disposable
    {
    public:
        Shared<Surface> surface{};

        explicit Event(const Shared<Surface>& inSurface);
    };
}
