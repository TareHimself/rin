#pragma once
#include "InputConsumer.hpp"

namespace vengine::input {
class SceneInputConsumer : public InputConsumer {
  bool _bShouldProcessInput = true;
public:
  void Init(InputSubsystem * outer) override;
  void BeforeDestroy() override;
  bool CanConsumeInput() override;
};
}
