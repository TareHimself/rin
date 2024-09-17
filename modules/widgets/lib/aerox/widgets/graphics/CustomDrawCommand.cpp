#include "aerox/widgets/graphics/CustomDrawCommand.hpp"
namespace aerox::widgets
{
    DrawCommand::Type CustomDrawCommand::GetType() const
    {
        return Type::Custom;
    }
}
