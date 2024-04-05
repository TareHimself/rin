#include "aerox/input/KeyInputEvent.hpp"
#include "aerox/window/Window.hpp"
namespace aerox::input {
KeyInputEvent::KeyInputEvent(inputEventVariant event) : _event(std::move(event)) {
  if(std::holds_alternative<std::shared_ptr<window::MouseButtonEvent>>(_event)) {
    _window = window::getManager()->Find(GetMouseEvent().lock()->window->GetId());
  }
  else {
    _window = window::getManager()->Find(GetKeyEvent().lock()->window->GetId());
  }
}

bool KeyInputEvent::IsMouse() const {
  return std::holds_alternative<std::shared_ptr<window::MouseButtonEvent>>(_event);
}

std::string KeyInputEvent::GetName() const {
  if(IsMouse()) {
    return *std::get<std::shared_ptr<window::MouseButtonEvent>>(_event).get();
  }
  
  return *std::get<std::shared_ptr<window::KeyEvent>>(_event).get();
}

std::weak_ptr<window::MouseButtonEvent> KeyInputEvent::GetMouseEvent() {
  if(IsMouse()) {
    return std::get<std::shared_ptr<window::MouseButtonEvent>>(_event);
  }
  
  return {};
}

std::weak_ptr<window::KeyEvent> KeyInputEvent::GetKeyEvent() {
  if(!IsMouse()) {
    return std::get<std::shared_ptr<window::KeyEvent>>(_event);
  }
  
  return {};
}
}
