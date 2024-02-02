#include <iostream>
#include <vengine/Engine.hpp>
#include <vengine/assets/AssetManager.hpp>
#include <vengine/input/InputManager.hpp>
#include <vengine/drawing/Drawer.hpp>
#include <vengine/scene/Scene.hpp>
#include <chrono>
#include <thread>
#include <vengine/scripting/ScriptManager.hpp>
#include <vengine/widget/WidgetManager.hpp>
#include <SDL_video.h>

namespace vengine {

void Engine::SetAppName(const String &newName) {
  _applicationName = newName;

}

String Engine::GetAppName() const{
  return _applicationName;
}

bool Engine::IsRunning() const {
  return bIsRunning;
}

bool Engine::ShouldExit() const {
  return bExitRequested;
}

void Engine::RequestExit() {
  bExitRequested = true;
}

void Engine::Run() {
  bIsRunning = true;
  Init();
  
  _lastTickTime = Now();
  
  std::thread drawThread([this] {
    RunDraw();
  });
  
  
  RunGame();
  drawThread.join();
  Destroy();
  bIsRunning = false;
}

void Engine::RunGame() {
  while (bIsRunning && !ShouldExit()) {
    const auto tickStart = Now();
    const auto delta = tickStart - _lastTickTime;
    _runTime += delta;
    const auto deltaFloat = static_cast<float>(
      static_cast<double>(delta) / 1000.0);

    SDL_Event e;
    while (SDL_PollEvent(&e) != 0) {
      _inputManager->ProcessSdlEvent(e);
      switch (e.type) {
      case SDL_EVENT_QUIT:
        bExitRequested = true;
        break;
      case SDL_EVENT_WINDOW_MINIMIZED:
        bIsMinimized = true;
        break;

      case SDL_EVENT_WINDOW_MAXIMIZED:
        bIsMinimized = false;
        break;
      case SDL_EVENT_WINDOW_LEAVE_FULLSCREEN:
        NotifyWindowResize();
        break;
      case SDL_EVENT_WINDOW_FOCUS_GAINED:
        bIsFocused = true;
        break;
      case SDL_EVENT_WINDOW_FOCUS_LOST:
        bIsFocused = false;
        break;
      case SDL_EVENT_MOUSE_MOTION:
        _mousePosition = math::Vector2{e.motion.x,e.motion.y};
        break;
      default:
        break;
      }
    }

    if(_inputMode == EInputMode::GameOnly) {
      const auto windowHalfW = _windowExtent.width / 2;
      const auto windowHalfH = _windowExtent.height / 2;
      const auto mouseX = windowHalfW - _mousePosition.x;
      const auto mouseY = windowHalfH - _mousePosition.y;
      const auto windowQH = windowHalfH / 2;
      const auto windowQW = windowHalfW / 2;
      if(IsFocused() && (std::abs(mouseX) > windowQW || std::abs(mouseY) > windowQH)) {
        SDL_WarpMouseInWindow(_window.Get(),windowHalfW,windowHalfH);
        _mousePosition = math::Vector2{windowHalfW,windowHalfH};
      }
    }
    
    Update(deltaFloat);
    
    _lastTickTime = tickStart;
  }
}

void Engine::RunDraw() const {
  while (!ShouldExit()) {
    if(bIsMinimized) {
      std::this_thread::sleep_for(std::chrono::milliseconds{1000});
      return;
    }
    
    _drawer->Draw();
  }
}

float Engine::GetEngineTimeSeconds() const {
  return static_cast<float>(_runTime / 1000.0);
}

Array<WeakPointer<scene::Scene>> Engine::GetScenes() const {
  Array<WeakPointer<scene::Scene>> scenes;
  for(auto &scene : _scenes) {
    scenes.Push(scene);
  }
  return scenes;
}

WeakPointer<SDL_Window> Engine::GetWindow() const {
  return _window;
}

vk::Extent2D Engine::GetWindowExtent() const { return _windowExtent; }

WeakPointer<drawing::Drawer> Engine::GetDrawer() const{
  return _drawer;
}

WeakPointer<input::InputManager> Engine::GetInputManager() const{
  return _inputManager;
}

long long Engine::Now() {
  return std::chrono::steady_clock::now().time_since_epoch().count() / 1000000;
}

Engine::Engine() = default;

void Engine::Init() {
  InitWindow();
  InitScriptManager();
  InitInputManager();
  InitDrawer();
  InitAssetManager();
  InitWidgetManager();
  InitScenes();

  auto testInputConsumer = GetInputManager().Reserve()->Consume<input::InputConsumer>().Reserve();
  
  testInputConsumer->BindKey(SDLK_ESCAPE,[=](const  input::KeyInputEvent &e) {
    RequestExit();
    return true;
  },[](const  input::KeyInputEvent &_) {
    return false;
  });

  testInputConsumer->BindKey(SDLK_LSHIFT,[=](const  input::KeyInputEvent &e) {
    SetInputMode(EInputMode::GameOnly);
    return true;
  },[=](const  input::KeyInputEvent &_) {
    SetInputMode(EInputMode::UiOnly);
    return true;
  });

  SetInputMode(EInputMode::UiOnly);
}

void Engine::Update(float deltaTime) const {
  for (const auto &scene : _scenes) {
    scene->Update(deltaTime);
  }
}


void Engine::InitWindow() {
  // SDL_SetMainReady();
  SDL_Init(SDL_INIT_VIDEO);
  

  constexpr auto flags = SDL_WINDOW_VULKAN | SDL_WINDOW_RESIZABLE;

  _window = Pointer<SDL_Window>(SDL_CreateWindow(
      GetAppName().c_str(),
      _windowExtent.width,
      _windowExtent.height,
      flags
      ),[](SDL_Window * win) {
        SDL_DestroyWindow(win);
      });
  
  //SDL_SetWindowFullscreen(window,true);
  //SDL_CaptureMouse(true);
  
  AddCleanup([=] {
    _window.Clear();
  });
}

void Engine::InitDrawer() {

  _drawer = CreateDrawer();
  _drawer->Init(this);

  AddCleanup([=] {
    _drawer.Clear();
  });
}

void Engine::InitScenes() {
  for(const auto &scene: _scenes) {
    InitScene(scene);
  }

  AddCleanup([=] {
    _scenes.clear();
  });
}

void Engine::InitAssetManager() {
  _assetManager = CreateAssetManager();
  _assetManager->Init(this);

  AddCleanup([=] {
    _assetManager.Clear();
  });
}

WeakPointer<assets::AssetManager> Engine::GetAssetManager() const {
  return _assetManager;
}

WeakPointer<scripting::ScriptManager> Engine::GetScriptManager() const {
  return _scriptManager;
}

WeakPointer<widget::WidgetManager> Engine::GetWidgetManager() const {
  return _widgetManager;
}

Pointer<drawing::Drawer> Engine::CreateDrawer() {
  return newSharedObject<drawing::Drawer>();
}

Pointer<input::InputManager> Engine::CreateInputManager() {
  return newSharedObject<input::InputManager>();
}

Pointer<assets::AssetManager> Engine::CreateAssetManager() {
  return newSharedObject<assets::AssetManager>();
}

Pointer<scripting::ScriptManager> Engine::CreateScriptManager() {
  return newSharedObject<scripting::ScriptManager>();
}

Pointer<widget::WidgetManager> Engine::CreateWidgetManager() {
  return newSharedObject<widget::WidgetManager>();
}

void Engine::SetInputMode(EInputMode mode) {
  switch (mode) {
  case EInputMode::GameOnly:
    SDL_SetWindowGrab(_window.Get(),true);
    SDL_HideCursor();
    break;

  case EInputMode::UiOnly:
    SDL_SetWindowGrab(_window.Get(),false);
    SDL_ShowCursor();
    break;

  case EInputMode::GameAndUi:
    SDL_SetWindowGrab(_window.Get(),true);
    SDL_ShowCursor();
    break;
    
  }

  onInputModeChanged.Emit(_inputMode,mode);

  _inputMode = mode;
}

void Engine::NotifyWindowResize() {
  int width,height;

  if(IsFullScreen()) {
    const auto display = SDL_GetPrimaryDisplay();
    const auto dm = SDL_GetDesktopDisplayMode(display);
    width = dm->w;
    height = dm->h;
    SDL_CaptureMouse(true);
  }
  else
  {
    SDL_GetWindowSize(_window.Get(),&width,&height);
  }
  _windowExtent.setWidth(width);
  _windowExtent.setHeight(height);

  if(_drawer->GetDrawImageExtent() != _windowExtent) {
    _drawer->RequestResize();
  }
}

bool Engine::IsFullScreen() const {
  return SDL_GetWindowFlags(_window.Get()) & SDL_WINDOW_FULLSCREEN;
}

bool Engine::IsFocused() const {
  return SDL_GetWindowFlags(_window.Get()) & SDL_WINDOW_MOUSE_FOCUS;
}

void Engine::InitScene(const Pointer<scene::Scene> &scene) {
  if (IsRunning()) {
    scene->Init(this);
  }
}

void Engine::InitInputManager() {
  _inputManager = CreateInputManager();
  _inputManager->Init(this);
  AddCleanup([=] {
    _inputManager.Clear();
  });
}

void Engine::InitScriptManager() {
  _scriptManager = CreateScriptManager();
  _scriptManager->Init(this);
  AddCleanup([=] {
    _scriptManager.Clear();
  });
}

void Engine::InitWidgetManager() {
  _widgetManager = CreateWidgetManager();
  _widgetManager->Init(this);
  AddCleanup([=] {
    _widgetManager.Clear();
  });
}

}
