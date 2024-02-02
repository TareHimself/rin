#pragma once
#include "InputConsumer.hpp"

namespace vengine::input {
class SceneInputConsumer : public InputConsumer {
  bool _bShouldProcessInput = true;
public:
  void Init(InputManager * outer) override;
  void HandleDestroy() override;
  bool CanConsumeInput() override;
};
}
