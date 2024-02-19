#pragma once
#include "Object.hpp"
#include "vengine/Managed.hpp"
#include "containers/Array.hpp"
#include "containers/Set.hpp"
#include "containers/String.hpp"
#include "containers/TDispatcher.hpp"
#include "math/Vector2.hpp"
#include "widget/WidgetSubsystem.hpp"
#include "window/Window.hpp"
#include <vulkan/vulkan.hpp>
#include <queue>

namespace vengine {
namespace audio {
class AudioSubsystem;
}
}

namespace vengine {
namespace input {
class InputConsumer;
}
}

namespace vengine {
class EngineSubsystem;
}

namespace vengine {
namespace scripting {
class ScriptSubsystem;
}
}

namespace vengine::assets {
class AssetSubsystem;
}

namespace vengine::input {
class InputSubsystem;
}

namespace vengine::drawing {
class DrawingSubsystem;
}

namespace vengine {
namespace scene {
class Scene;
}

class Engine : public Cleanable {

private:
  math::Vector2 _mousePosition{0, 0};
  vk::Extent2D _windowExtent{1000, 1000};

  Ref<window::Window> _mainWindow;
  Ref<window::Window> _focusedWindow;
  long long _runTime = 0;
  long long _lastTickTime = 0;
  float _lastDeltaSeconds = 0;
  String _applicationName;

  Array<Managed<EngineSubsystem>> _subsystems;
  
  Managed<drawing::DrawingSubsystem> _drawer;

  Managed<input::InputSubsystem> _inputManager;

  Managed<assets::AssetSubsystem> _assetManager;

  Managed<scripting::ScriptSubsystem> _scriptManager;

  Managed<widget::WidgetSubsystem> _widgetManager;

  Managed<audio::AudioSubsystem> _audioManager;

  Set<Managed<scene::Scene>> _scenes;

  EInputMode _inputMode = GameAndUi;
  
  bool bExitRequested = false;

  bool bIsRunning = false;

  bool bIsMinimized = false;

  bool bIsFocused = false;

  void Tick(float deltaTime);

  void InitWindow();

  void InitInputSubsystem();

  void InitScriptSubsystem();

  void InitWidgetSubsystem();

  void InitDrawingSubsystem();

  void InitScenes();

  void InitAssetSubsystem();

  void InitAudioSubsystem();

public:
  Engine();

  virtual void Init();

  static long long Now();

  void SetAppName(const String &newName);

  String GetAppName() const;

  bool IsRunning() const;

  bool ShouldExit() const;

  void RequestExit();
  
  static Engine * Get();
  void Run();

  void RunGame();

  void RunDraw() const;

  float GetEngineTimeSeconds() const;

  float GetDeltaSeconds() const;

  Array<Ref<scene::Scene>> GetScenes() const;

  Ref<window::Window> GetMainWindow() const;

  vk::Extent2D GetMainWindowSize() const;

  Ref<drawing::DrawingSubsystem> GetDrawingSubsystem() const;

  Ref<input::InputSubsystem> GetInputSubsystem() const;

  Ref<assets::AssetSubsystem> GetAssetSubsystem() const;

  Ref<scripting::ScriptSubsystem> GetScriptSubsystem() const;

  Ref<widget::WidgetSubsystem> GetWidgetSubsystem() const;

  Ref<audio::AudioSubsystem> GetAudioSubsystem() const;

  EInputMode GetInputMode() const;

  virtual Managed<drawing::DrawingSubsystem> CreateDrawingSubsystem();
  virtual Managed<input::InputSubsystem> CreateInputSubsystem();
  virtual Managed<assets::AssetSubsystem> CreateAssetSubsystem();
  virtual Managed<scripting::ScriptSubsystem> CreateScriptSubsystem();
  virtual Managed<widget::WidgetSubsystem> CreateWidgetSubsystem();

  virtual Managed<audio::AudioSubsystem> CreateAudioSubsystem();

  void SetInputMode(EInputMode mode);

  void NotifyWindowResize();

  bool IsFullScreen() const;

  Ref<window::Window> GetFocusedWindow() const;

  template <typename T,typename ... Args>
   Ref<T> CreateScene(Args &&... args);

  virtual void InitScene(const Managed<scene::Scene> &scene);

  // Events
  TDispatcher<EInputMode, EInputMode> onInputModeChanged;

  // Events
  TDispatcher<vk::Extent2D> onWindowSizeChanged;

private:

};

template <typename T, typename ... Args> Ref<T> Engine::CreateScene(
    Args &&... args) {
  Managed<T> rawObj = newManagedObject<T>(args...);
  //const auto castObj = rawObj.Cast<scene::Scene>();
  if(IsRunning()) {
    InitScene(rawObj);
  }
  
  _scenes.Add(rawObj);
  return rawObj;
}
}
