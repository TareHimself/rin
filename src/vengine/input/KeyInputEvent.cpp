#include "KeyInputEvent.hpp"


namespace vengine {
namespace input {
KeyInputEvent::KeyInputEvent(const SDL_KeyboardEvent &event) : _key(event) {
  
}

std::string KeyInputEvent::getName()  const {
  return SDL_GetScancodeName(_key.keysym.scancode);
}
}
}