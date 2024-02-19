#pragma once
#include "types.hpp"
#include "vengine/window/Window.hpp"
#include "vengine/widget/WidgetSubsystem.hpp"


namespace vengine::widget {
class WidgetRoot : public Object<WidgetSubsystem> {
  Ref<window::Window> _window;
  Array<Managed<Widget>> _widgets;
  Size2D _size{};
  Managed<drawing::AllocatedBuffer> _uiGlobalBuffer;
  std::list<Ref<Widget>> _lastHoverList;
  Ref<drawing::WindowDrawer> _windowDrawer;
public:

  Ref<drawing::AllocatedBuffer> GetGlobalBuffer() const;
  void Init(WidgetSubsystem *outer) override;
  virtual void Init(const Ref<window::Window>& window,WidgetSubsystem * outer);
  virtual void Draw(drawing::RawFrameData * frame);
  void HandleLastHovered(const std::shared_ptr<window::MouseMovedEvent>& event);
  
  Size2D GetDrawSize() const;

  void Add(const Managed<Widget>& widget);

  Array<Ref<Widget>> GetWidgets() const;

  Ref<Widget> GetWidget(size_t index);
};
}

