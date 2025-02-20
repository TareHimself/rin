#include "rin/gui/graphics/CustomCommand.h"
namespace rin::gui
{
    CommandType CustomCommand::GetCommandType() const
    {
        return CommandType::Custom;
    }
}
