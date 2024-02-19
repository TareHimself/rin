#include "vengine/input/KeyInputEvent.hpp"
#include "vengine/window/Window.hpp"
namespace vengine::input {
KeyInputEvent::KeyInputEvent(inputEventVariant event) : _event(std::move(event)) {
  if(IsMouse()) {
    _window = window::getManager()->Find(GetMouseEvent()->window->GetId());
  }
  else {
    _window = window::getManager()->Find(GetKeyEvent()->window->GetId());
  }
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

std::shared_ptr<window::MouseButtonEvent> KeyInputEvent::GetMouseEvent() {
  if(IsMouse()) {
    return std::get<std::shared_ptr<window::MouseButtonEvent>>(_event);
  }
  
  return nullptr;
}

std::shared_ptr<window::KeyEvent> KeyInputEvent::GetKeyEvent() {
  if(!IsMouse()) {
    return std::get<std::shared_ptr<window::KeyEvent>>(_event);
  }
  
  return nullptr;
}
}
