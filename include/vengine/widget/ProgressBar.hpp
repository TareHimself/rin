#pragma once
#include "Widget.hpp"
#include "vengine/drawing/MaterialInstance.hpp"

namespace vengine::widget {
class ProgressBar : public Widget {
  float _progress = 0.0f;
public:
  void SetProgress(float progress);
  float GetProgress() const;
};
}
