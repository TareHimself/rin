#pragma once
#include "AxisInputEvent.hpp"
#include "types.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/containers/Set.hpp"

#include <map>
#include <SDL3/SDL_events.h>

namespace vengine {
class Engine;
}

namespace vengine::input {
class KeyInputEvent;
}

namespace vengine::input {
typedef std::function<bool(const KeyInputEvent &handler)> keyEventHandler;
typedef std::function<bool(const AxisInputEvent &handler)> axisEventHandler;
typedef std::function<void()> unsubscribe;
typedef std::pair<keyEventHandler,keyEventHandler> callbackPair;
typedef std::map<uint64_t,callbackPair> keySubscriptions;

class InputManager : public Object<Engine> {
  uint64_t _pressHandlerIds = 0;
  uint64_t _releaseHandlerIds = 0;
  uint64_t _axisHandlerIds = 0;
  keySubscriptions _keyHandlers;
  std::unordered_map<SDL_KeyCode,keySubscriptions> _keySpecificHandlers;
  std::unordered_map<EInputAxis,std::map<uint64_t,axisEventHandler>> _axisHandlers;
  Array<InputManager *> _managers;
  Set<SDL_Keycode> _keysBeingPressed;
public:

  virtual void ProcessSdlEvent(const SDL_Event &e);

  virtual bool ReceiveMouseMovedEvent(const SDL_MouseMotionEvent &event);
  virtual bool ReceiveKeyPressedEvent(const SDL_KeyboardEvent &event);
  virtual bool ReceiveKeyReleasedEvent(const SDL_KeyboardEvent &event);

  unsubscribe BindKey(keyEventHandler pressed,keyEventHandler released);
  unsubscribe BindKey(SDL_KeyCode key,keyEventHandler pressed,keyEventHandler released);

  unsubscribe Subscribe(InputManager * manager);

  unsubscribe OnAxis(EInputAxis axis,axisEventHandler handler);
};
}
