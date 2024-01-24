#pragma once

//#define SDL_MAIN_HANDLED
#include "Object.hpp"
#include "utils.hpp"
#include <SDL3/SDL.h>
#include "containers/Array.hpp"
#include "containers/TEventDispatcher.hpp"
#include "math/Vector2.hpp"
#include <vulkan/vulkan.hpp>
#include <queue>
#include <SDL3/SDL_video.h>

namespace vengine {
namespace widget {
class WidgetManager;
}
}

namespace vengine {
class EngineSubsystem;
}

namespace vengine {
namespace scripting {
class ScriptManager;
}
}

namespace vengine::assets {
class AssetManager;
}

namespace vengine::input {
class InputManager;
}

namespace vengine::drawing {
class Drawer;
}

namespace vengine {
namespace scene {
class Scene;
}

class Engine : public Object<void> {

private:
  math::Vector2 _mousePosition{0, 0};
  vk::Extent2D _windowExtent{1000, 1000};

  SDL_Window *_window = nullptr;
  long long _runTime = 0;
  long long _lastTickTime = 0;
  std::string _applicationName;

  Array<EngineSubsystem *> _subsystems;
  
  drawing::Drawer *_drawer = nullptr;

  input::InputManager *_inputManager = nullptr;

  assets::AssetManager *_assetManager = nullptr;

  scripting::ScriptManager *_scriptManager = nullptr;

  widget::WidgetManager *_widgetManager = nullptr;

  Array<scene::Scene *> _scenes;

  EInputMode _inputMode = GameAndUi;

  std::queue<SDL_Event> _windowEventQueue;

  bool bExitRequested = false;

  bool bIsRunning = false;

  bool bIsMinimized = false;

  bool bIsFocused = false;

  void Update(float deltaTime) const;

  void InitWindow();

  void InitInputManager();

  void InitScriptManager();

  void InitWidgetManager();

  void InitDrawer();

  void InitScenes();

  void InitAssetManager();

public:
  Engine();

  void Init(void *outer) override;

  static long long Now();

  void SetAppName(const std::string &newName);

  std::string GetAppName();

  bool IsRunning() const;

  bool ShouldExit() const;

  void RequestExit();

  void Run();

  void RunGame();

  void RunWindowEvents();

  void RunDraw() const;

  void AddScene(scene::Scene *scene);

  float GetEngineTimeSeconds() const;

  Array<scene::Scene *> GetScenes();

  SDL_Window *GetWindow() const;

  vk::Extent2D GetWindowExtent() const;

  drawing::Drawer *GetDrawer() const;

  input::InputManager *GetInputManager() const;

  assets::AssetManager *GetAssetManager() const;

  scripting::ScriptManager *GetScriptManager() const;

  widget::WidgetManager *GetWidgetManager() const;

  virtual drawing::Drawer * CreateDrawer();
  virtual input::InputManager * CreateInputManager();
  virtual assets::AssetManager * CreateAssetManager();
  virtual scripting::ScriptManager * CreateScriptManager();
  virtual widget::WidgetManager * CreateWidgetManager();

  void SetInputMode(EInputMode mode);

  void NotifyWindowResize();

  bool IsFullScreen() const;

  bool IsFocused() const;

  template <typename T,typename ... Args>
  T * CreateScene(Args &&... args);

  virtual void InitScene(scene::Scene * scene);

  // Events
  TEventDispatcher<EInputMode, EInputMode> onInputModeChanged;

  // Events
  TEventDispatcher<vk::Extent2D> onWindowSizeChanged;

private:

};

template <typename T, typename ... Args> T * Engine::CreateScene(
    Args &&... args) {
  auto rawObj = newObject<T>(args...);
  const auto castObj = static_cast<scene::Scene *>(rawObj);
  InitScene(castObj);
  return rawObj;
}
}
