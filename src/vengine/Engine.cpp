#include <iostream>
#include "Engine.hpp"

#include "assets/AssetManager.hpp"
#include "input/InputManager.hpp"
#include "drawing/Drawer.hpp"
#include "scene/Scene.hpp"
#include <chrono>
#include <thread>
#include <imgui_impl_sdl3.h>
#include <imgui_impl_vulkan.h>

namespace vengine {

Engine::Engine() = default;

Engine::~Engine() {
}

void Engine::setAppName(const std::string &newName) {
  applicationName = newName;

}

std::string Engine::getAppName() {
  return applicationName;
}

bool Engine::isRunning() const {
  return bIsRunning;
}

bool Engine::shouldExit() const {
  return bExitRequested;
}

void Engine::requestExit() {
  bExitRequested = true;
}

void Engine::run() {
  init(nullptr);
  bIsRunning = true;
  lastTickTime = now();

  bool bShouldQuit = false;

  while (!shouldExit() && !bShouldQuit) {
    const auto tickStart = now();
    const auto delta = tickStart - lastTickTime;
    runTime += delta;
    const auto deltaFloat = static_cast<float>(
      static_cast<double>(delta) / 1000.0);

    SDL_Event e;

    while (SDL_PollEvent(&e) != 0) {
      ImGui_ImplSDL3_ProcessEvent(&e);
      switch (e.type) {
      case SDL_EVENT_QUIT:
        bShouldQuit = true;
        break;
      case SDL_EVENT_KEY_UP:
        inputManager->receiveKeyReleasedEvent(e.key);
        break;
      case SDL_EVENT_KEY_DOWN:
        if (e.key.repeat == 0) {
          inputManager->receiveKeyPressedEvent(e.key);
        }
        break;
      case SDL_EVENT_WINDOW_MINIMIZED:
        bIsMinimized = true;
        break;

      case SDL_EVENT_WINDOW_MAXIMIZED:
        bIsMinimized = false;
        break;
      default:
        break;
      }
    }

    const auto shouldRender = !bIsMinimized && !renderer->resizePending();
    // if () {
    //   std::this_thread::sleep_for(std::chrono::milliseconds(100));
    //   continue;
    // }

    update(deltaFloat);

    if(shouldRender) {
      // New ImGui Frame
      ImGui_ImplVulkan_NewFrame();
      ImGui_ImplSDL3_NewFrame();
      ImGui::NewFrame();

      if (ImGui::Begin("background")) {
        const auto renderer = getRenderer();
        if(!renderer->backgroundEffects.empty()) {
          drawing::ComputeEffect &selected = renderer->backgroundEffects[renderer
          ->currentBackgroundEffect];

          ImGui::Text("Selected effect: ", selected.name.c_str());

          ImGui::SliderInt("Effect Index", &renderer->currentBackgroundEffect, 0,
                           renderer->backgroundEffects.size() - 1);

          ImGui::InputFloat4("data1",
                             reinterpret_cast<float *>(&selected.data.data1));
          ImGui::InputFloat4("data2",
                             reinterpret_cast<float *>(&selected.data.data2));
          ImGui::InputFloat4("data3",
                             reinterpret_cast<float *>(&selected.data.data3));
          ImGui::InputFloat4("data4",
                             reinterpret_cast<float *>(&selected.data.data4));
        }

        ImGui::End();
      }

      ImGui::Render();

      renderer->draw();
    } else {
      if(getRenderer()->resizePending()) {
        getRenderer()->resizeSwapchain();
      } else {
        std::this_thread::sleep_for(std::chrono::milliseconds{1000});
      }
    }
    
    lastTickTime = tickStart;
  }
  bIsRunning = false;
  cleanup();
}

void Engine::addScene(scene::Scene *scene) {
  scenes.push(scene);

  if (isRunning()) {
    scene->init(this);
  }
}

float Engine::getEngineTimeSeconds() const {
  return static_cast<float>(runTime / 1000.0);
}

Array<scene::Scene *> Engine::getScenes() {
  return scenes;
}

SDL_Window *Engine::getWindow() const {
  return window;
}

vk::Extent2D Engine::getWindowExtent() const { return windowExtent; }

drawing::Drawer *Engine::getRenderer() const {
  return renderer;
}

input::InputManager *Engine::getInputManager() const {
  return inputManager;
}

long long Engine::now() {
  return std::chrono::steady_clock::now().time_since_epoch().count() / 1000000;
}

void Engine::update(float deltaTime) {
  for (const auto scene : scenes) {
    scene->update(deltaTime);
  }
}


void Engine::initWindow() {
  // SDL_SetMainReady();
  SDL_Init(SDL_INIT_VIDEO);

  constexpr auto flags = SDL_WINDOW_VULKAN | SDL_WINDOW_RESIZABLE;

  window = SDL_CreateWindow(
      getAppName().c_str(),
      windowExtent.width,
      windowExtent.height,
      flags
      );

  addCleanup([=] {
    if (window != nullptr) {
      SDL_DestroyWindow(window);
    }
  });
}

void Engine::initRenderer() {

  renderer = newObject<drawing::Drawer>();
  renderer->init(this);

  addCleanup([=] {
    renderer->cleanup();
  });
}

void Engine::initScenes() {
  for (const auto scene : scenes) {
    scene->init(this);
  }

  addCleanup([=] {
    for (const auto scene : scenes) {
      scene->cleanup();
    }

    scenes.clear();
  });
}

void Engine::initAssetManager() {
  assetManager = newObject<assets::AssetManager>();
  assetManager->init(this);

  addCleanup([=] {
    assetManager->cleanup();
  });
}

assets::AssetManager * Engine::getAssetManager() const {
  return assetManager;
}

void Engine::notifyWindowResize() {
  int width,height;

  SDL_GetWindowSize(window,&width,&height);
  windowExtent.setWidth(width);
  windowExtent.setHeight(height);
}

void Engine::initInputManager() {
  inputManager = newObject<input::InputManager>();
  inputManager->init(this);
  addCleanup([=] {
    inputManager->cleanup();
  });
}

void Engine::init(void *outer) {
  Object::init(outer);
  if (_bWasAllocated) {
    log::engine->info("Engine was allocated");
  } else {
    log::engine->info("Engine was not allocated");
  }
  initAssetManager();
  initWindow();
  initInputManager();
  initRenderer();
  initScenes();
}

}
