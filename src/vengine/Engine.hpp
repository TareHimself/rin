#pragma once
#include "containers/Array.hpp"
#include "containers/Vector2.hpp"

#include <SDL2/SDL.h>
#include <vulkan/vulkan.hpp>
#include <chrono>
#include <vector>
#include <SDL2/SDL_video.h>

namespace vengine {
namespace rendering {
class Renderer;
}
}

namespace vengine {
namespace scene {
class Scene;
}

class Engine {

  Engine( Engine const&) = delete;
  Engine operator=(const Engine&) = delete;

  

  Vector2 windowExtent{ 800, 600};

  SDL_Window *window = nullptr;
  long long runTime = 0;
  long long lastTickTime = 0;
  std::string applicationName;

  rendering::Renderer * renderer = nullptr;

  Array<scene::Scene *> scenes;

  bool bExitRequested = false;

  bool bIsRunning = false;

  void update(float deltaTime);
  
  void initWindow();
  
  void initRenderer();

  void initScenes();

  void destroyWindow();
  
  void destroyRenderer();

  void destroyWorlds();
  
public:

  static long long now();
  
  Engine();
  ~Engine();

  void setApplicationName(std::string newName);

  std::string getApplicationName();

  bool isRunning();

  bool shouldExit();

  void requestExit();
  
  void run();

  void addScene(scene::Scene * scene);

  Array<scene::Scene *> getScenes();

  SDL_Window * getWindow();

  vk::Extent2D getWindowExtent();
  
private:

  
};
}
