#pragma once
#define GLFW_INCLUDE_VULKAN
#include <GLFW/glfw3.h>
#include "types.hpp"
#include "aerox/typedefs.hpp"
#include "aerox/Object.hpp"
#include "aerox/containers/Array.hpp"

#include <glm/vec2.hpp>
#include <vulkan/vulkan.hpp>


namespace aerox::window {
class WindowManager;

class Window : public Object {
  GLFWwindow *_window = nullptr;
  Window *_owner = nullptr;
  uint64_t _id;

public:
  Window(GLFWwindow *window, uint64_t id);

  uint64_t GetId() const;

  GLFWwindow *GetRaw() const;

  vk::SurfaceKHR CreateSurface(const vk::Instance &instance) const;

  void SetCursorMode(ECursorMode mode) const;

  glm::uvec2 GetSize() const;

  glm::uvec2 GetPixelSize() const;

  glm::dvec2 GetMousePosition() const;

  std::weak_ptr<Window> CreateChild(int width, int height, const std::string &name);

  void SetMousePosition(const glm::dvec2 &position) const;

  bool CloseRequested() const;

  void OnDestroy() override;

  void HandleKey(int key, int scancode, int action, int mods);

  void HandleMouseMove(double x, double y);

  void HandleMouseButton(int button, int action, int mods);

  void HandleScroll(double xOffset, double yOffset);

  void HandleResize(int width, int height);

  bool IsFocused() const;

  void SetOwner(Window *owner);

  Window *GetOwner() const;

  DECLARE_DELEGATE(onKeyUp,const std::shared_ptr<KeyEvent> &)
  DECLARE_DELEGATE(onKeyDown,const std::shared_ptr<KeyEvent> &)
  DECLARE_DELEGATE(onMouseDown,const std::shared_ptr<MouseButtonEvent> &)
  DECLARE_DELEGATE(onMouseMoved,const std::shared_ptr<MouseMovedEvent> &)
  DECLARE_DELEGATE(onMouseUp,const std::shared_ptr<MouseButtonEvent> &)
  DECLARE_DELEGATE(onScroll,const std::shared_ptr<ScrollEvent> &)
  DECLARE_DELEGATE(onFocusChanged,const std::weak_ptr<Window> &, bool)
  DECLARE_DELEGATE(onResize,const std::weak_ptr<Window> &)
  DECLARE_DELEGATE(onCloseRequested,const std::weak_ptr<Window> &)
};

class WindowManager {

  std::unordered_map<uint64_t, std::shared_ptr<Window>> _windows;
  std::atomic<uint64_t> _ids = 0;

public:
  static void Start();

  void Stop();
  std::weak_ptr<Window> Create(int width, int height, const std::string &name,
                     Window *owner);

  std::shared_ptr<Window> CreateRaw(int width, int height, const std::string &name,
                            const Window *owner);


  std::weak_ptr<Window> Find(uint64_t id);
  void Destroy(uint64_t id);

  static void Poll();

  Array<std::weak_ptr<Window>> GetWindows() const;

  
  DECLARE_DELEGATE(onWindowCreated,const std::weak_ptr<Window> &)
  DECLARE_DELEGATE(onWindowDestroyed,const std::weak_ptr<Window> &)
  DECLARE_DELEGATE(onWindowKeyUp,const std::shared_ptr<KeyEvent> &)
  DECLARE_DELEGATE(onWindowKeyDown,const std::shared_ptr<KeyEvent> &)
  DECLARE_DELEGATE(onWindowMouseDown,const std::shared_ptr<MouseButtonEvent> &)
  DECLARE_DELEGATE(onWindowMouseMoved,const std::shared_ptr<MouseMovedEvent> &)
  DECLARE_DELEGATE(onWindowMouseUp,const std::shared_ptr<MouseButtonEvent> &)
  DECLARE_DELEGATE(onWindowScroll,const std::shared_ptr<ScrollEvent> &)
  DECLARE_DELEGATE(onWindowFocusChanged,const std::weak_ptr<Window> &, bool)
  DECLARE_DELEGATE(onWindowResize,const std::weak_ptr<Window> &)
  DECLARE_DELEGATE(onWindowCloseRequested,const std::weak_ptr<Window> &)
};

std::shared_ptr<WindowManager> getManager();

std::pair<uint32_t, const char **> getExtensions();
void init();
void poll();
void terminate();
std::weak_ptr<Window> create(int width, int height, const std::string &name,
                   const std::weak_ptr<Window> &owner);
void destroy(uint64_t id);
}
