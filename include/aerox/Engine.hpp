#pragma once
#include "Object.hpp"
#include "aerox/typedefs.hpp"
#include "containers/Array.hpp"
#include "containers/Set.hpp"
#include "containers/String.hpp"
#include "containers/TDelegate.hpp"
#include "math/Vec2.hpp"
#include "widgets/WidgetSubsystem.hpp"
#include "window/Window.hpp"
#include <vulkan/vulkan.hpp>
#include <queue>

namespace aerox::io {
class IoSubsystem;
}

namespace aerox::async {
class AsyncSubsystem;
}

namespace aerox::audio {
class AudioSubsystem;
}

namespace aerox::input {
class InputConsumer;
}

namespace aerox {
class EngineSubsystem;
}

namespace aerox::scripting {
class ScriptSubsystem;
}

namespace aerox::assets {
class AssetSubsystem;
}

namespace aerox::input {
class InputSubsystem;
}

namespace aerox::drawing {
class DrawingSubsystem;
}


namespace aerox {
namespace scene {
class Scene;
}

class Engine : public WithCleanupQueue {

private:
  vk::Extent2D _windowExtent{1000, 1000};

  std::weak_ptr<window::Window> _mainWindow;
  std::weak_ptr<window::Window> _focusedWindow;
  long long _runTime = 0;
  long long _lastTickTime = 0;
  float _lastDeltaSeconds = 0;
  String _applicationName;

  Array<std::shared_ptr<EngineSubsystem>> _subsystems;
  
  std::shared_ptr<drawing::DrawingSubsystem> _drawer;

  std::shared_ptr<input::InputSubsystem> _inputManager;

  std::shared_ptr<assets::AssetSubsystem> _assetManager;

  std::shared_ptr<scripting::ScriptSubsystem> _scriptManager;

  std::shared_ptr<widgets::WidgetSubsystem> _widgetManager;

  std::shared_ptr<audio::AudioSubsystem> _audioManager;

  std::shared_ptr<async::AsyncSubsystem> _asyncSubsystem;

  std::shared_ptr<io::IoSubsystem> _ioSubsystem;

  Set<std::shared_ptr<scene::Scene>, decltype([](const std::shared_ptr<scene::Scene>& a,const std::shared_ptr<scene::Scene>& b){
      return a == b;
  })> _scenes;

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

  void InitAsyncSubsystem();

  void InitIoSubsystem();

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

  Array<std::weak_ptr<scene::Scene>> GetScenes() const;

  std::weak_ptr<window::Window> GetMainWindow() const;

  vk::Extent2D GetMainWindowSize() const;

  std::weak_ptr<drawing::DrawingSubsystem> GetDrawingSubsystem() const;

  std::weak_ptr<input::InputSubsystem> GetInputSubsystem() const;

  std::weak_ptr<assets::AssetSubsystem> GetAssetSubsystem() const;

  std::weak_ptr<scripting::ScriptSubsystem> GetScriptSubsystem() const;

  std::weak_ptr<widgets::WidgetSubsystem> GetWidgetSubsystem() const;

  std::weak_ptr<audio::AudioSubsystem> GetAudioSubsystem() const;

  std::weak_ptr<async::AsyncSubsystem> GetAsyncSubsystem() const;

  std::weak_ptr<io::IoSubsystem> GetIoSubsystem() const;

  EInputMode GetInputMode() const;

  virtual std::shared_ptr<drawing::DrawingSubsystem> CreateDrawingSubsystem();
  virtual std::shared_ptr<input::InputSubsystem> CreateInputSubsystem();
  virtual std::shared_ptr<assets::AssetSubsystem> CreateAssetSubsystem();
  virtual std::shared_ptr<scripting::ScriptSubsystem> CreateScriptSubsystem();
  virtual std::shared_ptr<widgets::WidgetSubsystem> CreateWidgetSubsystem();
  virtual std::shared_ptr<audio::AudioSubsystem> CreateAudioSubsystem();
  virtual std::shared_ptr<async::AsyncSubsystem> CreateAsyncSubsystem();

  virtual std::shared_ptr<io::IoSubsystem> CreateIoSubsystem();

  void SetInputMode(EInputMode mode);

  void NotifyWindowResize();

  bool IsFullScreen() const;

  std::weak_ptr<window::Window> GetFocusedWindow() const;

  template <typename T,typename ... Args>
   TWeakConstruct<T> CreateScene(Args &&... args);

  virtual void InitScene(const std::shared_ptr<scene::Scene> &scene);

  // Events
  std::shared_ptr<TDelegate<EInputMode,EInputMode>> onInputModeChanged = std::make_shared<TDelegate<EInputMode,EInputMode>>();


  std::map<std::string,std::vector<Object *>> allocatedObjects;

  std::mutex allocationMutex;
private:

};

template <typename T, typename ... Args> TWeakConstruct<T> Engine::CreateScene(
    Args &&... args) {
  std::shared_ptr<T> rawObj = newObject<T>(args...);
  //const auto castObj = rawObj.Cast<scene::Scene>();
  if(IsRunning()) {
    InitScene(rawObj);
  }
  
  _scenes.Add(rawObj);
  return rawObj;
}

}
