#pragma once
#include "vengine/containers/String.hpp"

#include <string>

namespace vengine {
namespace input {
class InputEvent {
public:
  virtual String getName() const = 0;
};
}
}

