#include "aerox/meta/Function.hpp"

namespace aerox::meta
{
    Function::Function(const std::string& name, functionTypedef call,bool isStatic):
        _call(std::move(call))
    {
        _fieldType = EFieldType::FieldFunction;
        _name = name;
      _isStatic = isStatic;
    }

    bool Function::IsStatic() const {
      return _isStatic;
    }
}
