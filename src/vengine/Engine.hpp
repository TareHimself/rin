#pragma once
//#define SDL_MAIN_HANDLED
#include "Object.hpp"

#include <SDL3/SDL.h>
#include "containers/Array.hpp"
#include <vulkan/vulkan.hpp>
#include <chrono>
#include <SDL3/SDL_video.h>

namespace vengine {
namespace assets {
class AssetManager;
}
}

namespace vengine {
namespace input {
class InputManager;
}
}

namespace vengine {
namespace drawing {
class Drawer;
}
}

namespace vengine {
namespace scene {
class Scene;
}

class Engine : public Object<void> {

  Engine( Engine const&) = delete;
  Engine operator=(const Engine&) = delete;

  

  vk::Extent2D windowExtent{ 1000, 1000};

  SDL_Window *window = nullptr;
  long long runTime = 0;
  long long lastTickTime = 0;
  std::string applicationName;

  drawing::Drawer * renderer = nullptr;

  input::InputManager * inputManager = nullptr;

  assets::AssetManager * assetManager = nullptr;

  Array<scene::Scene *> scenes;

  bool bExitRequested = false;

  bool bIsRunning = false;

  bool bIsMinimized = false;

  void update(float deltaTime);
  
  void initWindow();

  void initInputManager();
  
  void initRenderer();

  void initScenes();

  void initAssetManager();

  
  
public:

  void init(void *outer) override;
  
  /**
   * \brief Returns the current time since the engine started in milliseconds
   * \return 
   */
  static long long now();
  
  Engine();
  ~Engine() override;

  void setAppName(const std::string &newName);

  std::string getAppName();

  bool isRunning() const;

  bool shouldExit() const;

  void requestExit();
  
  void run();

  void addScene(scene::Scene * scene);

  float getEngineTimeSeconds() const;
  
  Array<scene::Scene *> getScenes();

  SDL_Window * getWindow() const;

  vk::Extent2D getWindowExtent() const;

  drawing::Drawer * getRenderer() const;

  input::InputManager * getInputManager() const;

  assets::AssetManager * getAssetManager() const;

  void notifyWindowResize();
  
private:

  
};
}
