#include <vengine/input/InputManager.hpp>
#include <vengine/input/InputConsumer.hpp>
#include <vengine/input/AxisInputEvent.hpp>
#include <vengine/input/KeyInputEvent.hpp>

namespace vengine::input {
String InputManager::GetName() const {
  return "input";
}

void InputManager::ProcessSdlEvent(const SDL_Event &e) {
  switch (e.type) {
  case SDL_EVENT_KEY_UP:
    ReceiveKeyReleasedEvent(e.key);
    break;
  case SDL_EVENT_KEY_DOWN:
    if (e.key.repeat == 0) {
      ReceiveKeyPressedEvent(e.key);
    }
    break;
  case SDL_EVENT_MOUSE_MOTION:
    ReceiveMouseMovedEvent(e.motion);
    break;
  default: ;
  }
}

bool InputManager::ReceiveMouseMovedEvent(const SDL_MouseMotionEvent &event) {
  const auto deltaX = event.xrel;
  const auto deltaY = event.yrel;

  if (deltaX > 0 || deltaX < 0) {
    const AxisInputEvent axisEvent(EInputAxis::MouseX, deltaX);
    for (const auto &consumer : _consumers) {
      if (!consumer) {
        continue;
      }

      if (consumer->CanConsumeInput() && consumer->ReceiveAxis(
                                                MouseX, axisEvent)) {
        return true;
      }
    }
  }

  if (deltaY > 0 || deltaY < 0) {
    const AxisInputEvent axisEvent(EInputAxis::MouseY, deltaY);
    for (const auto &consumer : _consumers) {
      if (!consumer) {
        continue;
      }

      if (consumer->CanConsumeInput() && consumer->ReceiveAxis(
                                                MouseY, axisEvent)) {
        return true;
      }
    }
  }

  return false;
}

bool InputManager::ReceiveKeyPressedEvent(const SDL_KeyboardEvent &event) {
  const auto keyCode = static_cast<SDL_KeyCode>(event.keysym.sym);
  const KeyInputEvent keyEvent(event);

  for (const auto &consumer : _consumers) {
    if (!consumer) {
      continue;
    }

    if (consumer->CanConsumeInput() && consumer->ReceiveKeyPressed(
                                              keyCode, keyEvent)) {
      return true;
    }
  }
  return false;
}

bool InputManager::ReceiveKeyReleasedEvent(const SDL_KeyboardEvent &event) {
  const auto keyCode = static_cast<SDL_KeyCode>(event.keysym.sym);
  const KeyInputEvent keyEvent(event);
  for (const auto &consumer : _consumers) {
    if (!consumer) {
      continue;
    }

    if (consumer->CanConsumeInput() && consumer->
                                          ReceiveKeyReleased(
                                              keyCode, keyEvent)) {
      return true;
    }
  }

  return false;
}

void InputManager::InitConsumer(
    const Pointer<InputConsumer> &consumer) {
  consumer->Init(this);
  _consumers.push_back(consumer);
  WeakPointer<InputConsumer> weakPtr = consumer; 
  consumer->onDestroyed.On([this,weakPtr] {
    if(weakPtr && !IsPendingDestroy()) {
      _consumers.remove(weakPtr.Reserve());
    }
  });
}

void InputManager::HandleDestroy() {
  EngineSubsystem::HandleDestroy();
  _consumers.clear();
}
}
