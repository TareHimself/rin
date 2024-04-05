#include "aerox/widgets/Canvas.hpp"

#include "aerox/Engine.hpp"
#include "aerox/widgets/WidgetSubsystem.hpp"

namespace aerox::widgets {

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
