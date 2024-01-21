#include "SceneInputManager.hpp"

#include "vengine/Engine.hpp"

namespace vengine::input {

void SceneInputManager::Init(Engine *outer) {
  InputManager::Init(outer);
  AddCleanup(GetOuter()->GetInputManager()->Subscribe(this));
  AddCleanup(outer->onInputModeChanged.On([=](EInputMode _, const EInputMode newMode) {
    if(newMode == EInputMode::UiOnly) {
      _bShouldProcessInput = false;
      
    } else {
      _bShouldProcessInput = true;
    }
  }));
}

void SceneInputManager::HandleDestroy() {
  InputManager::HandleDestroy();
}

void SceneInputManager::ProcessSdlEvent(const SDL_Event &e) {
  
  InputManager::ProcessSdlEvent(e);
}
}
