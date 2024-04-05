#pragma once
#include "InputConsumer.hpp"

namespace aerox::input {
class SceneInputConsumer : public InputConsumer {
  bool _bShouldProcessInput = true;
public:
  void Init() override;
  bool CanConsumeInput() override;
};
}
