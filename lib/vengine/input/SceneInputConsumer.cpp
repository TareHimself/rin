#include <vengine/input/SceneInputConsumer.hpp>
#include "vengine/Engine.hpp"

namespace vengine::input {


void SceneInputConsumer::Init(InputSubsystem * outer) {
  InputConsumer::Init(outer);
  AddCleanup(outer->GetOuter()->onInputModeChanged,outer->GetOuter()->onInputModeChanged.Bind([this](EInputMode _, const EInputMode newMode) {
      if(newMode == EInputMode::UiOnly) {
        _bShouldProcessInput = false;
        
      } else {
        _bShouldProcessInput = true;
      }
    }));
}

void SceneInputConsumer::BeforeDestroy() {
  InputConsumer::BeforeDestroy();
}

bool SceneInputConsumer::CanConsumeInput() {
  return _bShouldProcessInput;
}

}
