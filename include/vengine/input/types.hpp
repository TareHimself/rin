#ifndef VENGINE_INPUT_TYPES
#define VENGINE_INPUT_TYPES
#include "vengine/window/types.hpp"

#include <cstdint>

namespace vengine::input {
enum EInputAxis : uint8_t {
  MouseX,
  MouseY,
  MouseScroll
};

}
#endif