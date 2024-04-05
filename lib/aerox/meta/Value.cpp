#include "aerox/meta/Value.hpp"

namespace aerox::meta
{

    Type Value::GetType() const {
      return _type;
    }

    Value::Value()
    {
        _data = nullptr;
        _type = {};
    }
}
