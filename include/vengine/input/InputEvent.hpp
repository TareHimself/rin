#pragma once
#include "vengine/containers/String.hpp"

namespace vengine::input {
class InputEvent {
public:
  virtual String GetName() const = 0;
};
}

