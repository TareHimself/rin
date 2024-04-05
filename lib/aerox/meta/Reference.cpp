#include "aerox/meta/Reference.hpp"

namespace aerox::meta {
Reference::Reference() {
  _data = nullptr;
}

Type Reference::GetType() const {
  return _type;
}
}
