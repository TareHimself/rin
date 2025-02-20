#pragma once
#include "rin/io/Window.h"
namespace rin::io
{
    struct Event
        {
            Window*window{nullptr};
            explicit Event(Window* inWindow);
        };
}
