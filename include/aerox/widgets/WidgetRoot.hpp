#pragma once
#include <aerox/TOwnedBy.hpp>
#include "types.hpp"
#include "aerox/window/Window.hpp"
#include "aerox/widgets/WidgetSubsystem.hpp"


namespace aerox::widgets {
class WidgetRoot : public TOwnedBy<WidgetSubsystem,const std::weak_ptr<window::Window>&> {
  std::weak_ptr<window::Window> _window;
  std::shared_ptr<drawing::AllocatedImage> _drawImage;
  Array<std::shared_ptr<Widget>> _widgets;
  Size2D _size{};
  std::shared_ptr<drawing::AllocatedBuffer> _uiGlobalBuffer;
  std::list<std::weak_ptr<Widget>> _lastHoverList;
  std::weak_ptr<drawing::WindowDrawer> _windowDrawer;
public:

  TDelegate<> onResize;
  
  std::weak_ptr<drawing::AllocatedBuffer> GetGlobalBuffer() const;
  void OnInit(WidgetSubsystem * subsystem, const std::weak_ptr<window::Window> &window) override;

  virtual void Draw(drawing::RawFrameData * frame);
  void HandleLastHovered(const std::shared_ptr<window::MouseMovedEvent>& event);

  std::weak_ptr<drawing::WindowDrawer> GetWindowDrawer() const;
  void CreateDrawImage();
  Size2D GetDrawSize() const;

  std::weak_ptr<drawing::AllocatedImage> GetRenderTarget() const;

  void Add(const std::shared_ptr<Widget>& widget);

  Array<std::weak_ptr<Widget>> GetWidgets() const;

  std::weak_ptr<Widget> GetWidget(size_t index);

  virtual void Tick(float deltaTime);
};
}

