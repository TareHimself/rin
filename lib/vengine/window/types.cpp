#include "vengine/window/types.hpp"

#include <fmt/format.h>

namespace vengine::window {
KeyEvent::operator std::string() const {
  return glfwGetKeyName(key,glfwGetKeyScancode(key));
}

KeyEvent::KeyEvent(EKey inKey) {
  key = inKey;
}

MouseMovedEvent::operator std::string() const {
  return fmt::format("x: {}, y: {}",x,y);
}

MouseMovedEvent::MouseMovedEvent(double inX, double inY) {
  x = inX;
  y = inY;
}

MouseButtonEvent::operator std::string() const {
  switch (button) {

  case MouseButton_Left:
    return "Left Mouse Button";
  case MouseButton_Right:
    return "Left Mouse Button";
  case MouseButton_Middle:
    return "Left Mouse Button";
  case MouseButton_4:
    return "Extra Mouse Button 1";
  case MouseButton_5:
    return "Extra Mouse Button 2";
  case MouseButton_6:
    return "Extra Mouse Button 3";
  case MouseButton_7:
    return "Extra Mouse Button 4";
  case MouseButton_8:
    return "Extra Mouse Button 5";
  
  default:
    return "Unknown Mouse Button";
  }
}

MouseButtonEvent::MouseButtonEvent(EMouseButton inButton, double inX,
    double inY) {
  button = inButton;
  x = inX;
  y = inY;
}
}
