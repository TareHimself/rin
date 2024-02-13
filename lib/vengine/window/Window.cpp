#include "vengine/window/Window.hpp"
namespace vengine::window {
Window::Window(GLFWwindow * window) {
  _window = window;
  glfwSetWindowUserPointer(_window,this);
  
}

void Window::Init(WindowManager *outer) {
  Object<WindowManager>::Init(outer);
  if(_window != nullptr) {
    glfwSetKeyCallback(_window,[](GLFWwindow* window, int key, int scancode, int action, int mods) {
      const auto self = static_cast<Window*>(glfwGetWindowUserPointer(window));
      self->HandleKey(key,scancode,action,mods);
    });

    glfwSetCursorPosCallback(_window,[](GLFWwindow* window, double x, double y) {
      const auto self = static_cast<Window*>(glfwGetWindowUserPointer(window));
      self->HandleMouseMove(x,y);
    });

    glfwSetMouseButtonCallback(_window,[](GLFWwindow* window, int button, int action, int mods) {
      const auto self = static_cast<Window*>(glfwGetWindowUserPointer(window));
      self->HandleMouseButton(button,action,mods);
    });

    glfwSetWindowFocusCallback(_window,[](GLFWwindow* window, int focused) {
      const auto self = static_cast<Window*>(glfwGetWindowUserPointer(window));
      self->onFocusChanged(static_cast<bool>(focused));
    });
  }
}

GLFWwindow * Window::GetRaw() const {
  return _window;
}

vk::SurfaceKHR Window::CreateSurface(const vk::Instance &instance) const {
  VkSurfaceKHR rawSurface;
  const VkInstance rawInstance = instance;
  glfwCreateWindowSurface(rawInstance,_window,nullptr,&rawSurface);
  return rawSurface;
}

void Window::SetCursorMode(ECursorMode mode) const {
  glfwSetInputMode(_window,GLFW_CURSOR,mode);
}

glm::ivec2 Window::GetSize() const {
  int width,height = 0;

  glfwGetWindowSize(_window,&width,&height);
  
  return {width,height};
}

glm::dvec2 Window::GetMousePosition() const {
  double x,y = 0;
  glfwGetCursorPos(_window,&x,&y);

  return {x,y};
}

void Window::SetMousePosition(const glm::dvec2 &position) const {
  glfwSetCursorPos(_window,position.x,position.y);
}

bool Window::CloseRequested() const {
  return glfwWindowShouldClose(_window);
}

void Window::HandleKey(int key, int scancode, int action, int mods){
  if(action == GLFW_PRESS) {
    onKeyDown(std::make_shared<KeyEvent>(static_cast<EKey>(key)));
  }

  if(action == GLFW_RELEASE){
    onKeyUp(std::make_shared<KeyEvent>(static_cast<EKey>(key)));
  }
  
}

void Window::HandleMouseMove(const double x, const double y){
  onMouseMoved(std::make_shared<MouseMovedEvent>(x,y));
}

void Window::HandleMouseButton(int button, const int action, int mods){
  const auto mousePos = GetMousePosition();
  
  auto buttonEnum = static_cast<EMouseButton>(button);
  
  if(action == GLFW_PRESS) {
    onMouseDown(std::make_shared<MouseButtonEvent>(buttonEnum,mousePos.x,mousePos.y));
  }
  if(action == GLFW_RELEASE){
    onMouseUp(std::make_shared<MouseButtonEvent>(buttonEnum,mousePos.x,mousePos.y));
  }
}

bool Window::IsFocused() const {
  return static_cast<bool>(glfwGetWindowAttrib(_window, GLFW_FOCUSED));
}

void Window::BeforeDestroy() {
  Object<WindowManager>::BeforeDestroy();
  glfwDestroyWindow(_window);
  _window = nullptr;
}

void WindowManager::Start() const {
  glfwInit();
}

void WindowManager::Stop() const {
  glfwTerminate();
}

Ref<Window> WindowManager::Create(int width, int height,
    const std::string &name, const Window *owner) {
  glfwWindowHint(GLFW_CLIENT_API, GLFW_NO_API);
  
  if(const auto window = glfwCreateWindow(width,height,name.c_str(),nullptr,owner == nullptr ? nullptr : owner->GetRaw())) {
    _windows.insert({window,newManagedObject<Window>(window)});
    _windows[window]->Init(this);
    return _windows[window];
  }
  return {};
}

void WindowManager::Destroy(const Ref<Window> &window) {
  if(auto reserved = window.Reserve()) {
    _windows.erase(_windows.find(reserved->GetRaw()));
  }
}

void WindowManager::Poll() const {
  glfwPollEvents();
}

std::shared_ptr<WindowManager> get() {
  static auto instance = std::make_shared<WindowManager>();
  return instance;
}

std::pair<uint32_t, const char **> getExtensions() {
  uint32_t numExtensions = 0;
  auto data = glfwGetRequiredInstanceExtensions(&numExtensions);

  return std::pair{numExtensions,data};
}

void init() {
  
}

void terminate() {
  
}
}
