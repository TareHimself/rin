#include "aerox/meta/Type.hpp"


namespace aerox::meta
{
template<typename Base, typename Derived>
bool is_base_of(const Base&, const Derived&) {
  return std::is_base_of<Base, Derived>::value;
}

    Type::Type() = default;

    bool Type::operator==(const Type& other) const
    {
        return _typeIndex == other._typeIndex;
    }

    TFlags<ETypeFlags> Type::GetFlags() const {
      return _flags;
    }

    std::type_index Type::GetTypeIndex() const {
      return _typeIndex;
    }

    size_t Type::GetSize() const {
      return _size;
    }
}
