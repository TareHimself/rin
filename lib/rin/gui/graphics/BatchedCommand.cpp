#include "rin/gui/graphics/BatchedCommand.h"
namespace rin::gui
{
    CommandType BatchedCommand::GetCommandType() const
    {
        return CommandType::Batched;
    }
}
