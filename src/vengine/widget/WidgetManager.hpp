#pragma once
#include "vengine/Object.hpp"
#include "vengine/EngineSubsystem.hpp"
#include "vengine/drawing/Drawable.hpp"
#include "vengine/drawing/MaterialInstance.hpp"


namespace vengine::widget {
class Widget;

class WidgetManager : public EngineSubsystem, drawing::Drawable {
  Array<Widget *> _topLevelWidgets;
  vk::Extent2D _windowSize;
  drawing::MaterialInstance * _defaultWidgetShader = nullptr;
  std::optional<drawing::AllocatedBuffer> _uiGlobalBuffer;
  vk::DescriptorSetLayout _widgetDescriptorLayout;
public:
  void Init(Engine *outer) override;

  void HandleDestroy() override;

  String GetName() const override;

  void Draw(drawing::Drawer *drawer, drawing::RawFrameData *frameData) override;

  template <typename T,typename... Args>
  T * AddWidget(Args &&... args);

  void InitWidget(Widget * widget);

  drawing::MaterialInstance * GetDefaultRectMaterial() const;

  vk::DescriptorSetLayout GetWidgetDescriptorLayout() const;
};

template <typename T, typename ... Args> T * WidgetManager::AddWidget(
    Args &&... args) {
  auto rawObj = newObject<T>(args...);
  const auto obj = reinterpret_cast<Widget *>(rawObj);
  InitWidget(obj);
  return rawObj;
}
}
