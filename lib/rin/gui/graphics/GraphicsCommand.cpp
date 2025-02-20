#include "rin/gui/graphics/GraphicsCommand.h"
namespace rin::gui
{
    bool GraphicsCommand::RequiresStencil() const
    {
        return true;
    }
}
