#include "vengine/widget/WidgetRoot.hpp"
#include "vengine/Engine.hpp"
#include "vengine/drawing/WindowDrawer.hpp"
#include "vengine/widget/Widget.hpp"
#include "vengine/drawing/DrawingSubsystem.hpp"

namespace vengine::widget {

Ref<drawing::AllocatedBuffer> WidgetRoot::GetGlobalBuffer() const {
  return _uiGlobalBuffer;
}

void WidgetRoot::Init(WidgetSubsystem *outer) {
  Object<WidgetSubsystem>::Init(outer);
  auto window = _window.Reserve();

  const auto engine = Engine::Get();

  const auto drawer = engine->GetDrawingSubsystem().Reserve();

  if (auto windowDrawer = drawer->GetWindowDrawer(_window).Reserve()) {
    _windowDrawer = windowDrawer;

    AddCleanup(windowDrawer->onDrawUi, windowDrawer->onDrawUi.Bind(
                   [this](drawing::RawFrameData *rawFrame) {
                     Draw(rawFrame);
                   }));
  }

  _size = _window.Reserve()->GetPixelSize(); //engine->GetMainWindowSize();

  _uiGlobalBuffer = drawer->GetAllocator().Reserve()->CreateUniformCpuGpuBuffer(
      sizeof(UiGlobalBuffer), false);

  AddCleanup(window->onMouseDown, window->onMouseDown.Bind([this](
                 const std::shared_ptr<window::MouseButtonEvent> &
                 event) {
                   for (auto &child : _widgets) {
                     if (child->ReceiveMouseDown(event)) {
                       return;
                     }
                   }
                 }));

  AddCleanup(window->onMouseMoved, window->onMouseMoved.Bind([this](
                 const std::shared_ptr<window::MouseMovedEvent> &
                 event) {
                   HandleLastHovered(event);
                   const Point2D point = {event->x, event->y};

                   for (auto &child : _widgets) {
                     // Only the first item will receive hover events
                     if (child->GetDrawRect().IsWithin(point)) {
                       _lastHoverList.push_front(child);
                       child->ReceiveMouseEnter(event, _lastHoverList);
                       break;
                     }
                   }
                   return;
                 }));

  AddCleanup(window->onMouseMoved, window->onMouseMoved.Bind([this](
                 const std::shared_ptr<window::MouseMovedEvent> &
                 event) {
                   for (auto &child : _widgets) {
                     if (child->ReceiveMouseMove(event)) {
                       return;
                     }
                   }
                 }));

  AddCleanup(window->onScroll, window->onScroll.Bind([this](
                 const std::shared_ptr<window::ScrollEvent> &
                 event) {
                   const Point2D point = {event->x, event->y};

                   for (auto &child : _widgets) {
                     if (child->ReceiveScroll(event)) {
                       return;
                     }
                   }
                 }));

  AddCleanup(window->onResize, window->onResize.Bind(
                 [this](window::Window *win) {
                   _size = _window.Reserve()->GetPixelSize();
                 }));
}

void WidgetRoot::Init(const Ref<window::Window> &window,
                      WidgetSubsystem *outer) {
  _window = window;
  Init(outer);
}

void WidgetRoot::Draw(drawing::RawFrameData *frame) {
  if (!_widgets.empty()) {
    UiGlobalBuffer uiGb;
    uiGb.viewport = glm::vec4{0, 0, _size.width, _size.height};
    uiGb.time.x = Engine::Get()->GetEngineTimeSeconds();

    const auto mappedData = _uiGlobalBuffer->GetMappedData();
    const auto uiGlobalBuffer = static_cast<UiGlobalBuffer *>(mappedData);
    *uiGlobalBuffer = uiGb;

    const Size2D size = {static_cast<float>(_size.width),
                         static_cast<float>(_size.height)};

    DrawInfo myInfo;
    myInfo.parent = nullptr;
    myInfo.clip.SetSize(size);

    WidgetFrameData wFrameData(frame, this);

    for (auto &widget : _widgets.clone()) {
      if (widget) {
        widget->UpdateDrawRect(Rect().SetSize(size));
        widget->Draw(&wFrameData, myInfo);
      }
    }
  }
}

void WidgetRoot::HandleLastHovered(
    const std::shared_ptr<window::MouseMovedEvent> &event) {
  const Point2D point = {event->x, event->y};
  for (auto &widget : _lastHoverList) {
    if (auto ref = widget.Reserve(); !ref->GetDrawRect().IsWithin(point)) {
      ref->OnMouseLeave(event);
    }
  }

  _lastHoverList.clear();
}

Size2D WidgetRoot::GetDrawSize() const {
  return _size;
}

void WidgetRoot::Add(const Managed<Widget> &widget) {
  _widgets.push(widget);
  widget->SetParent(this);
}

Array<Ref<Widget>> WidgetRoot::GetWidgets() const {
  return _widgets.map<Ref<Widget>>([](size_t idx,const Managed<Widget>& item) {
    return item;
  });
}

Ref<Widget> WidgetRoot::GetWidget(size_t index) {
  if(_widgets.size() > index) {
    return _widgets.at(index);
  }

  return {};
}
}
