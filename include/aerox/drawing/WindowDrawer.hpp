#pragma once
#include "DrawingSubsystem.hpp"
#include "aerox/Object.hpp"
#include "aerox/TObjectWithInit.hpp"

namespace aerox {
namespace window {
class Window;
}
}

namespace aerox::drawing {
class WindowDrawer : public TObjectWithInit<const std::weak_ptr<window::Window> &,DrawingSubsystem * > {

protected:
  std::weak_ptr<window::Window> _window;
  DrawingSubsystem * _drawingSubsystem = nullptr;
  
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

  void OnInit(const std::weak_ptr<window::Window> & window, DrawingSubsystem * drawer) override;

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

  DECLARE_DELEGATE(onResizeScenes)
  DECLARE_DELEGATE(onResizeUi)
  DECLARE_DELEGATE(onDrawScenes,RawFrameData *)
  DECLARE_DELEGATE(onDrawUi,RawFrameData *)

  virtual void Draw();

  void OnDestroy() override;
};

}
