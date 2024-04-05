#include "aerox/async/Task.hpp"
#include "aerox/audio/AudioSubsystem.hpp"
#include "aerox/audio/LiveAudio.hpp"
#include <aerox/async/AsyncSubsystem.hpp>
#include <aerox/Engine.hpp>
#include <aerox/assets/AssetSubsystem.hpp>
#include <aerox/input/InputSubsystem.hpp>
#include <aerox/drawing/DrawingSubsystem.hpp>
#include <aerox/scene/Scene.hpp>
#include <chrono>
#include <thread>
#include <aerox/scripting/ScriptSubsystem.hpp>
#include <aerox/widgets/WidgetSubsystem.hpp>
#include <aerox/io/IoSubsystem.hpp>
#include <bass/utils.hpp>

namespace aerox {

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

Engine *Engine::Get() {
  static auto instance = std::make_shared<Engine>();
  return instance.get();
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

  _asyncSubsystem->StopAll();
  Clean();
}

void Engine::RunGame() {
  while (bIsRunning && !ShouldExit()) {
    const auto tickStart = Now();
    const auto delta = tickStart - _lastTickTime;
    _runTime += delta;
    const auto deltaFloat = static_cast<float>(
      static_cast<double>(delta) / 1000.0);

    window::getManager()->Poll();

    if (!_mainWindow.expired()) {
      bExitRequested = bExitRequested || GetMainWindow().lock()->
                       CloseRequested();
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
    //     _mousePosition = math::Vec2{e.mouseMove.x, e.mouseMove.y};
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

Array<std::weak_ptr<scene::Scene>> Engine::GetScenes() const {
  Array<std::weak_ptr<scene::Scene>> scenes;
  for (auto &scene : _scenes) {
    scenes.push(scene);
  }
  return scenes;
}

std::weak_ptr<window::Window> Engine::GetMainWindow() const {
  return _mainWindow;
}

vk::Extent2D Engine::GetMainWindowSize() const { return _windowExtent; }

std::weak_ptr<drawing::DrawingSubsystem> Engine::GetDrawingSubsystem() const {
  return _drawer;
}

std::weak_ptr<input::InputSubsystem> Engine::GetInputSubsystem() const {
  return _inputManager;
}

long long Engine::Now() {
  return std::chrono::steady_clock::now().time_since_epoch().count() / 1000000;
}

Engine::Engine() {
  log::engine->Info("Created new engine instance");
};

void Engine::Init() {
  InitAsyncSubsystem();
  InitIoSubsystem();
  InitWindow();
  InitAssetSubsystem();
  InitDrawingSubsystem();
  InitAudioSubsystem();
  InitInputSubsystem();
  InitWidgetSubsystem();
  InitScriptSubsystem();
  InitScenes();

  // Multiple window test
  // auto w = GetMainWindow().lock()->CreateChild(_windowExtent.width, _windowExtent.height,"Child A");
  // w.lock()->onCloseRequested->BindFunction([](const std::weak_ptr<window::Window> & w) {
  //   window::getManager()->Destroy(w.lock()->GetId());
  // });
  // GetMainWindow().Reserve()->CreateChild(_windowExtent.width, _windowExtent.height,"Child B");
  // GetMainWindow().Reserve()->CreateChild(_windowExtent.width, _windowExtent.height,"Child C");

  auto testInputConsumer = GetInputSubsystem().lock()->Consume<
    input::InputConsumer>().lock();

  testInputConsumer->BindKey(window::EKey::Key_Escape,
                             [this](
                             const std::shared_ptr<input::KeyInputEvent> &
                             e) {
                               RequestExit();
                               return true;
                             }, {});

  testInputConsumer->BindKey(window::EKey::Key_LeftShift,
                             [this](
                             const std::shared_ptr<input::KeyInputEvent> &
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
                               const auto task = async::newTask([] {
                                 std::vector<fs::path> files;
                                 io::IoSubsystem::SelectFiles(
                                     files, false, "Select Track",
                                     "*.wav;*.mp3;*.ogg");
                                 if (!files.empty()) {
                                   if (const auto sound = Engine::Get()->
                                       GetAudioSubsystem().lock()->
                                       PlaySound2D(files.front())) {
                                     if (sound->Play()) {
                                       log::engine->Info(
                                           "Playing {}", files.front().string().c_str());
                                     } else {
                                       log::engine->Info(
                                           "Failed to play sound {}", files.front().string().c_str());
                                       auto err = bass::bassErrorToString(
                                           static_cast<bass::EBassError>(
                                             GetLastError()));

                                     }
                                   } else {
                                     log::engine->Info(
                                           "Failed to load sound {}", files.front().string().c_str());
                                       auto err = bass::bassErrorToString(
                                           static_cast<bass::EBassError>(
                                             GetLastError()));
                                   }
                                 }

                               });

                               task->Enqueue();

                               return true;
                             }, {});

  SetInputMode(EInputMode::UiOnly);
}

void Engine::Tick(float deltaTime) {
  _lastDeltaSeconds = deltaTime;
  for (const auto &scene : _scenes) {
    scene->Tick(deltaTime);
  }

  _widgetManager->Tick(deltaTime);
}


void Engine::InitWindow() {
  window::getManager()->Start();

  AddCleanup(window::getManager()->onWindowFocusChanged->BindFunction(
      [this](const std::weak_ptr<window::Window> &win, bool val) {
        if (val) {
          _focusedWindow = win;
          log::engine->Info("Set focused window to {}",
                            _focusedWindow.lock()->GetId());
        }
      }));

  _mainWindow = window::create(_windowExtent.width, _windowExtent.height,
                               GetAppName(), {});
  _focusedWindow = _mainWindow;
  //SDL_SetWindowFullscreen(window,true);
  //SDL_CaptureMouse(true);

  AddCleanup([this] {
    window::destroy(_mainWindow.lock()->GetId());
    window::getManager()->Stop();
  });
}

void Engine::InitDrawingSubsystem() {

  _drawer = CreateDrawingSubsystem();
  _drawer->Init(this);

  AddCleanup([this] {
    _drawer.reset();
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
    _assetManager.reset();
  });
}

void Engine::InitAudioSubsystem() {
  _audioManager = CreateAudioSubsystem();
  _audioManager->Init(this);

  AddCleanup([this] {
    _audioManager.reset();
  });
}

void Engine::InitAsyncSubsystem() {
  _asyncSubsystem = CreateAsyncSubsystem();
  _asyncSubsystem->Init(this);

  AddCleanup([this] {
    _asyncSubsystem.reset();
  });
}

void Engine::InitIoSubsystem() {
  _ioSubsystem = CreateIoSubsystem();
  _ioSubsystem->Init(this);

  AddCleanup([this] {
    _ioSubsystem.reset();
  });
}

std::weak_ptr<assets::AssetSubsystem> Engine::GetAssetSubsystem() const {
  return _assetManager;
}

std::weak_ptr<scripting::ScriptSubsystem> Engine::GetScriptSubsystem() const {
  return _scriptManager;
}

std::weak_ptr<widgets::WidgetSubsystem> Engine::GetWidgetSubsystem() const {
  return _widgetManager;
}

std::weak_ptr<audio::AudioSubsystem> Engine::GetAudioSubsystem() const {
  return _audioManager;
}

std::weak_ptr<async::AsyncSubsystem> Engine::GetAsyncSubsystem() const {
  return _asyncSubsystem;
}

std::weak_ptr<io::IoSubsystem> Engine::GetIoSubsystem() const {
  return _ioSubsystem;
}

EInputMode Engine::GetInputMode() const {
  return _inputMode;
}

std::shared_ptr<drawing::DrawingSubsystem> Engine::CreateDrawingSubsystem() {
  return newObject<drawing::DrawingSubsystem>();
}

std::shared_ptr<input::InputSubsystem> Engine::CreateInputSubsystem() {
  return newObject<input::InputSubsystem>();
}

std::shared_ptr<assets::AssetSubsystem> Engine::CreateAssetSubsystem() {
  return newObject<assets::AssetSubsystem>();
}

std::shared_ptr<scripting::ScriptSubsystem> Engine::CreateScriptSubsystem() {
  return newObject<scripting::ScriptSubsystem>();
}

std::shared_ptr<widgets::WidgetSubsystem> Engine::CreateWidgetSubsystem() {
  return newObject<widgets::WidgetSubsystem>();
}

std::shared_ptr<audio::AudioSubsystem> Engine::CreateAudioSubsystem() {
  return newObject<audio::AudioSubsystem>();
}

std::shared_ptr<async::AsyncSubsystem> Engine::CreateAsyncSubsystem() {
  return newObject<async::AsyncSubsystem>();
}

std::shared_ptr<io::IoSubsystem> Engine::CreateIoSubsystem() {
  return newObject<io::IoSubsystem>();
}

void Engine::SetInputMode(EInputMode mode) {

  if (const auto focused = GetFocusedWindow().lock()) {
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

    onInputModeChanged->Execute(_inputMode, mode);

    _inputMode = mode;
  }
}

void Engine::NotifyWindowResize() {
  int width, height;
  const auto winSize = _mainWindow.lock()->GetSize();
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

std::weak_ptr<window::Window> Engine::GetFocusedWindow() const {
  return _focusedWindow;
}


void Engine::InitScene(const std::shared_ptr<scene::Scene> &scene) {
  if (IsRunning()) {
    scene->Init(this);
  }
}

void Engine::InitInputSubsystem() {
  _inputManager = CreateInputSubsystem();
  _inputManager->Init(this);
  AddCleanup([this] {
    _inputManager.reset();
  });
}

void Engine::InitScriptSubsystem() {
  _scriptManager = CreateScriptSubsystem();
  _scriptManager->Init(this);
  AddCleanup([this] {
    _scriptManager.reset();
  });
}

void Engine::InitWidgetSubsystem() {
  _widgetManager = CreateWidgetSubsystem();
  _widgetManager->Init(this);
  AddCleanup([this] {
    _widgetManager.reset();
  });
}

}
