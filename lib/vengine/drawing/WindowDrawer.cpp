#include "vengine/drawing/WindowDrawer.hpp"

#include "VkBootstrap.h"
#include "vengine/window/Window.hpp"


namespace vengine::drawing {
void WindowDrawer::Init(DrawingSubsystem *outer) {
  Object<DrawingSubsystem>::Init(outer);
  const auto instance = GetOuter()->GetVulkanInstance();
  _surface = _window.Reserve()->CreateSurface(instance);
  const auto newSize = _window.Reserve()->GetPixelSize();
  _viewport.x = 0;
  _viewport.y = 0;
  _viewport.width = newSize.x;
  _viewport.height = newSize.y;
  _viewport.minDepth = 0.0f;
  _viewport.maxDepth = 1.0f;
}

void WindowDrawer::Init(const Ref<window::Window> &window,
    DrawingSubsystem *outer) {
  _window = window;
  Init(outer);
  
}

bool WindowDrawer::ShouldDraw() const {
  return IsInitialized()  && _isReady && !_isResizing;
}

void WindowDrawer::CreateResources() {
  
  AddCleanup(_window.Reserve()->onResize,_window.Reserve()->onResize.Bind([this](const window::Window* win) {
    _isResizing = true;
    const auto newSize = win->GetPixelSize();
    if(newSize == glm::uvec2{0,0}) {
      return;
    }
    _viewport.width = newSize.x;
    _viewport.height = newSize.y;
    GetOuter()->WaitDeviceIdle();
    DestroySwapchain();
    CreateSwapchain();
    _isResizing = false;
  }));
  
  CreateSwapchain();

  AddCleanup([this] {
    if (_swapchain != nullptr) {
      DestroySwapchain();
    }
  });

  

  InitFrames();
  InitSyncStructures();
  InitDescriptors();


  _isReady = true;
}

void WindowDrawer::CreateSwapchain() {
  auto extent = GetSwapchainExtent();
  auto device = GetVirtualDevice();
  auto gpu = GetOuter()->GetPhysicalDevice();
  vkb::SwapchainBuilder swapchainBuilder{gpu, device, _surface};

  _swapchainImageFormat = vk::Format::eB8G8R8A8Unorm;

  vkb::Swapchain vkbSwapchain = swapchainBuilder
                                .set_desired_format(
                                    vk::SurfaceFormatKHR(
                                        _swapchainImageFormat,
                                        vk::ColorSpaceKHR::eSrgbNonlinear))
                                .set_desired_present_mode(
                                    VK_PRESENT_MODE_FIFO_KHR)
                                .set_desired_extent(extent.width, extent.height)
                                .add_image_usage_flags(
                                    VK_IMAGE_USAGE_TRANSFER_DST_BIT)
                                .build()
                                .value();

  _swapchain = vkbSwapchain.swapchain;
  const auto vkbSwapchainImages = vkbSwapchain.get_images().value();
  for (const auto image : vkbSwapchainImages) {
    vk::Image im = image;
    _swapchainImages.push(im);
  }

  const auto vkbSwapchainViews = vkbSwapchain.get_image_views().value();
  for (const auto image : vkbSwapchainViews) {
    vk::ImageView im = image;
    _swapchainImageViews.push(im);
  }

  vk::ImageUsageFlags drawImageUsages;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferSrc;
  drawImageUsages |= vk::ImageUsageFlagBits::eTransferDst;
  drawImageUsages |= vk::ImageUsageFlagBits::eStorage;
  drawImageUsages |= vk::ImageUsageFlagBits::eColorAttachment;

  auto drawCreateInfo = DrawingSubsystem::MakeImageCreateInfo(vk::Format::eR16G16B16A16Sfloat,
                                            vk::Extent3D{
                                                extent.width, extent.height, 1},
                                            drawImageUsages);
  _drawImage = GetOuter()->GetAllocator().Reserve()->AllocateImage(drawCreateInfo,
                                         VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                                         vk::MemoryPropertyFlagBits::eDeviceLocal);

  
  const auto drawViewInfo = vk::ImageViewCreateInfo({}, _drawImage->image,
                                                    vk::ImageViewType::e2D,
                                                    _drawImage->format, {},
                                                    {vk::ImageAspectFlagBits::eColor,
                                                      0,
                                                      1, 0, 1});

  _drawImage->view = device.createImageView(drawViewInfo);

  auto depthCreateInfo = DrawingSubsystem::MakeImageCreateInfo(vk::Format::eD32Sfloat,
                                             _drawImage->extent,
                                             vk::ImageUsageFlagBits::eDepthStencilAttachment);

  _depthImage = GetOuter()->GetAllocator().Reserve()->AllocateImage(depthCreateInfo,
                                          VMA_MEMORY_USAGE_AUTO_PREFER_DEVICE,
                                          vk::MemoryPropertyFlagBits::eDeviceLocal);

  const auto depthViewInfo = vk::ImageViewCreateInfo({}, _depthImage->image,
    vk::ImageViewType::e2D,
    _depthImage->format, {},
    {vk::ImageAspectFlagBits::eDepth, 0,
     1, 0, 1});

  _depthImage->view = device.createImageView(depthViewInfo);
}

void WindowDrawer::DestroySwapchain() {
  const auto device = GetVirtualDevice();
  _drawImage.Clear();
  _depthImage.Clear();

  for (const auto view : _swapchainImageViews) {
    device.destroyImageView(view);
  }

  _swapchainImageViews.clear();
  _swapchainImages.clear();

  device.destroySwapchainKHR(_swapchain);

  _swapchain = nullptr;
}

vk::Device WindowDrawer::GetVirtualDevice() const {
  return GetOuter()->GetVirtualDevice();
}

void WindowDrawer::InitFrames() {
  const auto device = GetVirtualDevice();

  const auto commandPoolInfo = vk::CommandPoolCreateInfo(
      vk::CommandPoolCreateFlags(vk::CommandPoolCreateFlagBits::eResetCommandBuffer),
      GetOuter()->GetQueueFamily());
  
  for (auto &frame : _frames) {

    frame.SetCommandPool(device.createCommandPool(commandPoolInfo, nullptr));
    const auto commandBufferAllocateInfo = vk::CommandBufferAllocateInfo(
        *frame.GetCmdPool(), vk::CommandBufferLevel::ePrimary, 1);

    frame.SetCommandBuffer(
        device.allocateCommandBuffers(commandBufferAllocateInfo)
               .
               at(0));

    frame.SetDrawer(GetOuter());
    frame.SetWindowDrawer(this);
  }

  AddCleanup([this] {
    const auto device = GetVirtualDevice();
    
    for (auto &frame : _frames) {
      device.destroyCommandPool(*frame.GetCmdPool());
    }
  });
}

void WindowDrawer::InitSyncStructures() {
  const auto device = GetVirtualDevice();
  
  constexpr auto fenceCreateInfo = vk::FenceCreateInfo(
      vk::FenceCreateFlagBits::eSignaled);

  constexpr auto semaphoreCreateInfo = vk::SemaphoreCreateInfo(
      vk::SemaphoreCreateFlags());

  for (auto &frame : _frames) {
    frame.SetRenderFence(device.createFence(fenceCreateInfo));
    frame.SetSemaphores(device.createSemaphore(semaphoreCreateInfo),
                         device.createSemaphore(semaphoreCreateInfo));
  }
  
  AddCleanup([this] {
    const auto device = GetVirtualDevice();
    for (const auto &frame : _frames) {
      device.destroyFence(frame.GetRenderFence());
      device.destroySemaphore(frame.GetRenderSemaphore());
      device.destroySemaphore(frame.GetSwapchainSemaphore());
    }
  });
}

RawFrameData * WindowDrawer::GetCurrentFrame() {
  return &_frames[_frameCount % FRAME_OVERLAP];
}

void WindowDrawer::InitDescriptors() {

  const auto device = GetVirtualDevice();
  {

    _drawImageDescriptorLayout = DescriptorLayoutBuilder().AddBinding(0, vk::DescriptorType::eStorageImage,
                       vk::ShaderStageFlagBits::eCompute).Build();

    AddCleanup([this] {
      GetVirtualDevice().destroyDescriptorSetLayout(_drawImageDescriptorLayout);
      //descriptorAllocator.
    });
  }

  const auto allocator = GetOuter()->GetGlobalDescriptorAllocator();
  
  _drawImageDescriptors = allocator->Allocate(_drawImageDescriptorLayout);
  _drawImageDescriptors.Reserve()->WriteImage(0, _drawImage, {},
                                              vk::ImageLayout::eGeneral,
                                              vk::DescriptorType::eStorageImage);

  AddCleanup(_window.Reserve()->onResize,_window.Reserve()->onResize.Bind([this](window::Window* win) {
    if (_drawImageDescriptors) {
      _drawImageDescriptors.Reserve()->WriteImage(0, _drawImage, {},
                                                vk::ImageLayout::eGeneral,
                                                vk::DescriptorType::eStorageImage);
    }
  }));

  for (auto &frame : _frames) {
    // create a descriptor pool
    std::vector<DescriptorAllocatorGrowable::PoolSizeRatio> frame_sizes = {
      {vk::DescriptorType::eStorageImage, 3},
      {vk::DescriptorType::eStorageBuffer, 3},
      {vk::DescriptorType::eUniformBuffer, 3},
      {vk::DescriptorType::eCombinedImageSampler, 4},
  };

    frame.SetDrawer(GetOuter());
    frame.GetDescriptorAllocator()->Init(device, 1000, frame_sizes);

    AddCleanup([&] {
      frame.GetDescriptorAllocator()->DestroyPools();
      frame.cleaner.Run();
    });
  }
}

vk::SurfaceKHR WindowDrawer::GetSurface() const {
  return _surface;
}

vk::Extent2D WindowDrawer::GetSwapchainExtent() const {
  const auto pixelSize = _window.Reserve()->GetPixelSize();
  return {static_cast<uint32_t>(pixelSize.x),static_cast<uint32_t>(pixelSize.y)};
}

vk::Extent2D WindowDrawer::GetSwapchainExtentScaled() const {
  
  const auto extent = GetSwapchainExtent();
  constexpr auto renderScale = 1.0f;
  return {static_cast<uint32_t>(renderScale * extent.width),
          static_cast<uint32_t>(renderScale * extent.height)};
}

vk::Extent2D WindowDrawer::GetDrawImageExtent() const {
  if (!_drawImage) {
    return {};
  }
  return {static_cast<uint32_t>(_drawImage->extent.width),
          static_cast<uint32_t>(_drawImage->extent.height)};
}

vk::Extent2D WindowDrawer::GetDrawImageExtentScaled() const {
  constexpr auto renderScale = 1.0f;
  return {static_cast<uint32_t>(_drawImage->extent.width * renderScale),
          static_cast<uint32_t>(_drawImage->extent.height * renderScale)};
}

vk::Format WindowDrawer::GetDrawImageFormat() const {
  return _drawImage->format;
}

vk::Format WindowDrawer::GetDepthImageFormat() const {
  return _depthImage->format;
}

vk::Viewport WindowDrawer::GetViewport() const {
  return _viewport;
}

void WindowDrawer::DrawScenes(RawFrameData *frame) {
  const vk::Extent2D drawExtent = GetDrawImageExtentScaled();

  const auto colorAttachment = DrawingSubsystem::MakeRenderingAttachment(
      _drawImage->view, vk::ImageLayout::eGeneral);

  vk::ClearValue depthClear;
  depthClear.setDepthStencil({1.f});

  const auto depthAttachment = DrawingSubsystem::MakeRenderingAttachment(
      _depthImage->view, vk::ImageLayout::eDepthAttachmentOptimal, depthClear);

  auto renderingInfo = DrawingSubsystem::MakeRenderingInfo(drawExtent);
  renderingInfo.setColorAttachments(colorAttachment);
  renderingInfo.setPDepthAttachment(&depthAttachment);

  const auto cmd = frame->GetCmd();

  cmd->beginRendering(renderingInfo);

  //Actual Rendering
  onDrawScenes(frame);

  cmd->endRendering();
}

void WindowDrawer::DrawUi(RawFrameData *frame) {
  const vk::Extent2D drawExtent = GetDrawImageExtentScaled();
    
  const auto colorAttachment = DrawingSubsystem::MakeRenderingAttachment(_drawImage->view, vk::ImageLayout::eGeneral);
    
  auto renderingInfo = DrawingSubsystem::MakeRenderingInfo(drawExtent);
  renderingInfo.setColorAttachments(colorAttachment);
    
  const auto cmd = frame->GetCmd();
    
  cmd->beginRendering(renderingInfo);
    
  onDrawUi(frame);
    
  cmd->endRendering();
}

void WindowDrawer::Draw() {
  
  const auto frame = GetCurrentFrame();
  const auto device = GetVirtualDevice();
  // Wait for gpu to finish past work
  vk::resultCheck(
      device.waitForFences({frame->GetRenderFence()}, true, 1000000000),
      "Wait For Fences Failed");
  
  frame->cleaner.Run();
  frame->GetDescriptorAllocator()->ClearPools();

  device.resetFences({frame->GetRenderFence()});

  // Request image index from swapchain
  uint32_t swapchainImageIndex;

  try {
    const auto _ = device.acquireNextImageKHR(_swapchain, 1000000000,
                                               frame->GetSwapchainSemaphore(),
                                               nullptr, &swapchainImageIndex);
  } catch (vk::OutOfDateKHRError &_) {
    /* Resize here */
    _isResizing = true;
    return;
  }

  const auto cmd = frame->GetCmd();

  // Clear command buffer and prepare to render
  cmd->reset();

  constexpr auto commandBeginInfo = vk::CommandBufferBeginInfo(
      vk::CommandBufferUsageFlagBits::eOneTimeSubmit);

  const auto swapchainExtent = GetSwapchainExtent();

  const vk::Extent2D drawExtent = GetDrawImageExtentScaled();

  cmd->begin(commandBeginInfo);

  // Transition image to general layout
  DrawingSubsystem::TransitionImage(*cmd, _drawImage->image,
                  vk::ImageLayout::eUndefined, vk::ImageLayout::eGeneral);

  DrawBackground(frame);

  DrawingSubsystem::TransitionImage(*cmd, _drawImage->image,
                  vk::ImageLayout::eGeneral,
                  vk::ImageLayout::eColorAttachmentOptimal);
  DrawingSubsystem::TransitionImage(*cmd, _depthImage->image,
                  vk::ImageLayout::eUndefined,
                  vk::ImageLayout::eDepthAttachmentOptimal,vk::ImageAspectFlagBits::eDepth);

  cmd->setViewport(0, {_viewport});

  vk::Rect2D scissor{{0, 0}, {static_cast<uint32_t>(_viewport.width),static_cast<uint32_t>(_viewport.height)}};

  cmd->setScissor(0, {scissor});
  
  if(onDrawScenes.GetNumListeners() > 0) {
    DrawScenes(frame);
  }
  
  if(onDrawUi.GetNumListeners() > 0) {
    DrawUi(frame);
  }

  // Transition images to correct transfer layouts
  DrawingSubsystem::TransitionImage(*cmd, _drawImage->image,
                  vk::ImageLayout::eColorAttachmentOptimal,
                  vk::ImageLayout::eTransferSrcOptimal);
  
  DrawingSubsystem::TransitionImage(*cmd, _swapchainImages[swapchainImageIndex],
                  vk::ImageLayout::eUndefined,
                  vk::ImageLayout::eTransferDstOptimal);

  DrawingSubsystem::CopyImageToImage(*cmd, _drawImage->image,
                   _swapchainImages[swapchainImageIndex],
                   drawExtent, swapchainExtent);

  DrawingSubsystem::TransitionImage(*cmd, _swapchainImages[swapchainImageIndex],
                  vk::ImageLayout::eTransferDstOptimal,
                  vk::ImageLayout::ePresentSrcKHR);
  

  // Cant add commands anymore
  cmd->end();

  const auto cmdInfo = vk::CommandBufferSubmitInfo(*cmd, 0);

  const auto waitingInfo = vk::SemaphoreSubmitInfo(
      frame->GetSwapchainSemaphore(), 1,
      vk::PipelineStageFlagBits2::eColorAttachmentOutput);
  const auto signalInfo = vk::SemaphoreSubmitInfo(
      frame->GetRenderSemaphore(), 1, vk::PipelineStageFlagBits2::eAllGraphics);

  const auto submitInfo = vk::SubmitInfo2({}, waitingInfo, cmdInfo,
                                          signalInfo);

  
  
  const auto renderSemaphore = frame->GetRenderSemaphore();
  const auto presentInfo = vk::PresentInfoKHR(renderSemaphore,
                                              _swapchain,
                                              swapchainImageIndex);

  
  try {
    GetOuter()->SubmitAndPresent(frame,submitInfo,presentInfo);
  } catch (vk::OutOfDateKHRError &_) {
    /* Need to resize here */
    _isResizing = true;
    return;
  }

  _frameCount++;
}

void WindowDrawer::DrawBackground(RawFrameData *frame) {
  // Draw background
  const auto cmd = frame->GetCmd();

  //const float flash = abs(sin(_frameCount / 120.f));

  const auto clearValue = vk::ClearColorValue(0.7f, 0.7f, 0.7f, 0.0f);
  //vk::ClearColorValue({0.0f, 0.0f, flash, 0.0f});

  auto clearRange = DrawingSubsystem::ImageSubResourceRange(vk::ImageAspectFlagBits::eColor);

  cmd->clearColorImage(_drawImage->image,
                       vk::ImageLayout::eGeneral, clearValue,
                       {clearRange});
}

void WindowDrawer::BeforeDestroy() {
  Object<DrawingSubsystem>::BeforeDestroy();
  const auto instance = GetOuter()->GetVulkanInstance();
  instance.destroySurfaceKHR(_surface);
}
}
