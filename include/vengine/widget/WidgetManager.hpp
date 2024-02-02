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
  Array<Pointer<Widget>> _topLevelWidgets;
  vk::Extent2D _windowSize;
  Pointer<drawing::MaterialInstance> _defaultWidgetMat;
  Pointer<drawing::AllocatedBuffer> _uiGlobalBuffer;
public:
  WeakPointer<drawing::AllocatedBuffer> GetGlobalBuffer() const;
  void Init(Engine * outer) override;

  void HandleDestroy() override;

  String GetName() const override;

  void Draw(drawing::Drawer *drawer, drawing::RawFrameData *frameData) override;

  template <typename T,typename... Args>
  WeakPointer<T> AddWidget(Args &&... args);

  void InitWidget(const Pointer<Widget> &widget);

  WeakPointer<drawing::MaterialInstance> GetDefaultMaterial() const;
};

template <typename T, typename ... Args> WeakPointer<T> WidgetManager::AddWidget(
    Args &&... args) {
  auto rawObj = newSharedObject<T>(args...);
  const auto obj = rawObj.template Cast<Widget>();
  InitWidget(obj);
  return rawObj;
}
}
