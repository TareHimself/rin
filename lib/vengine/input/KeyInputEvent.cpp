#include "vengine/input/KeyInputEvent.hpp"

namespace vengine::input {
KeyInputEvent::KeyInputEvent(inputEventVariant event) : _event(std::move(event)) {
}

bool KeyInputEvent::IsMouse() const {
  return std::holds_alternative<std::shared_ptr<window::MouseButtonEvent>>(_event);
}

std::string KeyInputEvent::GetName() const {
  if(IsMouse()) {
    return *std::get<std::shared_ptr<window::MouseButtonEvent>>(_event);
  }
  
  return *std::get<std::shared_ptr<window::KeyEvent>>(_event);
}
}
