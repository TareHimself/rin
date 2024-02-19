#pragma once
#include "InputEvent.hpp"
#include "vengine/Managed.hpp"
#include "vengine/window/types.hpp"

#include <variant>

namespace vengine::input {
typedef std::variant<std::shared_ptr<window::MouseButtonEvent>,std::shared_ptr<window::KeyEvent>> inputEventVariant;
class KeyInputEvent : public InputEvent {
   inputEventVariant _event;
  
public:
  KeyInputEvent(inputEventVariant event);
  virtual bool IsMouse() const;
  std::string GetName() const override;
  std::shared_ptr<window::MouseButtonEvent> GetMouseEvent();
  std::shared_ptr<window::KeyEvent> GetKeyEvent();
  
};
}
