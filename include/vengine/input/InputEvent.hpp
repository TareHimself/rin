#pragma once
#include "vengine/containers/String.hpp"

namespace vengine::input {
class InputEvent {
  
public:
  virtual std::string GetName() const = 0;
};
}

