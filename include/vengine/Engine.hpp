#pragma once

//#define SDL_MAIN_HANDLED
#include "Object.hpp"
#include "vengine/Ref.hpp"
#include "utils.hpp"
#include <SDL3/SDL.h>
#include "containers/Array.hpp"
#include "containers/Set.hpp"
#include "containers/String.hpp"
#include "containers/TEventDispatcher.hpp"
#include "math/Vector2.hpp"
#include <vulkan/vulkan.hpp>
#include <queue>
#include <SDL3/SDL_video.h>

namespace vengine {
namespace input {
class InputConsumer;
}
}

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

class Engine : public Cleanable {

private:
  math::Vector2 _mousePosition{0, 0};
  vk::Extent2D _windowExtent{1000, 1000};

  Ref<SDL_Window> _window;
  long long _runTime = 0;
  long long _lastTickTime = 0;
  String _applicationName;

  Array<Ref<EngineSubsystem>> _subsystems;
  
  Ref<drawing::Drawer> _drawer;

  Ref<input::InputManager> _inputManager;

  Ref<assets::AssetManager> _assetManager;

  Ref<scripting::ScriptManager> _scriptManager;

  Ref<widget::WidgetManager> _widgetManager;

  Set<Ref<scene::Scene>> _scenes;

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

  virtual void Init();

  static long long Now();

  void SetAppName(const String &newName);

  String GetAppName() const;

  bool IsRunning() const;

  bool ShouldExit() const;

  void RequestExit();

  void Run();

  void RunGame();

  void RunDraw() const;

  float GetEngineTimeSeconds() const;

  Array<WeakRef<scene::Scene>> GetScenes() const;

  WeakRef<SDL_Window> GetWindow() const;

  vk::Extent2D GetWindowExtent() const;

  WeakRef<drawing::Drawer> GetDrawer() const;

  WeakRef<input::InputManager> GetInputManager() const;

  WeakRef<assets::AssetManager> GetAssetManager() const;

  WeakRef<scripting::ScriptManager> GetScriptManager() const;

  WeakRef<widget::WidgetManager> GetWidgetManager() const;

  virtual Ref<drawing::Drawer> CreateDrawer();
  virtual Ref<input::InputManager> CreateInputManager();
  virtual Ref<assets::AssetManager> CreateAssetManager();
  virtual Ref<scripting::ScriptManager> CreateScriptManager();
  virtual Ref<widget::WidgetManager> CreateWidgetManager();

  void SetInputMode(EInputMode mode);

  void NotifyWindowResize();

  bool IsFullScreen() const;

  bool IsFocused() const;

  template <typename T,typename ... Args>
   WeakRef<T> CreateScene(Args &&... args);

  virtual void InitScene(const Ref<scene::Scene> &scene);

  // Events
  TEventDispatcher<EInputMode, EInputMode> onInputModeChanged;

  // Events
  TEventDispatcher<vk::Extent2D> onWindowSizeChanged;

private:

};

template <typename T, typename ... Args> WeakRef<T> Engine::CreateScene(
    Args &&... args) {
  Ref<T> rawObj = newSharedObject<T>(args...);
  //const auto castObj = rawObj.Cast<scene::Scene>();
  if(IsRunning()) {
    InitScene(rawObj);
  }
  
  _scenes.Add(rawObj);
  return rawObj;
}
}
