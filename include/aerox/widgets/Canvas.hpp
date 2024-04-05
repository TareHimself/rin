#pragma once
#include "GeometryWidget.hpp"
#include "aerox/drawing/MaterialInstance.hpp"
#include "aerox/widgets/Widget.hpp"

namespace aerox::widgets {

class Canvas : public GeometryWidget {
  Size2D _size{0,0};
public:
  void Draw(WidgetFrameData *frameData, DrawInfo info) override;
  virtual void OnPaint(WidgetFrameData *frameData, const Rect& clip) = 0;
  void SetSize(const Size2D& size);
  Size2D ComputeDesiredSize() const override;
};
}
