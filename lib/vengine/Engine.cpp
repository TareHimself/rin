#include "vengine/audio/AudioSubsystem.hpp"
#include "vengine/audio/LiveAudio.hpp"
#include "vengine/containers/TAsyncTask.hpp"
#include <iostream>
#include <vengine/Engine.hpp>
#include <vengine/assets/AssetSubsystem.hpp>
#include <vengine/input/InputSubsystem.hpp>
#include <vengine/drawing/DrawingSubsystem.hpp>
#include <vengine/scene/Scene.hpp>
#include <chrono>
#include <thread>
#include <vengine/scripting/ScriptSubsystem.hpp>
#include <vengine/widget/WidgetSubsystem.hpp>
#include <bass/utils.hpp>

namespace vengine {

void Engine::SetAppName(const String &newName) {
  _applicationName = newName;

}

String Engine::GetAppName() const {
  return _applicationName;
}

bool Engine::IsRunning() const {
  return bIsRunning;
}

bool Engine::ShouldExit() const {
  return bExitRequested;
}

void Engine::RequestExit() {
  bExitRequested = true;
}

Engine * Engine::Get() {
  static auto instance = new Engine();
  return instance;
}

void Engine::Run() {
  bIsRunning = true;
  Init();

  _lastTickTime = Now();

  std::thread drawThread([this] {
    RunDraw();
  });

  RunGame();
  drawThread.join();
  
  bIsRunning = false;
  
  Destroy();
  
  delete this;
}

void Engine::RunGame() {
  while (bIsRunning && !ShouldExit()) {
    const auto tickStart = Now();
    const auto delta = tickStart - _lastTickTime;
    _runTime += delta;
    const auto deltaFloat = static_cast<float>(
      static_cast<double>(delta) / 1000.0);

    window::getManager()->Poll();

    if (_mainWindow) {
      bExitRequested = bExitRequested || GetMainWindow().Reserve()->CloseRequested();
    }
    // while (_window->pollEvent(e)) {
    //
    //   _inputManager->ProcessEvent(e);
    //   switch (e.type) {
    //   case sf::Event::Closed:
    //     bExitRequested = true;
    //     break;
    //   case sf::Event::LostFocus:
    //     bIsFocused = false;
    //     bIsMinimized = true;
    //     break;
    //
    //   case sf::Event::GainedFocus:
    //     bIsFocused = true;
    //     bIsMinimized = false;
    //     break;
    //   case sf::Event::Resized:
    //     NotifyWindowResize();
    //     break;
    //   case sf::Event::MouseMoved:
    //     _mousePosition = math::Vector2{e.mouseMove.x, e.mouseMove.y};
    //     break;
    //   default:
    //     break;
    //   }
    // }

    _inputManager->CheckMouse(deltaFloat);

    Tick(deltaFloat);

    if (!bIsMinimized) {
      _drawer->Draw();
    }

    _lastTickTime = tickStart;
  }
}

void Engine::RunDraw() const {
  while (!ShouldExit()) {
    std::this_thread::sleep_for(std::chrono::milliseconds{1000});
    // if(bIsMinimized) {
    //   std::this_thread::sleep_for(std::chrono::milliseconds{1000});
    //   return;
    // }

    // _drawer->Draw();
  }
}

float Engine::GetEngineTimeSeconds() const {
  return static_cast<float>(_runTime / 1000.0);
}

float Engine::GetDeltaSeconds() const {
  return _lastDeltaSeconds;
}

Array<Ref<scene::Scene>> Engine::GetScenes() const {
  Array<Ref<scene::Scene>> scenes;
  for (auto &scene : _scenes) {
    scenes.push(scene);
  }
  return scenes;
}

Ref<window::Window> Engine::GetMainWindow() const {
  return _mainWindow;
}

vk::Extent2D Engine::GetMainWindowSize() const { return _windowExtent; }

Ref<drawing::DrawingSubsystem> Engine::GetDrawingSubsystem() const {
  return _drawer;
}

Ref<input::InputSubsystem> Engine::GetInputSubsystem() const {
  return _inputManager;
}

long long Engine::Now() {
  return std::chrono::steady_clock::now().time_since_epoch().count() / 1000000;
}

Engine::Engine() {
  log::engine->info("Created new engine instance");
};

void Engine::Init() {
  AddCleanup([] {
    AsyncTaskManager::Get()->Tick(0.0f);
    AsyncTaskManager::Get()->Clear();
  });

  InitWindow();
  InitAssetSubsystem();
  InitDrawingSubsystem();
  InitAudioSubsystem();
  InitInputSubsystem();
  InitWidgetSubsystem();
  InitScriptSubsystem();
  InitScenes();

  // Multiple window test
  // GetMainWindow().Reserve()->CreateChild(_windowExtent.width, _windowExtent.height,"Child A");
  // GetMainWindow().Reserve()->CreateChild(_windowExtent.width, _windowExtent.height,"Child B");
  // GetMainWindow().Reserve()->CreateChild(_windowExtent.width, _windowExtent.height,"Child C");

  auto testInputConsumer = GetInputSubsystem().Reserve()->Consume<
    input::InputConsumer>().Reserve();

  testInputConsumer->BindKey(window::EKey::Key_Escape,
                             [this](const std::shared_ptr<input::KeyInputEvent> &
                             e) {
                               RequestExit();
                               return true;
                             }, {});

  testInputConsumer->BindKey(window::EKey::Key_LeftShift,
                             [this](const std::shared_ptr<input::KeyInputEvent> &
                             e) {
                               SetInputMode(EInputMode::GameOnly);
                               return true;
                             }, [this](
                             const std::shared_ptr<input::KeyInputEvent> &_) {
                               SetInputMode(EInputMode::UiOnly);
                               return true;
                             });

  testInputConsumer->BindKey(window::EKey::Key_J,
                             [this](
                             const std::shared_ptr<input::KeyInputEvent> &e) {
                               if (auto sound = GetAudioSubsystem().Reserve()->
                                   PlaySound2D(
                                       R"(D:\BH & Kirk Cosier - Slipping Away (ft. Cheney).wav)")) {
                                 if (sound->Play()) {
                                   log::engine->info("Playing audio");
                                 } else {
                                   log::engine->info("Failed to play Audio");
                                   auto err = bass::bassErrorToString(
                                       static_cast<bass::EBassError>(
                                         GetLastError()));

                                 }
                               }

                               return true;
                             }, {});

  SetInputMode(EInputMode::UiOnly);
}

void Engine::Tick(float deltaTime) {
  AsyncTaskManager::Get()->Tick(deltaTime);
  _lastDeltaSeconds = deltaTime;
  for (const auto &scene : _scenes) {
    scene->Tick(deltaTime);
  }
}


void Engine::InitWindow() {
  window::getManager()->Start();

  AddCleanup(window::getManager()->onWindowFocusChanged,window::getManager()->onWindowFocusChanged.Bind([this](const window::Window * win, bool val) {
    if(val) {
      _focusedWindow = win->ToRef();
      log::engine->info("Set focused window to {}",_focusedWindow.Reserve()->GetId());
    }
  }));
  
  _mainWindow = window::create(_windowExtent.width, _windowExtent.height,
                                         GetAppName(), nullptr);
  _focusedWindow = _mainWindow;
  //SDL_SetWindowFullscreen(window,true);
  //SDL_CaptureMouse(true);

  AddCleanup([this] {
    window::destroy(_mainWindow.Reserve()->GetId());
    window::getManager()->Stop();
  });
}

void Engine::InitDrawingSubsystem() {

  _drawer = CreateDrawingSubsystem();
  _drawer->Init(this);

  AddCleanup([this] {
    _drawer.Clear();
  });
}

void Engine::InitScenes() {
  for (const auto &scene : _scenes) {
    InitScene(scene);
  }

  AddCleanup([this] {
    _scenes.clear();
  });
}

void Engine::InitAssetSubsystem() {
  _assetManager = CreateAssetSubsystem();
  _assetManager->Init(this);

  AddCleanup([this] {
    _assetManager.Clear();
  });
}

void Engine::InitAudioSubsystem() {
  _audioManager = CreateAudioSubsystem();
  _audioManager->Init(this);

  AddCleanup([this] {
    _audioManager.Clear();
  });
}

Ref<assets::AssetSubsystem> Engine::GetAssetSubsystem() const {
  return _assetManager;
}

Ref<scripting::ScriptSubsystem> Engine::GetScriptSubsystem() const {
  return _scriptManager;
}

Ref<widget::WidgetSubsystem> Engine::GetWidgetSubsystem() const {
  return _widgetManager;
}

Ref<audio::AudioSubsystem> Engine::GetAudioSubsystem() const {
  return _audioManager;
}

EInputMode Engine::GetInputMode() const {
  return _inputMode;
}

Managed<drawing::DrawingSubsystem> Engine::CreateDrawingSubsystem() {
  return newManagedObject<drawing::DrawingSubsystem>();
}

Managed<input::InputSubsystem> Engine::CreateInputSubsystem() {
  return newManagedObject<input::InputSubsystem>();
}

Managed<assets::AssetSubsystem> Engine::CreateAssetSubsystem() {
  return newManagedObject<assets::AssetSubsystem>();
}

Managed<scripting::ScriptSubsystem> Engine::CreateScriptSubsystem() {
  return newManagedObject<scripting::ScriptSubsystem>();
}

Managed<widget::WidgetSubsystem> Engine::CreateWidgetSubsystem() {
  return newManagedObject<widget::WidgetSubsystem>();
}

Managed<audio::AudioSubsystem> Engine::CreateAudioSubsystem() {
  return newManagedObject<audio::AudioSubsystem>();
}

void Engine::SetInputMode(EInputMode mode) {

  if(auto focused = GetFocusedWindow().Reserve()) {
    switch (mode) {
    case EInputMode::GameOnly:
      focused->SetCursorMode(window::CursorMode_Captured);
      break;

    case EInputMode::UiOnly:
      focused->SetCursorMode(window::CursorMode_Visible);
      break;

    case EInputMode::GameAndUi:
      focused->SetCursorMode(window::CursorMode_Visible);
      break;

    }

    onInputModeChanged(_inputMode, mode);

    _inputMode = mode;
  }
}

void Engine::NotifyWindowResize() {
  int width, height;
  const auto winSize = _mainWindow.Reserve()->GetSize();
  _windowExtent.setHeight(winSize.y);
  _windowExtent.setWidth(winSize.x);

  if (IsFullScreen()) {
    // const auto display = SDL_GetPrimaryDisplay();
    // const auto dm = SDL_GetDesktopDisplayMode(display);
    // width = dm->w;
    // height = dm->h;
    //SDL_CaptureMouse(true);
  } else {
    //SDL_GetWindowSize(_window.Get(),&width,&height);

  }
  //
  // if (_drawer->GetDrawImageExtent() != _windowExtent) {
  //   _drawer->RequestResize();
  // }
}

bool Engine::IsFullScreen() const {
  return false;
}

Ref<window::Window> Engine::GetFocusedWindow() const {
  return _focusedWindow;
}


void Engine::InitScene(const Managed<scene::Scene> &scene) {
  if (IsRunning()) {
    scene->Init(this);
  }
}

void Engine::InitInputSubsystem() {
  _inputManager = CreateInputSubsystem();
  _inputManager->Init(this);
  AddCleanup([this] {
    _inputManager.Clear();
  });
}

void Engine::InitScriptSubsystem() {
  _scriptManager = CreateScriptSubsystem();
  _scriptManager->Init(this);
  AddCleanup([this] {
    _scriptManager.Clear();
  });
}

void Engine::InitWidgetSubsystem() {
  _widgetManager = CreateWidgetSubsystem();
  _widgetManager->Init(this);
  AddCleanup([this] {
    _widgetManager.Clear();
  });
}

}
