#include "vengine/widget/Canvas.hpp"

#include "vengine/Engine.hpp"
#include "vengine/widget/WidgetSubsystem.hpp"

namespace vengine::widget {

void Canvas::Draw(drawing::SimpleFrameData *frameData, DrawInfo info) {
  const auto myDrawRect = CalculateFinalRect(info.drawRect);

  OnPaint(frameData,myDrawRect);
}

void Canvas::SetSize(const Size2D &size) {
  _size = size;
  InvalidateCachedSize();
}

Size2D Canvas::ComputeDesiredSize() const {
  return _size;
}
}
