#pragma once
#include "vengine/Object.hpp"
#include "vengine/containers/Set.hpp"

#include <map>
#include <SDL2/SDL_events.h>

namespace vengine {
class Engine;
}

namespace vengine {
namespace input {
class KeyInputEvent;
}
}

namespace vengine {
namespace input {
typedef std::function<bool(const KeyInputEvent &handler)> keyEventHandler;
typedef std::function<void()> unsubscribe;

class InputManager : public Object<Engine> {
  std::map<SDL_KeyCode,Array<keyEventHandler>> keySpecificPressedHandlers;
  std::map<SDL_KeyCode,Array<keyEventHandler>> keySpecificReleasedHandlers;
  // Array<keyEventHandler> upHandlers;
  // Array<keyEventHandler> downHandlers;
  Set<SDL_Keycode> keysBeingPressed;
public:
  void receiveKeyPressedEvent(const SDL_KeyboardEvent &event);
  void receiveKeyReleasedEvent(const SDL_KeyboardEvent &event);
  
  unsubscribe onPressed(SDL_KeyCode key,keyEventHandler handler);

  unsubscribe onReleased(SDL_KeyCode key,keyEventHandler handler);
};
}
}
