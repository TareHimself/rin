#include <iostream>
#include "Engine.hpp"

#include "assets/AssetManager.hpp"
#include "input/InputManager.hpp"
#include "drawing/Drawer.hpp"
#include "scene/Scene.hpp"
#include <chrono>
#include <thread>
// #include <imgui_impl_sdl3.h>
// #include <imgui_impl_vulkan.h>
#include "scripting/ScriptManager.hpp"

#include <SDL_video.h>

namespace vengine {

void Engine::SetAppName(const std::string &newName) {
  _applicationName = newName;

}

std::string Engine::GetAppName() {
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
  Init(nullptr);
  _lastTickTime = Now();

  std::thread windowEvents([=] {
    RunWindowEvents();
  });
  
  std::thread drawThread([=] {
    RunDraw();
  });
  
  bIsRunning = true;
  RunGame();
  bIsRunning = false;
  windowEvents.join();
  drawThread.join();
  Destroy();
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
      _windowEventQueue.push(e);
    }
    Update(deltaFloat);
    
    _lastTickTime = tickStart;
  }
}

void Engine::RunWindowEvents() {
  while(!ShouldExit()) {
    while (!_windowEventQueue.empty()) {
      auto e = _windowEventQueue.front();
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
      _windowEventQueue.pop();
    }

    if(_inputMode == EInputMode::GameOnly) {
      const auto windowHalfW = _windowExtent.width / 2;
      const auto windowHalfH = _windowExtent.height / 2;
      const auto mouseX = windowHalfW - _mousePosition.x;
      const auto mouseY = windowHalfH - _mousePosition.y;
      const auto windowQH = windowHalfH / 2;
      const auto windowQW = windowHalfW / 2;
      if(IsFocused() && (std::abs(mouseX) > windowQW || std::abs(mouseY) > windowQH)) {
        SDL_WarpMouseInWindow(_window,windowHalfW,windowHalfH);
        _mousePosition = math::Vector2{windowHalfW,windowHalfH};
      }
    }
  }
}

void Engine::RunDraw() const {
  while (!ShouldExit()) {
    if(!bIsMinimized && !_drawer->ResizePending()) {
      // New ImGui Frame
      // ImGui_ImplVulkan_NewFrame();
      // ImGui_ImplSDL3_NewFrame();
      // ImGui::NewFrame();
      //
      // if (ImGui::Begin("background")) {
      //   if(!_drawer->backgroundEffects.empty()) {
      //     drawing::ComputeEffect &selected = _drawer->backgroundEffects[_drawer
      //     ->currentBackgroundEffect];
      //
      //     ImGui::Text("Selected effect: ", selected.name.c_str());
      //
      //     ImGui::SliderInt("Effect Index", &_drawer->currentBackgroundEffect, 0,
      //                      _drawer->backgroundEffects.size() - 1);
      //
      //     ImGui::InputFloat4("data1",
      //                        reinterpret_cast<float *>(&selected.data.data1));
      //     ImGui::InputFloat4("data2",
      //                        reinterpret_cast<float *>(&selected.data.data2));
      //     ImGui::InputFloat4("data3",
      //                        reinterpret_cast<float *>(&selected.data.data3));
      //     ImGui::InputFloat4("data4",
      //                        reinterpret_cast<float *>(&selected.data.data4));
      //   }
      //
      //   ImGui::End();
      // }
      //
      // ImGui::Render();

      _drawer->Draw();
    } else {
      if(_drawer->ResizePending()) {
        _drawer->ResizeSwapchain();
      } else {
        std::this_thread::sleep_for(std::chrono::milliseconds{1000});
      }
    }
  }
}

void Engine::AddScene(scene::Scene * scene) {
  
}

float Engine::GetEngineTimeSeconds() const {
  return static_cast<float>(_runTime / 1000.0);
}

Array<scene::Scene *> Engine::GetScenes() {
  return _scenes;
}

SDL_Window *Engine::GetWindow() const {
  return _window;
}

vk::Extent2D Engine::GetWindowExtent() const { return _windowExtent; }

drawing::Drawer * Engine::GetDrawer() const{
  return _drawer;
}

input::InputManager * Engine::GetInputManager() const{
  return _inputManager;
}

long long Engine::Now() {
  return std::chrono::steady_clock::now().time_since_epoch().count() / 1000000;
}

Engine::Engine() = default;

void Engine::Update(float deltaTime) const {
  for (const auto &scene : _scenes) {
    scene->Update(deltaTime);
  }
}


void Engine::InitWindow() {
  // SDL_SetMainReady();
  SDL_Init(SDL_INIT_VIDEO);
  

  constexpr auto flags = SDL_WINDOW_VULKAN | SDL_WINDOW_RESIZABLE;

  _window = SDL_CreateWindow(
      GetAppName().c_str(),
      _windowExtent.width,
      _windowExtent.height,
      flags
      );
  
  //SDL_SetWindowFullscreen(window,true);
  //SDL_CaptureMouse(true);
  SDL_SetWindowGrab(_window,true);
  SetInputMode(EInputMode::GameOnly);
  AddCleanup([=] {
    if (_window != nullptr) {
      SDL_DestroyWindow(_window);
    }
  });
}

void Engine::InitDrawer() {

  _drawer = CreateDrawer();
  _drawer->Init(this);

  AddCleanup([=] {
    _drawer->Destroy();
  });
}

void Engine::InitScenes() {
  for(const auto scene: _scenes) {
    scene->Init(this);
  }

  AddCleanup([=] {
    for(const auto & scene: _scenes) {
    scene->Destroy();
  }

    _scenes.clear();
  });
}

void Engine::InitAssetManager() {
  _assetManager = CreateAssetManager();
  _assetManager->Init(this);

  AddCleanup([=] {
    _assetManager->Destroy();
  });
}

assets::AssetManager * Engine::GetAssetManager() const {
  return _assetManager;
}

scripting::ScriptManager * Engine::GetScriptManager() const {
  return _scriptManager;
}

drawing::Drawer * Engine::CreateDrawer() {
  return newObject<drawing::Drawer>();
}

input::InputManager * Engine::CreateInputManager() {
  return newObject<input::InputManager>();
}

assets::AssetManager * Engine::CreateAssetManager() {
  return newObject<assets::AssetManager>();
}

scripting::ScriptManager * Engine::CreateScriptManager() {
  return newObject<scripting::ScriptManager>();
}

void Engine::SetInputMode(EInputMode mode) {
  switch (mode) {
  case EInputMode::GameOnly:
    SDL_HideCursor();
    break;

  case EInputMode::UiOnly:
    SDL_ShowCursor();
    break;

  case EInputMode::GameAndUi:
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
    SDL_GetWindowSize(_window,&width,&height);
  }
  _windowExtent.setWidth(width);
  _windowExtent.setHeight(height);
  GetDrawer()->RequestResize();
}

bool Engine::IsFullScreen() const {
  return SDL_GetWindowFlags(_window) & SDL_WINDOW_FULLSCREEN;
}

bool Engine::IsFocused() const {
  return SDL_GetWindowFlags(_window) & SDL_WINDOW_MOUSE_FOCUS;
}

void Engine::InitScene(scene::Scene *scene) {
  _scenes.Push(scene);

  if (IsRunning()) {
    scene->Init(this);
  }
}

void Engine::InitInputManager() {
  _inputManager = CreateInputManager();
  _inputManager->Init(this);
  AddCleanup([=] {
    _inputManager->Destroy();
  });
}

void Engine::InitScriptManager() {
  _scriptManager = CreateScriptManager();
  _scriptManager->Init(this);
  AddCleanup([=] {
    _scriptManager->Destroy();
  });
}

void Engine::Init(void *outer) {
  Object::Init(outer);
  InitAssetManager();
  InitWindow();
  InitScriptManager();
  InitInputManager();
  InitDrawer();
  InitScenes();

  GetInputManager()->BindKey(SDLK_ESCAPE,[=](const  input::KeyInputEvent &e) {
    RequestExit();
    return true;
  },[](const  input::KeyInputEvent &_) {
    return false;
  });
}

}
