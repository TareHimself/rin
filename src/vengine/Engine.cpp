#include <iostream>
#include "Engine.hpp"

#include "input/InputManager.hpp"
#include "rendering/Renderer.hpp"
#include "scene/Scene.hpp"

namespace vengine {

Engine::Engine() = default;

Engine::~Engine() {
}

void Engine::setApplicationName(const std::string &newName) {
  applicationName = newName;
  
}

std::string Engine::getApplicationName() {
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
    const auto deltaFloat = static_cast<float>(static_cast<double>(delta) / 1000.0);

    SDL_Event e;
    
    while(SDL_PollEvent(&e) != 0){

      if(e.type == SDL_QUIT){
        bShouldQuit = true;
        break;
      }
      else if(e.type == SDL_KEYDOWN && e.key.repeat == 0) {
        inputManager->receiveKeyPressedEvent(e.key);
      }else if(e.type == SDL_KEYUP) {
        inputManager->receiveKeyReleasedEvent(e.key);
      }
    }
    
    update(deltaFloat);

    renderer->render();
  
    lastTickTime = tickStart;
  }
  bIsRunning = false;
  cleanup();
}

void Engine::addScene(scene::Scene *scene) {
  scenes.push_back(scene);
  
  if(isRunning()) {
    scene->init(this);
  }
}

Array<scene::Scene *> Engine::getScenes() {
  return scenes;
}

SDL_Window * Engine::getWindow() const {
  return window;
}

vk::Extent2D Engine::getWindowExtent() const { return windowExtent; }

rendering::Renderer * Engine::getRenderer() const {
  return renderer;
}

input::InputManager * Engine::getInputManager() const {
  return  inputManager;
}

long long Engine::now() {
  return std::chrono::steady_clock::now().time_since_epoch().count() / 1000000;
}

void Engine::update(float deltaTime) {
  for(const auto scene : scenes) {
    scene->update(deltaTime);
  }
}


void Engine::initWindow() {
  // SDL_SetMainReady();
  SDL_Init(SDL_INIT_VIDEO);

  constexpr auto flags = (SDL_WindowFlags)(SDL_WINDOW_VULKAN);

  window = SDL_CreateWindow(
    getApplicationName().c_str(),
    SDL_WINDOWPOS_UNDEFINED,
    SDL_WINDOWPOS_UNDEFINED,
    windowExtent.x,
    windowExtent.y,
    flags
  );

  addCleanup([=] {
    if(window != nullptr){
    SDL_DestroyWindow(window);
    }
  });
}

void Engine::initRenderer() {
  
  renderer = newObject<rendering::Renderer>();
  renderer->init(this);

  addCleanup([=] {
    renderer->cleanup();
  });
}

void Engine::initScenes() {
  for(const auto scene : scenes) {
    scene->init(this);
  }

  addCleanup([=] {
    for(const auto scene : scenes) {
    scene->cleanup();
  }

  scenes.clear();
  });
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
  if(_bWasAllocated) {
    log::engine->info("Engine was allocated");
  } else {
    log::engine->info("Engine was not allocated");
  }
  initWindow();
  initInputManager();
  initRenderer();
  initScenes();
}

}
