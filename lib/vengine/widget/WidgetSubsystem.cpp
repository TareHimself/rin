#include <vengine/widget/WidgetSubsystem.hpp>
#include <vengine/widget/Widget.hpp>
#include <vengine/Engine.hpp>
#include <vengine/assets/AssetSubsystem.hpp>
#include <vengine/drawing/DrawingSubsystem.hpp>
#include <vengine/drawing/MaterialBuilder.hpp>
#include <vengine/io/io.hpp>

namespace vengine::widget {
Ref<drawing::AllocatedBuffer> WidgetSubsystem::GetGlobalBuffer() const {
  return _uiGlobalBuffer;
}

void WidgetSubsystem::Init(Engine *outer) {
  EngineSubsystem::Init(outer);
  auto window = outer->GetWindow().Reserve();

  AddCleanup(window->onMouseDown ,window->onMouseDown.Bind([this](
      const std::shared_ptr<window::MouseButtonEvent> &
      event) {
        for (auto &child : _topLevelWidgets) {
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

        for (auto &child : _topLevelWidgets) {
          // Only the first item will receive hover events
          if (child->IsInBounds(point)) {
            _lastHoverList.push_front(child);
            child->ReceiveMouseEnter(event, _lastHoverList);
            break;
          }
        }
        return;
      }));

  const auto engine = GetEngine();

  engine->onWindowSizeChanged.Bind([this](vk::Extent2D size) {
    log::engine->info("WIDGETS SYSTEM, WINDOW SIZE CHANGED");
    _drawSize = size;
  });

  _drawSize = engine->GetWindowExtent();
  const auto drawer = engine->GetDrawingSubsystem().Reserve();

  _uiGlobalBuffer = drawer->GetAllocator().Reserve()->CreateUniformCpuGpuBuffer(
      sizeof(UiGlobalBuffer), false);
}

void WidgetSubsystem::BeforeDestroy() {
  EngineSubsystem::BeforeDestroy();

  GetOuter()->GetDrawingSubsystem().Reserve()->WaitDeviceIdle();

  _uiGlobalBuffer.Clear();

  _topLevelWidgets.clear();
}

String WidgetSubsystem::GetName() const {
  return "widgets";
}

void WidgetSubsystem::Draw(
    drawing::RawFrameData *frameData) {
  if (!_topLevelWidgets.empty()) {

    UiGlobalBuffer uiGb;
    uiGb.viewport = glm::vec4{0, 0, _drawSize.width, _drawSize.height};

    const auto mappedData = _uiGlobalBuffer->GetMappedData();
    const auto uiGlobalBuffer = static_cast<UiGlobalBuffer *>(mappedData);
    *uiGlobalBuffer = uiGb;

    DrawInfo myInfo;
    myInfo.parent = nullptr;
    myInfo.drawRect.x = 0;
    myInfo.drawRect.y = 0;
    myInfo.drawRect.width = static_cast<float>(_drawSize.width);
    myInfo.drawRect.height = static_cast<float>(_drawSize.height);

    drawing::SimpleFrameData wFrameData(frameData);

    for (const auto &widget : _topLevelWidgets.clone()) {
      widget->Draw(&wFrameData, myInfo);
    }
  }
}

void WidgetSubsystem::AddToScreen(const Managed<Widget> &widget) {
  _topLevelWidgets.push(widget);
}

void WidgetSubsystem::HandleLastHovered(
    const std::shared_ptr<window::MouseMovedEvent> &event) {
  const Point2D point = {event->x, event->y};
  for (auto &widget : _lastHoverList) {
    if (auto ref = widget.Reserve(); !ref->IsInBounds(point)) {
      ref->OnMouseLeave(event);
    }
  }

  _lastHoverList.clear();
}

void WidgetSubsystem::InitWidget(const Managed<Widget> &widget) {
  widget->Init(this);
}

vk::Extent2D WidgetSubsystem::GetDrawSize() const {
  return _drawSize;
}
}
