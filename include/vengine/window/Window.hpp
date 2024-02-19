#pragma once
#define GLFW_INCLUDE_VULKAN
#include <GLFW/glfw3.h>
#include "types.hpp"
#include "vengine/Managed.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"

#include <glm/vec2.hpp>
#include <vulkan/vulkan.hpp>


namespace vengine::window {
class WindowManager;

class Window : public Object<WindowManager>, public RefThis<Window> {
  GLFWwindow * _window = nullptr;
  Window * _owner = nullptr;
  uint64_t _id;
public:
  Window(GLFWwindow * window,uint64_t id);

  uint64_t GetId() const;

  void Init(WindowManager *outer) override;

  GLFWwindow* GetRaw() const;

  vk::SurfaceKHR CreateSurface(const vk::Instance& instance) const;

  void SetCursorMode(ECursorMode mode) const;

  glm::uvec2 GetSize() const;

  glm::uvec2 GetPixelSize() const;

  glm::dvec2 GetMousePosition() const;
  
  Ref<Window> CreateChild(int width, int height, const std::string &name);

  void SetMousePosition(const glm::dvec2& position) const;

  bool CloseRequested() const;

  void BeforeDestroy() override;

  void HandleKey(int key, int scancode, int action, int mods);

  void HandleMouseMove(double x, double y);

  void HandleMouseButton(int button, int action, int mods);

  void HandleScroll(double xOffset, double yOffset);

  void HandleResize(int width, int height);

  bool IsFocused() const;

  void SetOwner(Window *owner);

  Window * GetOwner() const;
  
  TDispatcher<const std::shared_ptr<KeyEvent>&> onKeyUp;
  TDispatcher<const std::shared_ptr<KeyEvent>&> onKeyDown;
  TDispatcher<const std::shared_ptr<MouseButtonEvent>&> onMouseDown;
  TDispatcher<const std::shared_ptr<MouseMovedEvent>&> onMouseMoved;
  TDispatcher<const std::shared_ptr<MouseButtonEvent>&> onMouseUp;
  TDispatcher<const std::shared_ptr<ScrollEvent>&> onScroll;
  TDispatcher<Window *,bool> onFocusChanged;
  TDispatcher<Window *> onResize;
  TDispatcher<Window *> onCloseRequested;
};

class WindowManager {

  std::unordered_map<uint64_t,Managed<Window>> _windows;
  std::atomic<uint64_t> _ids = 0;
public:
  static void Start();
  
  void Stop();
  Ref<Window> Create(int width, int height, const std::string &name,
                     Window *owner);

  Managed<Window> CreateRaw(int width, int height, const std::string &name,
                     const Window *owner);


  Ref<Window> Find(uint64_t id);
  void Destroy(uint64_t id);

  static void Poll();

  Array<Ref<Window>> GetWindows() const;

  TDispatcher<Ref<Window>> onWindowCreated;
  TDispatcher<Ref<Window>> onWindowDestroyed;

  TDispatcher<const std::shared_ptr<KeyEvent>&> onWindowKeyUp;
  TDispatcher<const std::shared_ptr<KeyEvent>&> onWindowKeyDown;
  TDispatcher<const std::shared_ptr<MouseButtonEvent>&> onWindowMouseDown;
  TDispatcher<const std::shared_ptr<MouseMovedEvent>&> onWindowMouseMoved;
  TDispatcher<const std::shared_ptr<MouseButtonEvent>&> onWindowMouseUp;
  TDispatcher<const std::shared_ptr<ScrollEvent>&> onWindowScroll;
  TDispatcher<Window *,bool> onWindowFocusChanged;
  TDispatcher<Window *> onWindowResize;
  TDispatcher<Window *> onWindowCloseRequested;
};
std::shared_ptr<WindowManager> getManager();

std::pair<uint32_t, const char **> getExtensions();
void init();
void poll();
void terminate();
Ref<Window> create(int width,int height,const std::string& name, const Ref<Window>& owner);
void destroy(uint64_t id);
}

