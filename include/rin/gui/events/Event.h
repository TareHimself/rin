#pragma once

namespace rin::gui
{
    class Surface;
    struct Event
    {
        Surface * surface{};

        Event(Surface * inSurface);
    };
}
