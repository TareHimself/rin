#include "aerox/Engine.hpp"
#include <aerox/input/InputSubsystem.hpp>
#include <aerox/input/InputConsumer.hpp>
#include <aerox/input/AxisInputEvent.hpp>

namespace aerox::input {
String InputSubsystem::GetName() const {
  return "input";
}

void InputSubsystem::CheckMouse(float deltaTime) {
  if (GetOwner()->GetInputMode() == EInputMode::GameOnly) {
    if(auto window = GetOwner()->GetFocusedWindow().lock()) {
      const auto mousePosition = window->GetMousePosition();

      if (!_lastMousePosition.has_value()) {
        _lastMousePosition = mousePosition;
        return;
      }
      const auto windowSize = static_cast<glm::fvec2>(window->GetSize());
      const auto center = windowSize / 2.0f;
      const auto mouseDelta = mousePosition - _lastMousePosition.value();
      // const auto totalMouseDelta = mousePosition - center;

      ReceiveMouseMovedEvent(static_cast<float>(mouseDelta.x) * deltaTime,
                             static_cast<float>(mouseDelta.y) * deltaTime);

      window->SetMousePosition(center);
      _lastMousePosition = center;
    } else {
      _lastMousePosition.reset();
    }
  }
}

void InputSubsystem::ReceiveMouseMovedEvent(float deltaX, float deltaY) {
    std::shared_ptr<AxisInputEvent> xDeltaEvent{};
    std::shared_ptr<AxisInputEvent> yDeltaEvent{};
  if (deltaX > 0 || deltaX < 0) {
    xDeltaEvent = std::make_shared<AxisInputEvent>(EInputAxis::MouseX, deltaX);
  }

  if (deltaY > 0 || deltaY < 0) {
    yDeltaEvent = std::make_shared<AxisInputEvent>(EInputAxis::MouseY, deltaY);
  }

  if (xDeltaEvent || yDeltaEvent) {
    bool xHasCompleted = false;
    bool yHasCompleted = false;
    for (const auto &consumer : _consumers) {

      if (xHasCompleted && yHasCompleted) {
        return;
      }

      if (!consumer) {
        continue;
      }

      if (consumer->CanConsumeInput()) {
        if (xDeltaEvent && !xHasCompleted) {
          xHasCompleted = consumer->ReceiveAxis(
              MouseX, xDeltaEvent);
        }

        if (yDeltaEvent && !yHasCompleted) {
          yHasCompleted = consumer->ReceiveAxis(
              MouseY, yDeltaEvent);
        }
      }
    }
  }
}

bool InputSubsystem::ReceiveMouseDown(
    const std::shared_ptr<window::MouseButtonEvent> &event) {
  const auto keyCode = event->button;
  //_keysBeingPressed.Add(keyCode);
  for (const auto &consumer : _consumers) {
    if (!consumer) {
      continue;
    }

    if (consumer->CanConsumeInput() && consumer->ReceiveKeyDown(
            keyCode, std::make_shared<KeyInputEvent>(event))) {
      return true;
    }
  }
  return false;

}

bool InputSubsystem::ReceiveMouseUp(
    const std::shared_ptr<window::MouseButtonEvent> &event) {
  const auto keyCode = event->button;
  // if (_keysBeingPressed.contains(keyCode)) {
  //   _keysBeingPressed.erase(keyCode);
  // }

  for (const auto &consumer : _consumers) {
    if (!consumer) {
      continue;
    }

    if (consumer->CanConsumeInput() && consumer->
        ReceiveKeyUp(
            keyCode, std::make_shared<KeyInputEvent>(event))) {
      return true;
    }
  }

  return false;
}

bool InputSubsystem::ReceiveKeyDown(
    const std::shared_ptr<window::KeyEvent> &event) {
  const auto keyCode = event->key;
  // const auto keyEvent = std::make_shared<KeyboardEvent>(event);
  // _keysBeingPressed.Add(keyCode);
  for (const auto &consumer : _consumers) {
    if (!consumer) {
      continue;
    }

    if (consumer->CanConsumeInput() && consumer->ReceiveKeyDown(
            keyCode, std::make_shared<KeyInputEvent>(event))) {
      return true;
    }
  }
  return false;
}

bool InputSubsystem::ReceiveKeyUp(
    const std::shared_ptr<window::KeyEvent> &event) {
  const auto keyCode = event->key;
  // if (_keysBeingPressed.contains(keyCode)) {
  //   _keysBeingPressed.erase(keyCode);
  // }
  //
  // const auto keyEvent = std::make_shared<KeyboardEvent>(event);

  for (const auto &consumer : _consumers) {
    if (!consumer) {
      continue;
    }

    if (consumer->CanConsumeInput() && consumer->
        ReceiveKeyUp(
            keyCode, std::make_shared<KeyInputEvent>(event))) {
      return true;
    }
  }

  return false;
}

void InputSubsystem::InitConsumer(
    const std::shared_ptr<InputConsumer> &consumer) {
  consumer->Init();
  _consumers.push_back(consumer);
  std::weak_ptr<InputConsumer> weakPtr = consumer;
  consumer->onDestroyedDelegate->BindFunction([this,weakPtr] {
    if(const auto locked = weakPtr.lock(); locked && !IsPendingDestroy()) {
      _consumers.remove(weakPtr.lock());
    }
  });
}

void InputSubsystem::OnDestroy() {
  EngineSubsystem::OnDestroy();
  _consumers.clear();
}

void InputSubsystem::OnInit(Engine *outer) {
  EngineSubsystem::OnInit(outer);
  outer->onInputModeChanged->BindFunction([this](EInputMode _, const EInputMode mode) {
    if (mode == EInputMode::UiOnly || mode == EInputMode::GameAndUi) {
      _lastMousePosition.reset();
    }
  });

  auto window = outer->GetMainWindow().lock();

  AddCleanup(window::getManager()->onWindowKeyDown->BindFunction([this](const std::shared_ptr<window::KeyEvent>& event) {
    this->ReceiveKeyDown(event);
  }));
  AddCleanup(window::getManager()->onWindowKeyUp->BindFunction([this](const std::shared_ptr<window::KeyEvent>& event) {
    this->ReceiveKeyUp(event);
  }));
  AddCleanup(window::getManager()->onWindowMouseDown->BindFunction([this](const std::shared_ptr<window::MouseButtonEvent>& event) {
    this->ReceiveMouseDown(event);
  }));
  AddCleanup(window::getManager()->onWindowMouseUp->BindFunction([this](const std::shared_ptr<window::MouseButtonEvent>& event) {
    this->ReceiveMouseUp(event);
  }));
}
}
