#include "aerox/widgets/WidgetRoot.hpp"
#include "aerox/Engine.hpp"
#include "aerox/drawing/WindowDrawer.hpp"
#include "aerox/widgets/Widget.hpp"
#include "aerox/drawing/DrawingSubsystem.hpp"

#include <glm/ext/matrix_clip_space.hpp>

namespace aerox::widgets {

std::weak_ptr<drawing::AllocatedBuffer> WidgetRoot::GetGlobalBuffer() const {
  return _uiGlobalBuffer;
}

void WidgetRoot::OnInit(WidgetSubsystem *subsystem,
                        const std::weak_ptr<window::Window> &window) {
  TOwnedBy::OnInit(
      subsystem, window);
  _window = window;

  const auto windowRef = _window.lock();

  const auto engine = Engine::Get();

  const auto drawer = engine->GetDrawingSubsystem().lock();

  if (const auto windowDrawer = drawer->GetWindowDrawer(_window).lock()) {
    _windowDrawer = windowDrawer;

    AddCleanup(windowDrawer->onDrawUi->BindFunction(
        [this](drawing::RawFrameData *rawFrame) {
          Draw(rawFrame);
        }));

    _size = _window.lock()->GetPixelSize(); //engine->GetMainWindowSize();

    _uiGlobalBuffer = drawer->GetAllocator().lock()->CreateUniformCpuGpuBuffer(
        sizeof(UiGlobalBuffer), false);

    CreateDrawImage();

    AddCleanup(windowRef->onMouseDown->BindFunction([this](
        const std::shared_ptr<window::MouseButtonEvent> &
        event) {
          for (const auto &child : _widgets) {
            if (child->ReceiveMouseDown(event)) {
              return;
            }
          }
        }));

    AddCleanup(windowRef->onMouseMoved->BindFunction([this](
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

    AddCleanup(windowRef->onMouseMoved->BindFunction([this](
        const std::shared_ptr<window::MouseMovedEvent> &
        event) {
          for (auto &child : _widgets) {
            if (child->ReceiveMouseMove(event)) {
              return;
            }
          }
        }));

    AddCleanup(windowRef->onScroll->BindFunction([this](
        const std::shared_ptr<window::ScrollEvent> &
        event) {
          const Point2D point = {event->x, event->y};

          for (auto &child : _widgets) {
            if (child->ReceiveScroll(event)) {
              return;
            }
          }
        }));

    AddCleanup(_windowDrawer.lock()->onResizeScenes->BindFunction([this] {
      _size = _window.lock()->GetPixelSize();
      CreateDrawImage();
      onResize.Execute();
    }));
  }
}

void WidgetRoot::Draw(drawing::RawFrameData *frame) {
  if (!_widgets.empty()) {
    drawing::DrawingSubsystem::TransitionImage(*frame->GetCmd(),
                                               _drawImage->image,
                                               vk::ImageLayout::eUndefined,
                                               vk::ImageLayout::eColorAttachmentOptimal);
    const vk::Extent2D drawExtent = frame->GetWindowDrawer()->
                                           GetSwapchainExtent();
    vk::ClearValue colorClear;
    colorClear.setColor({0, 0, 0, 0});

    const auto colorAttachment =
        drawing::DrawingSubsystem::MakeRenderingAttachment(
            _drawImage->view, vk::ImageLayout::eColorAttachmentOptimal,
            colorClear);

    auto renderingInfo = drawing::DrawingSubsystem::MakeRenderingInfo(
        drawExtent);
    renderingInfo.setColorAttachments(colorAttachment);

    const auto cmd = frame->GetCmd();

    cmd->beginRendering(renderingInfo);

    UiGlobalBuffer uiGb;
    uiGb.viewport = glm::vec4{0, 0, _size.width, _size.height};
    uiGb.time.x = Engine::Get()->GetEngineTimeSeconds();

    _uiGlobalBuffer->Write(uiGb);

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

    cmd->endRendering();
    drawing::DrawingSubsystem::TransitionImage(*frame->GetCmd(),
                                               _drawImage->image,
                                               vk::ImageLayout::eColorAttachmentOptimal,
                                               vk::ImageLayout::eTransferSrcOptimal);

  } else {
    drawing::DrawingSubsystem::TransitionImage(*frame->GetCmd(),
                                               _drawImage->image,
                                               vk::ImageLayout::eUndefined,
                                               vk::ImageLayout::eTransferSrcOptimal);

  }
}

void WidgetRoot::HandleLastHovered(
    const std::shared_ptr<window::MouseMovedEvent> &event) {
  const Point2D point = {event->x, event->y};
  for (auto &widget : _lastHoverList) {
    if (const auto ref = widget.lock();
      ref && !ref->GetDrawRect().IsWithin(point)) {
      ref->OnMouseLeave(event);
    }
  }

  _lastHoverList.clear();
}

std::weak_ptr<drawing::WindowDrawer> WidgetRoot::GetWindowDrawer() const {
  return _windowDrawer;
}

void WidgetRoot::CreateDrawImage() {
  _drawImage.reset();

  const auto swapchainExtent = _windowDrawer.lock()->
                                             GetSwapchainExtent();

  const auto imageExtent = vk::Extent3D{
      swapchainExtent.width, swapchainExtent.height, 1};

  vk::ImageUsageFlags drawImageUsages;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferSrc;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferDst;
  drawImageUsages |= vk::ImageUsageFlagBits::eColorAttachment;
  // drawImageUsages |= vk::ImageUsageFlagBits::eSampled;

  auto drawCreateInfo = drawing::DrawingSubsystem::MakeImageCreateInfo(
      vk::Format::eR16G16B16A16Sfloat,
      imageExtent,
      drawImageUsages);
  auto drawer = Engine::Get()->GetDrawingSubsystem().lock();
  _drawImage = drawer->GetAllocator().
                       lock()->AllocateImage(
                           drawCreateInfo,
                           VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                           vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto drawViewInfo = vk::ImageViewCreateInfo({}, _drawImage->image,
                                                    vk::ImageViewType::e2D,
                                                    _drawImage->format, {},
                                                    {vk::ImageAspectFlagBits::eColor,
                                                      0,
                                                      1, 0, 1});

  _drawImage->view = drawer->GetVirtualDevice().createImageView(
      drawViewInfo);

}

Size2D WidgetRoot::GetDrawSize() const {
  return _size;
}

std::weak_ptr<drawing::AllocatedImage> WidgetRoot::GetRenderTarget() const {
  return _drawImage;
}

void WidgetRoot::Add(const std::shared_ptr<Widget> &widget) {
  _widgets.push(widget);
  widget->NotifyRootChanged(utils::cast<WidgetRoot>(this->shared_from_this()));
  widget->NotifyAddedToScreen();
}

Array<std::weak_ptr<Widget>> WidgetRoot::GetWidgets() const {
  return _widgets.map<std::weak_ptr<Widget>>([](size_t idx, const std::shared_ptr<Widget> &item) {
    return item;
  });
}

std::weak_ptr<Widget> WidgetRoot::GetWidget(size_t index) {
  if (_widgets.size() > index) {
    return _widgets.at(index);
  }

  return {};
}

void WidgetRoot::Tick(float deltaTime) {
  for (auto &widget : _widgets.clone()) {
    if (widget) {
      widget->Tick(deltaTime);
    }
  }
}
}
