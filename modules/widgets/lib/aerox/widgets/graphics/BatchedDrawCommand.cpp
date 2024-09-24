#include "aerox/widgets/graphics/BatchedDrawCommand.hpp"
DrawCommand::Type BatchedDrawCommand::GetType() const
{
    return Type::Batched;
}
