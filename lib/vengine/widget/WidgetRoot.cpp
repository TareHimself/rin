#include "vengine/widget/WidgetRoot.hpp"
#include "vengine/Engine.hpp"
#include "vengine/drawing/WindowDrawer.hpp"
#include "vengine/widget/Widget.hpp"
#include "vengine/drawing/DrawingSubsystem.hpp"

#include <glm/ext/matrix_clip_space.hpp>

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
  

    _size = _window.Reserve()->GetPixelSize(); //engine->GetMainWindowSize();

    _uiGlobalBuffer = drawer->GetAllocator().Reserve()->CreateUniformCpuGpuBuffer(
        sizeof(UiGlobalBuffer), false);

    CreateDrawImage();

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

    AddCleanup(_windowDrawer.Reserve()->onResizeScenes,_windowDrawer.Reserve()->onResizeScenes.Bind([this]{
      _size = _window.Reserve()->GetPixelSize();
                       CreateDrawImage();
      onResize();
    }));
  }
}

void WidgetRoot::Init(const Ref<window::Window> &window,
                      WidgetSubsystem *outer) {
  _window = window;
  Init(outer);
}

void WidgetRoot::Draw(drawing::RawFrameData *frame) {
  if (!_widgets.empty()) {
    drawing::DrawingSubsystem::TransitionImage(*frame->GetCmd(),_drawImage->image,vk::ImageLayout::eUndefined,vk::ImageLayout::eColorAttachmentOptimal);
    const vk::Extent2D drawExtent = frame->GetWindowDrawer()->GetSwapchainExtent();
    vk::ClearValue colorClear;
    colorClear.setColor({0,0,0,0});
    
    const auto colorAttachment = drawing::DrawingSubsystem::MakeRenderingAttachment(_drawImage->view, vk::ImageLayout::eColorAttachmentOptimal,colorClear);
      
    auto renderingInfo = drawing::DrawingSubsystem::MakeRenderingInfo(drawExtent);
    renderingInfo.setColorAttachments(colorAttachment);
    
      
    const auto cmd = frame->GetCmd();
      
    cmd->beginRendering(renderingInfo);
      
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
      
    cmd->endRendering();
    drawing::DrawingSubsystem::TransitionImage(*frame->GetCmd(),_drawImage->image,vk::ImageLayout::eColorAttachmentOptimal,vk::ImageLayout::eTransferSrcOptimal);
    
  } else {
    drawing::DrawingSubsystem::TransitionImage(*frame->GetCmd(),_drawImage->image,vk::ImageLayout::eUndefined,vk::ImageLayout::eTransferSrcOptimal);
    
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

Ref<drawing::WindowDrawer> WidgetRoot::GetWindowDrawer() const {
  return _windowDrawer;
}

void WidgetRoot::CreateDrawImage() {
  _drawImage.Clear();
  
  const auto swapchainExtent = _windowDrawer.Reserve()->
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
  auto drawer = GetOuter()->GetEngine()->GetDrawingSubsystem().Reserve();
  _drawImage = drawer->GetAllocator().
                                                 Reserve()->AllocateImage(
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

Ref<drawing::AllocatedImage> WidgetRoot::GetRenderTarget() const {
  return _drawImage;
}

void WidgetRoot::Add(const Managed<Widget> &widget) {
  _widgets.push(widget);
  widget->NotifyRootChanged(this);
  widget->NotifyAddedToScreen();
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
