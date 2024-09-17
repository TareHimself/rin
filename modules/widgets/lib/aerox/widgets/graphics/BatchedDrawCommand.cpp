#include "aerox/widgets/graphics/BatchedDrawCommand.hpp"
namespace aerox::widgets
{
    DrawCommand::Type BatchedDrawCommand::GetType() const
    {
        return Type::Batched;
    }
}
