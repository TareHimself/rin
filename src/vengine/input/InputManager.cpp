#include "InputManager.hpp"

#include <utility>

#include "KeyInputEvent.hpp"

#include <ranges>

namespace vengine::input {
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
  
  if(deltaX > 0 || deltaX < 0) {
    if(_axisHandlers.contains(EInputAxis::MouseX)) {
      const AxisInputEvent axisEvent(EInputAxis::MouseX,deltaX);
    
      for(const auto &val : _axisHandlers[EInputAxis::MouseX] |
                            std::views::values) {
        if(val(axisEvent)) {
          return true;
        }
      }
    }
  }

  if(deltaY > 0 || deltaY < 0) {
    if(_axisHandlers.contains(EInputAxis::MouseY)) {
      const AxisInputEvent axisEvent(EInputAxis::MouseY,deltaY);
    
      for(const auto &val : _axisHandlers[EInputAxis::MouseY] |
                            std::views::values) {
        if(val(axisEvent)) {
          return true;
        }
      }
    }
  }

  for(const auto manager : _managers) {
    if(manager->ReceiveMouseMovedEvent(event)) {
      return true;
    }
  }

  return false;
}

bool InputManager::ReceiveKeyPressedEvent(const SDL_KeyboardEvent &event) {
  const auto keyCode = static_cast<SDL_KeyCode>(event.keysym.sym);
  const KeyInputEvent keyEvent(event);
  
  for(const auto &val : _keyHandlers | std::views::values) {
    if(val.first(keyEvent)) {
      break;
    }
  }
  
  if(_keySpecificHandlers.contains(keyCode)) {
    
    for(const auto &handlers : _keySpecificHandlers[keyCode] | std::views::values) {
      if(handlers.first(keyEvent)) {
        break;
      }
    }
  }

  for(const auto manager : _managers) {
    if(manager->ReceiveKeyPressedEvent(event)) {
      return true;
    }
  }

  return false;
}

bool InputManager::ReceiveKeyReleasedEvent(const SDL_KeyboardEvent &event) {
  const auto keyCode = static_cast<SDL_KeyCode>(event.keysym.sym);
  const KeyInputEvent keyEvent(event);
  
  for(const auto &val : _keyHandlers | std::views::values) {
    if(val.second(keyEvent)) {
      break;
    }
  }
  
  if(_keySpecificHandlers.contains(keyCode)) {
    
    for(const auto &handlers : _keySpecificHandlers[keyCode] | std::views::values) {
      if(handlers.second(keyEvent)) {
        break;
      }
    }
  }

  for(const auto manager : _managers) {
    if(manager->ReceiveKeyReleasedEvent(event)) {
      return true;
    }
  }

  return false;
}

unsubscribe InputManager::BindKey(keyEventHandler pressed,
                                  keyEventHandler released) {
  auto id = ++_pressHandlerIds;
  _keyHandlers.emplace(id,std::pair{std::move(pressed),std::move(released)});

  return [this,id] {
    if(_keyHandlers.contains(id)) {
      _keyHandlers.erase(_keyHandlers.find(id));
    }
  };
}


unsubscribe InputManager::BindKey(SDL_KeyCode key,keyEventHandler pressed,keyEventHandler released) {
  auto id = ++_pressHandlerIds;
  
  if(!_keySpecificHandlers.contains(key)) {
    keySubscriptions initial;
    
    initial.emplace(id,std::pair{std::move(pressed),std::move(released)});
    
    _keySpecificHandlers.emplace(key,std::move(initial));
    
  } else {
    _keySpecificHandlers[key].emplace(id,std::pair{std::move(pressed),std::move(released)});
  }

  return [this,key,id] {
    if(auto handlers = _keySpecificHandlers[key]; handlers.contains(id)) {
      handlers.erase(handlers.find(id));
    }
  };
}

unsubscribe InputManager::Subscribe(InputManager *manager) {
  _managers.Push(manager);
  return [=] {
    for(auto i = 0; i < _managers.size(); i++) {
      if(_managers[i] == manager) {
        _managers.Remove(i);
        break;
      }
    }
  };
}

unsubscribe InputManager::OnAxis(EInputAxis axis, axisEventHandler handler) {
  auto id = ++_axisHandlerIds;
  
  if(!_axisHandlers.contains(axis)) {
    std::map<uint64_t,axisEventHandler> initial;
    
    initial.emplace(id,std::move(handler));
    
    _axisHandlers.emplace(axis,std::move(initial));
    
  } else {
    _axisHandlers[axis].emplace(id,std::move(handler));
  }

  return [this,axis,id] {
    auto handlers = _axisHandlers[axis];
    if(handlers.contains(id)) {
      handlers.erase(handlers.find(id));
    }
  };
}
}
