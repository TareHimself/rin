#include <iostream>
#include "Engine.hpp"

#include "rendering/Renderer.hpp"
#include "world/World.hpp"

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
  return bExitRequested || glfwWindowShouldClose(window);
}

void Engine::requestExit() {
  bExitRequested = true;
}

void Engine::run() {
  initRenderer();
  initWindow();
  initWorlds();
  
  bIsRunning = true;
  lastTickTime = now();
  
  while (!shouldExit()) {
    const auto tickStart = now();
    const auto delta = tickStart - lastTickTime;
    runTime += delta;
    const auto deltaFloat = static_cast<float>(static_cast<double>(delta) / 1000.0);
    glfwPollEvents();
    
    update(deltaFloat);
  
    lastTickTime = tickStart;
  }
  bIsRunning = false;
  destroyWindow();
  destroyWorlds();
  destroyRenderer();
}

void Engine::addWorld(world::World *world) {
  worlds.push_back(world);
  
  if(isRunning()) {
    world->init();
  }
}

GLFWwindow * Engine::getWindow() {
  return window;
}

long long Engine::now() {
  return std::chrono::steady_clock::now().time_since_epoch().count() / 1000000;
}

void Engine::update(float deltaTime) {
  for(const auto world : worlds) {
    world->update(deltaTime);
  }
}


void Engine::initWindow() {
  glfwInit();

  glfwWindowHint(GLFW_CLIENT_API,GLFW_NO_API);
  glfwWindowHint(GLFW_RESIZABLE,GLFW_FALSE);

  const uint32_t WIDTH = 800;
  const uint32_t HEIGHT = 600;

  window = glfwCreateWindow(WIDTH, HEIGHT, getApplicationName().c_str(), nullptr, nullptr);
}

void Engine::initRenderer() {
  renderer = new rendering::Renderer();
  renderer->setEngine(this);
  renderer->init();
}

void Engine::initWorlds() {
  for(const auto world : worlds) {
    world->init();
  }
}

void Engine::destroyWindow() {
  glfwTerminate();
}

void Engine::destroyRenderer() {
  renderer->destroy();
  delete renderer;
}

void Engine::destroyWorlds() {
  for(const auto world : worlds) {
    world->destroy();
    delete world;
  }

  worlds.clear();
}
}
