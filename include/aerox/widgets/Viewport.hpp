#pragma once
#include "aerox/widgets/Canvas.hpp"
#include "aerox/widgets/GeometryWidget.hpp"
#include "aerox/widgets/Widget.hpp"

namespace aerox::widgets {
class Viewport : public Canvas {
  vk::Sampler _sampler;
  std::shared_ptr<drawing::MaterialInstance> _shader;
  std::shared_ptr<TDelegateHandle<>> _rootResizeHandle;
  std::weak_ptr<scene::Scene> _scene;
public:
  void OnInit(WidgetSubsystem * ref) override;

  void OnPaint(WidgetFrameData *frameData, const Rect &clip) override;

  void OnDestroy() override;

  void OnAddedToScreen() override;

  void OnRemovedFromScreen() override;
  
};

}
