#ifndef VENGINE_INPUT_TYPES
#define VENGINE_INPUT_TYPES
#include "aerox/window/types.hpp"

#include <cstdint>

namespace aerox::input {
enum EInputAxis : uint8_t {
  MouseX,
  MouseY,
  MouseScroll
};

}
#endif