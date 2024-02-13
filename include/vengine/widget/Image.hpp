#pragma once
#include "Widget.hpp"
#include "vengine/drawing/MaterialInstance.hpp"

namespace vengine::widget {
class Image : public Widget{
  Managed<drawing::Texture> _image;
  Managed<drawing::MaterialInstance> _imageMat;
public:
  void Init(WidgetSubsystem * outer) override;
  void SetTexture(const Managed<drawing::Texture> &image);
  Ref<drawing::Texture> GetTexture() const;

  void Draw(drawing::SimpleFrameData *frameData, DrawInfo info) override;

  void BeforeDestroy() override;

  Size2D ComputeDesiredSize() const override;

  bool OnMouseDown(const std::shared_ptr<window::MouseButtonEvent> &event) override;

  void OnMouseEnter(const std::shared_ptr<window::MouseMovedEvent> &event) override;

  void OnMouseLeave(const std::shared_ptr<window::MouseMovedEvent> &event) override;
};
}
