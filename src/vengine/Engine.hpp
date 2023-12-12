#pragma once
#include <GLFW/glfw3.h>
#include <chrono>
#include <vector>

namespace vengine {
namespace rendering {
class Renderer;
}
}

namespace vengine {
namespace world {
class World;
}
}

namespace vengine {
class Engine {

  Engine( Engine const&) = delete;
  Engine operator=(const Engine&) = delete;
  
  GLFWwindow *window = nullptr;
  long long runTime = 0;
  long long lastTickTime = 0;
  std::string applicationName;

  rendering::Renderer * renderer = nullptr;

  std::vector<world::World *> worlds;

  bool bExitRequested = false;

  bool bIsRunning = false;

  void update(float deltaTime);
  
  void initWindow();
  
  void initRenderer();

  void initWorlds();

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

  void addWorld(world::World * world);

  GLFWwindow * getWindow();
  
private:

  
};
}
