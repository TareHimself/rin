#pragma once
#include "aerox/typedefs.hpp"
#include "aerox/containers/String.hpp"

namespace aerox {
namespace window {
class Window;
}
}

namespace aerox::input {
class InputEvent {
protected:
  std::weak_ptr<window::Window> _window;
public:
  virtual std::string GetName() const = 0;
  std::weak_ptr<window::Window> GetWindow() const;
};
}

