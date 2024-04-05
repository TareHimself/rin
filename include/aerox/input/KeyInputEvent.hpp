#pragma once
#include "InputEvent.hpp"
#include "aerox/typedefs.hpp"
#include "aerox/window/types.hpp"

#include <variant>

namespace aerox::input {
typedef std::variant<std::shared_ptr<window::MouseButtonEvent>,std::shared_ptr<window::KeyEvent>> inputEventVariant;
class KeyInputEvent : public InputEvent {
   inputEventVariant _event;
  
public:
  KeyInputEvent(inputEventVariant event);
  virtual bool IsMouse() const;
  std::string GetName() const override;
   std::weak_ptr<window::MouseButtonEvent> GetMouseEvent();
   std::weak_ptr<window::KeyEvent> GetKeyEvent();
  
};
}
