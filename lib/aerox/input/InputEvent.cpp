#include <aerox/input/InputEvent.hpp>

namespace aerox::input {
std::weak_ptr<window::Window> InputEvent::GetWindow() const {
  return _window;
}
}
