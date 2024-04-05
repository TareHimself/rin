#include <aerox/input/SceneInputConsumer.hpp>
#include "aerox/Engine.hpp"

namespace aerox::input {


void SceneInputConsumer::Init() {
  InputConsumer::Init();
  AddCleanup(Engine::Get()->onInputModeChanged->BindFunction(
      [this](EInputMode _, const EInputMode newMode) {
        if (newMode == EInputMode::UiOnly) {
          _bShouldProcessInput = false;

        } else {
          _bShouldProcessInput = true;
        }
      }));
}

bool SceneInputConsumer::CanConsumeInput() {
  return _bShouldProcessInput;
}

}
