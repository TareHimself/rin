#pragma once
#include "InputManager.hpp"

namespace vengine::input {
class SceneInputManager : public InputManager {
  bool _bShouldProcessInput = true;
public:
  void Init(Engine *outer) override;
  void HandleDestroy() override;
  void ProcessSdlEvent(const SDL_Event &e) override;
};
}
