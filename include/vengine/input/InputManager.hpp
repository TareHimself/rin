#pragma once
#include "vengine/EngineSubsystem.hpp"
#include "vengine/containers/Set.hpp"
#include <SDL3/SDL_events.h>



namespace vengine {
class Engine;
}

namespace vengine::input {
class InputConsumer;

class KeyInputEvent;
}

namespace vengine::input {

class InputManager : public EngineSubsystem {
  
  std::list<Pointer<InputConsumer>> _consumers;
  Set<SDL_Keycode> _keysBeingPressed;
public:

  String GetName() const override;
  virtual void ProcessSdlEvent(const SDL_Event &e);
  virtual bool ReceiveMouseMovedEvent(const SDL_MouseMotionEvent &event);
  virtual bool ReceiveKeyPressedEvent(const SDL_KeyboardEvent &event);
  virtual bool ReceiveKeyReleasedEvent(const SDL_KeyboardEvent &event);

  template <typename T,typename... Args>
  WeakPointer<T> Consume(Args &&... args);

  void InitConsumer(const Pointer<InputConsumer> &consumer);

  void RemoveConsumer(const WeakPointer<InputConsumer> &consumer);

  void HandleDestroy() override;
  
};

template <typename T, typename ... Args> WeakPointer<T> InputManager::Consume(Args &&... args) {
  auto rawObj = newSharedObject<T>(args...);
  InitConsumer(rawObj);
  return rawObj;
}
}
