#pragma once
#include "vengine/Object.hpp"
#include "vengine/EngineSubsystem.hpp"
#include "vengine/drawing/Drawable.hpp"
#include "vengine/drawing/MaterialInstance.hpp"
#include "generated/widget/WidgetSubsystem.reflect.hpp"

namespace vengine {
namespace widget {
class Panel;
}
}

namespace vengine {
namespace widget {
class Font;
}
}

namespace vengine::widget {
class Widget;

RCLASS()
class WidgetSubsystem : public EngineSubsystem, public drawing::Drawable {
  Array<Managed<Widget>> _topLevelWidgets;
  Managed<Panel> _canvas;
  vk::Extent2D _drawSize;
  Managed<drawing::AllocatedBuffer> _uiGlobalBuffer;
  std::list<Ref<Widget>> _lastHoverList;
public:
  Ref<drawing::AllocatedBuffer> GetGlobalBuffer() const;
  void Init(Engine * outer) override;

  void BeforeDestroy() override;

  String GetName() const override;

  void Draw(drawing::RawFrameData *frameData) override;

  void AddToScreen(const Managed<Widget>& widget);

  void HandleLastHovered(const std::shared_ptr<window::MouseMovedEvent>& event);

  template <typename T,typename... Args>
  Managed<T> CreateWidget(Args &&... args);

  void InitWidget(const Managed<Widget> &widget);

  vk::Extent2D GetDrawSize() const;
};

template <typename T, typename ... Args> Managed<T> WidgetSubsystem::CreateWidget(
    Args &&... args) {
  auto rawObj = newManagedObject<T>(std::forward<Args>(args)...);
  const auto obj = rawObj.template Cast<Widget>();
  InitWidget(obj);
  return rawObj;
}

REFLECT_IMPLEMENT(WidgetSubsystem)
}
