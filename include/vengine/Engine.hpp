#pragma once

//#define SDL_MAIN_HANDLED
#include "Object.hpp"
#include "vengine/Pointer.hpp"
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

  Pointer<SDL_Window> _window;
  long long _runTime = 0;
  long long _lastTickTime = 0;
  String _applicationName;

  Array<Pointer<EngineSubsystem>> _subsystems;
  
  Pointer<drawing::Drawer> _drawer;

  Pointer<input::InputManager> _inputManager;

  Pointer<assets::AssetManager> _assetManager;

  Pointer<scripting::ScriptManager> _scriptManager;

  Pointer<widget::WidgetManager> _widgetManager;

  Set<Pointer<scene::Scene>> _scenes;

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

  Array<WeakPointer<scene::Scene>> GetScenes() const;

  WeakPointer<SDL_Window> GetWindow() const;

  vk::Extent2D GetWindowExtent() const;

  WeakPointer<drawing::Drawer> GetDrawer() const;

  WeakPointer<input::InputManager> GetInputManager() const;

  WeakPointer<assets::AssetManager> GetAssetManager() const;

  WeakPointer<scripting::ScriptManager> GetScriptManager() const;

  WeakPointer<widget::WidgetManager> GetWidgetManager() const;

  virtual Pointer<drawing::Drawer> CreateDrawer();
  virtual Pointer<input::InputManager> CreateInputManager();
  virtual Pointer<assets::AssetManager> CreateAssetManager();
  virtual Pointer<scripting::ScriptManager> CreateScriptManager();
  virtual Pointer<widget::WidgetManager> CreateWidgetManager();

  void SetInputMode(EInputMode mode);

  void NotifyWindowResize();

  bool IsFullScreen() const;

  bool IsFocused() const;

  template <typename T,typename ... Args>
   WeakPointer<T> CreateScene(Args &&... args);

  virtual void InitScene(const Pointer<scene::Scene> &scene);

  // Events
  TEventDispatcher<EInputMode, EInputMode> onInputModeChanged;

  // Events
  TEventDispatcher<vk::Extent2D> onWindowSizeChanged;

private:

};

template <typename T, typename ... Args> WeakPointer<T> Engine::CreateScene(
    Args &&... args) {
  Pointer<T> rawObj = newSharedObject<T>(args...);
  //const auto castObj = rawObj.Cast<scene::Scene>();
  if(IsRunning()) {
    InitScene(rawObj);
  }
  
  _scenes.Add(rawObj);
  return rawObj;
}
}
