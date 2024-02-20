#pragma once
#include "GeometryWidget.hpp"
#include "Widget.hpp"
#include "vengine/drawing/MaterialInstance.hpp"

namespace vengine::widget {
class Image : public GeometryWidget {
  Managed<drawing::Texture2D> _image;
  Managed<drawing::MaterialInstance> _imageMat;
public:
  void Init(WidgetSubsystem * outer) override;
  void SetTexture(const Managed<drawing::Texture2D> &image);
  Ref<drawing::Texture2D> GetTexture() const;

  void Draw(WidgetFrameData *frameData, DrawInfo info) override;

  void BeforeDestroy() override;

  Size2D ComputeDesiredSize() const override;
};
}
