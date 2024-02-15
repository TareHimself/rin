#pragma once
#define GLFW_INCLUDE_VULKAN
#include <GLFW/glfw3.h>
#include "types.hpp"
#include "vengine/Managed.hpp"
#include "vengine/Object.hpp"

#include <glm/vec2.hpp>
#include <vulkan/vulkan.hpp>


namespace vengine::window {
class WindowManager;

class Window : public Object<WindowManager> {
  GLFWwindow * _window = nullptr;
public:
  Window(GLFWwindow * window);

  void Init(WindowManager *outer) override;

  GLFWwindow* GetRaw() const;

  vk::SurfaceKHR CreateSurface(const vk::Instance& instance) const;

  void SetCursorMode(ECursorMode mode) const;

  glm::ivec2 GetSize() const;

  glm::dvec2 GetMousePosition() const;

  void SetMousePosition(const glm::dvec2& position) const;

  bool CloseRequested() const;

  void BeforeDestroy() override;

  void HandleKey(int key, int scancode, int action, int mods);

  void HandleMouseMove(double x, double y);

  void HandleMouseButton(int button, int action, int mods);

  void HandleScroll(double xOffset, double yOffset);

  bool IsFocused() const;
  
  TDispatcher<const std::shared_ptr<KeyEvent>&> onKeyUp;
  TDispatcher<const std::shared_ptr<KeyEvent>&> onKeyDown;
  TDispatcher<const std::shared_ptr<MouseMovedEvent>&> onMouseMoved;
  TDispatcher<const std::shared_ptr<MouseButtonEvent>&> onMouseDown;
  TDispatcher<const std::shared_ptr<MouseButtonEvent>&> onMouseUp;
  TDispatcher<const std::shared_ptr<ScrollEvent>&> onScroll;
  TDispatcher<bool> onFocusChanged;
};

class WindowManager {

  std::unordered_map<GLFWwindow *,Managed<Window>> _windows;
public:
  void Start() const;
  
  void Stop() const;
  
  Ref<Window> Create(int width,int height,const std::string& name, const Window * owner);

  void Destroy(const Ref<Window>& window);

  void Poll() const;


};
std::shared_ptr<WindowManager> get();
std::pair<uint32_t, const char **> getExtensions();
}

