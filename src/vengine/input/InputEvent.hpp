#pragma once
#include "vengine/containers/String.hpp"

#include <string>

namespace vengine::input {
class InputEvent {
public:
  virtual String GetName() const = 0;
};
}

