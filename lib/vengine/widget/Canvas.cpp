#include "vengine/widget/Canvas.hpp"

#include "vengine/Engine.hpp"
#include "vengine/widget/WidgetSubsystem.hpp"

namespace vengine::widget {

void Canvas::Draw(WidgetFrameData *frameData, const DrawInfo info) {
  if(!GetDrawRect().HasIntersection(info.clip)) {
    return;
  }
  
  OnPaint(frameData,info.clip);
}

void Canvas::SetSize(const Size2D &size) {
  _size = size;
  InvalidateCachedSize();
}

Size2D Canvas::ComputeDesiredSize() const {
  return _size;
}
}
