#pragma once
//#define SDL_MAIN_HANDLED
#include "Object.hpp"

#include <SDL2/SDL.h>
#include "containers/Array.hpp"
#include "containers/Vector2.hpp"
#include <SDL2/SDL.h>
#include <vulkan/vulkan.hpp>
#include <chrono>
#include <vector>
#include <SDL2/SDL_video.h>

namespace vengine {
namespace input {
class InputManager;
}
}

namespace vengine {
namespace rendering {
class Renderer;
}
}

namespace vengine {
namespace scene {
class Scene;
}

class Engine : public Object<void> {

  Engine( Engine const&) = delete;
  Engine operator=(const Engine&) = delete;

  

  Vector2 windowExtent{ 500, 1000};

  SDL_Window *window = nullptr;
  long long runTime = 0;
  long long lastTickTime = 0;
  std::string applicationName;

  rendering::Renderer * renderer = nullptr;

  input::InputManager * inputManager = nullptr;

  Array<scene::Scene *> scenes;

  bool bExitRequested = false;

  bool bIsRunning = false;

  void update(float deltaTime);
  
  void initWindow();

  void initInputManager();
  
  void initRenderer();

  void initScenes();

  
  
public:

  void init(void *outer) override;
  static long long now();
  
  Engine();
  ~Engine() override;

  void setApplicationName(const std::string &newName);

  std::string getApplicationName();

  bool isRunning() const;

  bool shouldExit() const;

  void requestExit();
  
  void run();

  void addScene(scene::Scene * scene);

  Array<scene::Scene *> getScenes();

  SDL_Window * getWindow() const;

  vk::Extent2D getWindowExtent() const;

  rendering::Renderer * getRenderer() const;

  input::InputManager * getInputManager() const;
  
private:

  
};
}
