#pragma once
#include "vengine/widget/Canvas.hpp"
#include "vengine/widget/GeometryWidget.hpp"
#include "vengine/widget/Widget.hpp"

namespace vengine::widget {
class Viewport : public Canvas {
  vk::Sampler _sampler;
  Managed<drawing::MaterialInstance> _shader;
  std::optional<uint64_t> _rootResizeHandle;
public:
  void Init(WidgetSubsystem *outer) override;

  void OnPaint(WidgetFrameData *frameData, const Rect &clip) override;

  void BeforeDestroy() override;

  void OnAddedToScreen() override;

  void OnRemovedFromScreen() override;
};

}
