#include "vengine/Engine.hpp"
#include <vengine/input/InputSubsystem.hpp>
#include <vengine/input/InputConsumer.hpp>
#include <vengine/input/AxisInputEvent.hpp>

namespace vengine::input {
String InputSubsystem::GetName() const {
  return "input";
}

void InputSubsystem::CheckMouse(float deltaTime) {
  if (GetEngine()->GetInputMode() == EInputMode::GameOnly) {
    auto window = GetEngine()->GetWindow().Reserve();
    const auto mousePosition = window->GetMousePosition();

    if (!_lastMousePosition.has_value()) {
      _lastMousePosition = mousePosition;
      return;
    }
    const auto windowSize = window->GetSize();
    const auto center = windowSize / 2;
    const auto mouseDelta = mousePosition - _lastMousePosition.value();
    // const auto totalMouseDelta = mousePosition - center;

    ReceiveMouseMovedEvent(static_cast<float>(mouseDelta.x) * deltaTime,
                           static_cast<float>(mouseDelta.y) * deltaTime);

    window->SetMousePosition(center);
    _lastMousePosition = center;
  }
}

void InputSubsystem::ReceiveMouseMovedEvent(float deltaX, float deltaY) {
  std::optional<std::shared_ptr<AxisInputEvent>> xDeltaEvent{};
  std::optional<std::shared_ptr<AxisInputEvent>> yDeltaEvent{};
  if (deltaX > 0 || deltaX < 0) {
    xDeltaEvent = std::make_shared<AxisInputEvent>(EInputAxis::MouseX, deltaX);
  }

  if (deltaY > 0 || deltaY < 0) {
    yDeltaEvent = std::make_shared<AxisInputEvent>(EInputAxis::MouseY, deltaY);
  }

  if (xDeltaEvent.has_value() || yDeltaEvent.has_value()) {
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
        if (xDeltaEvent.has_value() && !xHasCompleted) {
          xHasCompleted = consumer->ReceiveAxis(
              MouseX, xDeltaEvent.value());
        }

        if (yDeltaEvent.has_value() && !yHasCompleted) {
          yHasCompleted = consumer->ReceiveAxis(
              MouseY, yDeltaEvent.value());
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
    const Managed<InputConsumer> &consumer) {
  consumer->Init(this);
  _consumers.push_back(consumer);
  Ref<InputConsumer> weakPtr = consumer;
  consumer->onDestroyed.Bind([this,weakPtr] {
    if (weakPtr && !IsPendingDestroy()) {
      _consumers.remove(weakPtr.Reserve());
    }
  });
}

void InputSubsystem::BeforeDestroy() {
  EngineSubsystem::BeforeDestroy();
  _consumers.clear();
}

void InputSubsystem::Init(Engine *outer) {
  EngineSubsystem::Init(outer);
  outer->onInputModeChanged.Bind([this](EInputMode _, const EInputMode mode) {
    if (mode == EInputMode::UiOnly || mode == EInputMode::GameAndUi) {
      _lastMousePosition.reset();
    }
  });

  auto window = outer->GetWindow().Reserve();
  AddCleanup(window->onKeyDown,window->onKeyDown.Bind([this](const std::shared_ptr<window::KeyEvent>& event) {
    this->ReceiveKeyDown(event);
  }));
  AddCleanup(window->onKeyUp,window->onKeyUp.Bind([this](const std::shared_ptr<window::KeyEvent>& event) {
    this->ReceiveKeyUp(event);
  }));
  AddCleanup(window->onMouseDown,window->onMouseDown.Bind([this](const std::shared_ptr<window::MouseButtonEvent>& event) {
    this->ReceiveMouseDown(event);
  }));
  AddCleanup(window->onMouseUp,window->onMouseUp.Bind([this](const std::shared_ptr<window::MouseButtonEvent>& event) {
    this->ReceiveMouseUp(event);
  }));
}
}
