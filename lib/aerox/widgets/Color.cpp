#include "aerox/widgets/Color.hpp"

namespace aerox::widgets {
Color::operator glm::vec4() {
  return _color;
}

Color::operator glm::vec4() const {
  return _color;
}
}
