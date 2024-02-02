#include <vengine/input/SceneInputConsumer.hpp>
#include "vengine/Engine.hpp"

namespace vengine::input {


void SceneInputConsumer::Init(InputManager * outer) {
  InputConsumer::Init(outer);
  AddCleanup(outer->GetOuter()->onInputModeChanged.On([=](EInputMode _, const EInputMode newMode) {
      if(newMode == EInputMode::UiOnly) {
        _bShouldProcessInput = false;
        
      } else {
        _bShouldProcessInput = true;
      }
    }));
}

void SceneInputConsumer::HandleDestroy() {
  InputConsumer::HandleDestroy();
}

bool SceneInputConsumer::CanConsumeInput() {
  return _bShouldProcessInput;
}

}
