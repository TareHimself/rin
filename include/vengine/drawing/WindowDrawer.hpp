#pragma once
#include "DrawingSubsystem.hpp"
#include "vengine/Object.hpp"

namespace vengine {
namespace window {
class Window;
}
}

namespace vengine::drawing {
class WindowDrawer : public Object<DrawingSubsystem> {
  
  Ref<window::Window> _window;

  long long _frameCount = 0;
  vk::SurfaceKHR _surface = nullptr;
  vk::SwapchainKHR _swapchain = nullptr;
  vk::Format _swapchainImageFormat = vk::Format::eUndefined;
  Array<vk::Image> _swapchainImages;
  Array<vk::ImageView> _swapchainImageViews;
  
  RawFrameData _frames[FRAME_OVERLAP];
  bool _isResizing = false;
  bool _isReady = false;

  vk::Viewport _viewport;
public:

  void Init(DrawingSubsystem *outer) override;

  virtual void Init(const Ref<window::Window> &window,DrawingSubsystem * outer);

  virtual bool ShouldDraw() const;
  virtual void CreateResources();
  virtual void CreateSwapchain();
  virtual void DestroySwapchain();

  virtual vk::Device GetVirtualDevice() const;
  virtual void InitFrames();

  virtual void InitSyncStructures();

  virtual RawFrameData * GetCurrentFrame();

  virtual void InitDescriptors();
  
  vk::SurfaceKHR GetSurface() const;

  vk::Extent2D GetSwapchainExtent() const;

  vk::Extent2D GetSwapchainExtentScaled() const;

  vk::Format GetSwapchainFormat() const;

  vk::Viewport GetViewport() const;

  TDispatcher<> onResizeScenes;
  TDispatcher<> onResizeUi;
  TDispatcher<RawFrameData *> onDrawScenes;
  TDispatcher<RawFrameData *> onDrawUi;

  virtual void Draw();

  void BeforeDestroy() override;
};

}
