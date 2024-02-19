#pragma once
#include "vengine/Managed.hpp"
#include "vengine/containers/String.hpp"

namespace vengine {
namespace window {
class Window;
}
}

namespace vengine::input {
class InputEvent {
protected:
  Ref<window::Window> _window;
public:
  virtual std::string GetName() const = 0;
  Ref<window::Window> GetWindow() const;
};
}

