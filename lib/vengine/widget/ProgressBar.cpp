#include "vengine/widget/ProgressBar.hpp"

namespace vengine::widget {

void ProgressBar::SetProgress(float progress) {
  _progress = progress;
}

float ProgressBar::GetProgress() const {
  return _progress;
}

}
