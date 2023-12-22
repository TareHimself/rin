#include <iostream>
#include "Engine.hpp"
#include "rendering/Renderer.hpp"
#include "scene/Scene.hpp"

namespace vengine {

Engine::Engine() {
}

Engine::~Engine() {
}

void Engine::setApplicationName(std::string newName) {
  applicationName = newName;
  
}

std::string Engine::getApplicationName() {
  return applicationName;
}

bool Engine::isRunning() {
  return bIsRunning;
}

bool Engine::shouldExit() {
  return bExitRequested;
}

void Engine::requestExit() {
  bExitRequested = true;
}

void Engine::run() {
  initWindow();
  initRenderer();
  initScenes();
  
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
    }
    
    update(deltaFloat);

    renderer->render();
  
    lastTickTime = tickStart;
  }
  bIsRunning = false;
  destroyWorlds();
  destroyRenderer();
  destroyWindow();
  
}

void Engine::addScene(scene::Scene *scene) {
  scenes.push_back(scene);
  
  if(isRunning()) {
    scene->init();
  }
}

Array<scene::Scene *> Engine::getScenes() {
  return scenes;
}

SDL_Window * Engine::getWindow() {
  return window;
}

vk::Extent2D Engine::getWindowExtent() { return windowExtent; }

long long Engine::now() {
  return std::chrono::steady_clock::now().time_since_epoch().count() / 1000000;
}

void Engine::update(float deltaTime) {
  for(const auto scene : scenes) {
    scene->update(deltaTime);
  }
}


void Engine::initWindow() {
  SDL_Init(SDL_INIT_VIDEO);

  const auto flags = (SDL_WindowFlags)(SDL_WINDOW_VULKAN);

  window = SDL_CreateWindow(
    getApplicationName().c_str(),
    SDL_WINDOWPOS_UNDEFINED,
    SDL_WINDOWPOS_UNDEFINED,
    windowExtent.x,
    windowExtent.y,
    flags
  );
}

void Engine::initRenderer() {
  renderer = new rendering::Renderer();
  renderer->setEngine(this);
  renderer->init();
}

void Engine::initScenes() {
  for(const auto scene : scenes) {
    scene->init();
  }
}

void Engine::destroyWindow() {
  if(window != nullptr){
    SDL_DestroyWindow(window);
  }
}

void Engine::destroyRenderer() {
  renderer->destroy();
  delete renderer;
}

void Engine::destroyWorlds() {
  for(const auto scene : scenes) {
    scene->destroy();
    delete scene;
  }

  scenes.clear();
}
}
