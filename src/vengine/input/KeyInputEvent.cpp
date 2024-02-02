#include <vengine/input/KeyInputEvent.hpp>


namespace vengine::input {
KeyInputEvent::KeyInputEvent(const SDL_KeyboardEvent &event) : _key(event) {
  
}

String KeyInputEvent::GetName()  const {
  return SDL_GetScancodeName(_key.keysym.scancode);
}
}
