#pragma once
#include "vengine/drawing/MaterialInstance.hpp"
#include "vengine/widget/Widget.hpp"

namespace vengine::widget {

class Canvas : public Widget {
  Size2D _size{0,0};
public:
  void Draw(drawing::SimpleFrameData *frameData, DrawInfo info) override;
  virtual void OnPaint(drawing::SimpleFrameData *frameData,const Rect& clip) = 0;
  void SetSize(const Size2D& size);
  Size2D ComputeDesiredSize() const override;
};
}
