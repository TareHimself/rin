#include <vengine/input/InputEvent.hpp>

namespace vengine::input {
Ref<window::Window> InputEvent::GetWindow() const {
  return _window;
}
}
