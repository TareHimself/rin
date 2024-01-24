#include "InputConsumer.hpp"
#include "KeyInputEvent.hpp"

namespace vengine::input {
void InputConsumer::Init(InputManager *outer) {
  Object<InputManager>::Init(outer);
  
}

bool InputConsumer::CanConsumeInput() {
  return true;
}

bool InputConsumer::ReceiveAxis(const EInputAxis axis,
                                const AxisInputEvent &event) {
  if (_axisHandlers.contains(axis)) {
    for (const auto &val : _axisHandlers[axis] |
                           std::views::values) {
      if (val(event)) {
        return true;
      }
    }
  }

  return false;
}

bool InputConsumer::ReceiveKeyPressed(const SDL_KeyCode key,
                                      const KeyInputEvent &event) {
  for (const auto &val : _keyHandlers | std::views::values) {
    if (val.first(event)) {
      return true;
    }
  }

  if (_keySpecificHandlers.contains(key)) {

    for (const auto &handlers : _keySpecificHandlers[key] |
                                std::views::values) {
      if (handlers.first(event)) {
        return true;
      }
    }
  }

  return false;
}

bool InputConsumer::ReceiveKeyReleased(const SDL_KeyCode key,
                                       const KeyInputEvent &event) {

  for (const auto &val : _keyHandlers | std::views::values) {
    if (val.second(event)) {
      break;
    }
  }

  if (_keySpecificHandlers.contains(key)) {

    for (const auto &handlers : _keySpecificHandlers[key] |
                                std::views::values) {
      if (handlers.second(event)) {
        break;
      }
    }
  }

  return false;
}

unsubscribe InputConsumer::BindKey(keyEventHandler pressed,
                                   keyEventHandler released) {
  auto id = ++_pressHandlerIds;
  _keyHandlers.emplace(id, std::pair{std::move(pressed), std::move(released)});

  return [this,id] {
    if (_keyHandlers.contains(id)) {
      _keyHandlers.erase(_keyHandlers.find(id));
    }
  };
}

unsubscribe InputConsumer::BindKey(SDL_KeyCode key, keyEventHandler pressed,
                                   keyEventHandler released) {
  auto id = ++_pressHandlerIds;

  if (!_keySpecificHandlers.contains(key)) {
    keySubscriptions initial;

    initial.emplace(id, std::pair{std::move(pressed), std::move(released)});

    _keySpecificHandlers.emplace(key, std::move(initial));

  } else {
    _keySpecificHandlers[key].emplace(
        id, std::pair{std::move(pressed), std::move(released)});
  }

  return [this,key,id] {
    if (auto handlers = _keySpecificHandlers[key]; handlers.contains(id)) {
      handlers.erase(handlers.find(id));
    }
  };
}

unsubscribe InputConsumer::BindAxis(EInputAxis axis, axisEventHandler handler) {
  auto id = ++_axisHandlerIds;

  if (!_axisHandlers.contains(axis)) {
    std::map<uint64_t, axisEventHandler> initial;

    initial.emplace(id, std::move(handler));

    _axisHandlers.emplace(axis, std::move(initial));

  } else {
    _axisHandlers[axis].emplace(id, std::move(handler));
  }

  return [this,axis,id] {
    auto handlers = _axisHandlers[axis];
    if (handlers.contains(id)) {
      handlers.erase(handlers.find(id));
    }
  };
}

}
