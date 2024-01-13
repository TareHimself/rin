#include "InputManager.hpp"

#include <utility>

#include "KeyInputEvent.hpp"

namespace vengine {
namespace input {
void InputManager::receiveKeyPressedEvent(const SDL_KeyboardEvent &event) {
  const auto keyCode = static_cast<SDL_KeyCode>(event.keysym.sym);
  
  if(keySpecificReleasedHandlers.contains(keyCode)) {

    const KeyInputEvent keyEvent(event);
    
    for(const auto &handler : keySpecificReleasedHandlers[keyCode]) {
      if(handler(keyEvent)) {
        break;
      }
    }
  }
}

void InputManager::receiveKeyReleasedEvent(const SDL_KeyboardEvent &event) {
  const auto keyCode = static_cast<SDL_KeyCode>(event.keysym.sym);
  
  if(keySpecificPressedHandlers.contains(keyCode)) {

    const KeyInputEvent keyEvent(event);
    
    for(const auto &handler : keySpecificPressedHandlers[keyCode]) {
      if(handler(keyEvent)) {
        break;
      }
    }
  }
}

unsubscribe InputManager::onPressed(SDL_KeyCode key,keyEventHandler handler) {
  auto fn = std::move(handler);
  if(!keySpecificPressedHandlers.contains(key)) {
    Array<keyEventHandler> initial;
    initial.push(fn);
    
    keySpecificPressedHandlers.insert({key,initial});
  } else {
    keySpecificPressedHandlers[key].push(fn);
  }

  return [this,key,&fn] {
    auto handlers = keySpecificPressedHandlers[key];
    const auto index = handlers.indexOf(fn,[](keyEventHandler& a,keyEventHandler& b) {
      return a.target<keyEventHandler>() == b.target<keyEventHandler>();
    });
    if(index > -1) {
      handlers.remove(index);
    }
  };
}

unsubscribe InputManager::onReleased(SDL_KeyCode key,keyEventHandler handler) {
  auto fn = std::move(handler);
  if(!keySpecificReleasedHandlers.contains(key)) {
    Array<keyEventHandler> initial;
    initial.push(fn);
    
    keySpecificReleasedHandlers.insert({key,initial});
  } else {
    keySpecificReleasedHandlers[key].push(fn);
  }

  return [this,key,&fn] {
    auto handlers = keySpecificReleasedHandlers[key];
    const auto index = handlers.indexOf(fn,[](keyEventHandler& a,keyEventHandler& b) {
      return a.target<keyEventHandler>() == b.target<keyEventHandler>();
    });
    if(index > -1) {
      handlers.remove(index);
    }
  };
}
}
}
