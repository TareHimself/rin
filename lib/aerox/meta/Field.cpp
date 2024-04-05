#include "aerox/meta/Field.hpp"

namespace aerox::meta
{
    std::string Field::GetName() const
    {
        return _name;
    }

    EFieldType Field::GetType() const
    {
        return _fieldType;
    }
}
