#pragma once
#include "Command.h"
#include "CommandType.h"
#include "GraphicsCommand.h"
#include "IBatcher.h"

namespace rin::gui
{
    class BatchedCommand : public GraphicsCommand
    {
    public:
        CommandType GetCommandType() const override;

        [[nodiscard]] virtual IBatcher * GetBatcher() const = 0;
    };
}
