#pragma once
#include "vengine/EngineSubsystem.hpp"
#include "glm/glm.hpp"
#include "generated/input/InputSubsystem.reflect.hpp"
#include "vengine/window/types.hpp"

#include <variant>

namespace vengine {
class Engine;
}

namespace vengine::input {
class InputConsumer;

class KeyboardEvent;
}

namespace vengine::input {

typedef std::variant<window::EKey,window::EMouseButton> bindableKey;
RCLASS()
class InputSubsystem : public EngineSubsystem {
  
  std::list<Managed<InputConsumer>> _consumers;
  // Set<SDL_Keycode> _keysBeingPressed;
  std::optional<glm::dvec2> _lastMousePosition;
  //Set<bindableKey> _keysBeingPressed;
public:

  String GetName() const override;
  void CheckMouse(float deltaTime);
  virtual void ReceiveMouseMovedEvent(float deltaX, float deltaY);
  virtual bool ReceiveMouseDown(const std::shared_ptr<window::MouseButtonEvent> &event);
  virtual bool ReceiveMouseUp(const std::shared_ptr<window::MouseButtonEvent> &event);
  virtual bool ReceiveKeyDown(const std::shared_ptr<window::KeyEvent> &event);
  virtual bool ReceiveKeyUp(const std::shared_ptr<window::KeyEvent> &event);

  template <typename T,typename... Args>
  Ref<T> Consume(Args &&... args);

  void InitConsumer(const Managed<InputConsumer> &consumer);

  void BeforeDestroy() override;
  void Init(Engine *outer) override;
};

template <typename T, typename ... Args> Ref<T> InputSubsystem::Consume(Args &&... args) {
  auto rawObj = newManagedObject<T>(args...);
  InitConsumer(rawObj);
  return rawObj;
}

REFLECT_IMPLEMENT(InputSubsystem)
}
