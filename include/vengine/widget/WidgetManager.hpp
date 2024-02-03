#pragma once
#include "vengine/Object.hpp"
#include "vengine/EngineSubsystem.hpp"
#include "vengine/drawing/Drawable.hpp"
#include "vengine/drawing/MaterialInstance.hpp"


namespace vengine {
namespace widget {
class Font;
}
}

namespace vengine::widget {
class Widget;

class WidgetManager : public EngineSubsystem,drawing::Drawable {
  Array<Ref<Widget>> _topLevelWidgets;
  vk::Extent2D _windowSize;
  Ref<drawing::MaterialInstance> _defaultWidgetMat;
  Ref<drawing::AllocatedBuffer> _uiGlobalBuffer;
public:
  WeakRef<drawing::AllocatedBuffer> GetGlobalBuffer() const;
  void Init(Engine * outer) override;

  void HandleDestroy() override;

  String GetName() const override;

  void Draw(drawing::Drawer *drawer, drawing::RawFrameData *frameData) override;

  template <typename T,typename... Args>
  WeakRef<T> AddWidget(Args &&... args);

  void InitWidget(const Ref<Widget> &widget);

  WeakRef<drawing::MaterialInstance> GetDefaultMaterial() const;
};

template <typename T, typename ... Args> WeakRef<T> WidgetManager::AddWidget(
    Args &&... args) {
  auto rawObj = newSharedObject<T>(args...);
  const auto obj = rawObj.template Cast<Widget>();
  InitWidget(obj);
  return rawObj;
}
}
