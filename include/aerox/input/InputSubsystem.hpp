#pragma once
#include "aerox/EngineSubsystem.hpp"
#include "glm/glm.hpp"
#include "gen/input/InputSubsystem.gen.hpp"
#include "aerox/window/types.hpp"

#include <variant>

namespace aerox {
class Engine;
}

namespace aerox::input {
class InputConsumer;

class KeyboardEvent;
}

namespace aerox::input {

typedef std::variant<window::EKey,window::EMouseButton> bindableKey;
META_TYPE()
class InputSubsystem : public EngineSubsystem {
  
  std::list<std::shared_ptr<InputConsumer>> _consumers;
  // Set<SDL_Keycode> _keysBeingPressed;
  std::optional<glm::dvec2> _lastMousePosition;
  //Set<bindableKey> _keysBeingPressed;
public:

  META_BODY()
  
  String GetName() const override;
  void CheckMouse(float deltaTime);
  virtual void ReceiveMouseMovedEvent(float deltaX, float deltaY);
  virtual bool ReceiveMouseDown(const std::shared_ptr<window::MouseButtonEvent> &event);
  virtual bool ReceiveMouseUp(const std::shared_ptr<window::MouseButtonEvent> &event);
  virtual bool ReceiveKeyDown(const std::shared_ptr<window::KeyEvent> &event);
  virtual bool ReceiveKeyUp(const std::shared_ptr<window::KeyEvent> &event);

  template <typename T,typename... Args>
  std::weak_ptr<T> Consume(Args &&... args);

  void InitConsumer(const std::shared_ptr<InputConsumer> &consumer);

  void OnDestroy() override;
  void OnInit(Engine *outer) override;
};

template <typename T, typename ... Args> std::weak_ptr<T> InputSubsystem::Consume(Args &&... args) {
  auto rawObj = newObject<T>(args...);
  InitConsumer(rawObj);
  return rawObj;
}
}
