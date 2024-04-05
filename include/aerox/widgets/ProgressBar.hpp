#pragma once
#include "Widget.hpp"
#include "aerox/drawing/MaterialInstance.hpp"

namespace aerox::widgets {
class ProgressBar : public Widget {
  float _progress = 0.0f;
public:
  void SetProgress(float progress);
  float GetProgress() const;
};
}
