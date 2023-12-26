#pragma once
#include <string>

namespace vengine {
namespace input {
class InputEvent {
public:
  virtual std::string getName() const = 0;
};
}
}

