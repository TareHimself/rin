#pragma once
#include "CommandType.h"

namespace rin::gui
{
    class Command
    {
    public:
        virtual ~Command() = default;
        virtual CommandType GetCommandType() const = 0;
    };
}
