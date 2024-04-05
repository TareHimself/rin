#include "aerox/input/KeyInputEvent.hpp"

#include <aerox/input/InputConsumer.hpp>

namespace aerox::input {

void InputConsumer::Init() {
}

bool InputConsumer::CanConsumeInput() {
  return true;
}

bool InputConsumer::ReceiveAxis(const EInputAxis axis,
                                const std::shared_ptr<AxisInputEvent> &event) {
  if (_axisHandlers.contains(axis)) {

    if (_axisHandlers[axis]->ForEach(
        [event](const axisHandler &handler) {
          return handler(event);
        })) {
      return true;
    }
  }

  return false;
}

bool InputConsumer::ReceiveKeyDown(const bindableKey &key,
                                   const std::shared_ptr<KeyInputEvent> &
                                   event) {
  if (_keyHandlers->ForEach(
      [event](const keyHandlerPair &handlers) {
        if(!handlers.second.has_value()) return false;
          return handlers.second.value()(event);
      })) {
    return true;
  }

  if (_keySpecificHandlers.contains(key)) {

    if (_keySpecificHandlers[key]->ForEach(
        [event](const keyHandlerPair &handlers) {
          if(!handlers.second.has_value()) return false;
          return handlers.second.value()(event);
        })) {
      return true;
    }
  }

  return false;
}

bool InputConsumer::ReceiveKeyUp(const bindableKey &key,
                                 const std::shared_ptr<KeyInputEvent> &
                                 event) {
  if (_keyHandlers->ForEach(
      [event](const keyHandlerPair &handlers) {
        if(!handlers.first.has_value()) return false;
          return handlers.first.value()(event);
      })) {
    return true;
  }

  if (_keySpecificHandlers.contains(key)) {

    if (_keySpecificHandlers[key]->ForEach(
        [event](const keyHandlerPair &handlers) {
          if(!handlers.first.has_value()) return false;
          return handlers.first.value()(event);
        })) {
      return true;
    }
  }

  return false;
}

unsubscribe
InputConsumer::BindAxis(EInputAxis axis, const axisHandler &handler) {
  if (!_axisHandlers.contains(axis)) {
    _axisHandlers.emplace(axis, std::make_shared<AxisInputHandlers>());
  }

  return _axisHandlers[axis]->Add(handler);
}

unsubscribe InputConsumer::BindKey(const std::optional<keyHandler> &down, const std::optional<keyHandler> &up) const {
  return _keyHandlers->Add({up, down});
}

unsubscribe InputConsumer::BindKey(const bindableKey &key,
                                   const std::optional<keyHandler> &down, const std::optional<keyHandler> &up) {
  if (!_keySpecificHandlers.contains(key)) {
    _keySpecificHandlers.emplace(key, std::make_shared<KeyInputHandlers>());
  }

  return _keySpecificHandlers[key]->Add({up, down});
}

// unsubscribe InputConsumer::BindMouse(const mouseHandler &down,const mouseHandler &up) const {
//   return _mouseHandlers->Add({up, down});
// }
//
// unsubscribe InputConsumer::BindMouse(const window::EMouseButton &button,
//                                      const mouseHandler &down,const mouseHandler &up) {
//   if (!_mouseSpecificHandlers.contains(button)) {
//     _mouseSpecificHandlers.emplace(
//         button, std::make_shared<MouseInputHandlers>());
//   }
//
//   return _mouseSpecificHandlers[button]->Add({up, down});
// }


}
