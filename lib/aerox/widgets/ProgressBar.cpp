#include "aerox/widgets/ProgressBar.hpp"

namespace aerox::widgets {

void ProgressBar::SetProgress(float progress) {
  _progress = progress;
}

float ProgressBar::GetProgress() const {
  return _progress;
}

}
