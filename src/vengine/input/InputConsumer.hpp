#pragma once
#include "AxisInputEvent.hpp"
#include "InputManager.hpp"
#include "vengine/Object.hpp"

namespace vengine::input {
typedef std::function<bool(const KeyInputEvent &handler)> keyEventHandler;
typedef std::function<bool(const AxisInputEvent &handler)> axisEventHandler;
typedef std::function<void()> unsubscribe;
typedef std::pair<keyEventHandler,keyEventHandler> callbackPair;
typedef std::map<uint64_t,callbackPair> keySubscriptions;

class InputConsumer : public Object<InputManager> {
  uint64_t _pressHandlerIds = 0;
  uint64_t _releaseHandlerIds = 0;
  uint64_t _axisHandlerIds = 0;
  keySubscriptions _keyHandlers;
  std::unordered_map<SDL_KeyCode,keySubscriptions> _keySpecificHandlers;
  std::unordered_map<EInputAxis,std::map<uint64_t,axisEventHandler>> _axisHandlers;
public:
  void Init(InputManager *outer) override;
  virtual bool CanConsumeInput();
  virtual bool ReceiveAxis(EInputAxis axis,const AxisInputEvent& event);
  virtual bool ReceiveKeyPressed(SDL_KeyCode key,const KeyInputEvent& event);
  virtual bool ReceiveKeyReleased(SDL_KeyCode key,const KeyInputEvent& event);
  unsubscribe BindKey(keyEventHandler pressed,keyEventHandler released);
  unsubscribe BindKey(SDL_KeyCode key,keyEventHandler pressed,keyEventHandler released);
  unsubscribe BindAxis(EInputAxis axis,axisEventHandler handler);
};
}
