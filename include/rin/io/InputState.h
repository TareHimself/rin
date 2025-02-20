#pragma once
#include <cstdint>
namespace rin::io
{
    enum class InputState : uint8_t
    {
        Released,
        Pressed,
        Repeat
    };
}
