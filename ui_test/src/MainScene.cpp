#include "MainScene.hpp"

#include "RootPanel.hpp"
#include "aerox/Engine.hpp"
#include "aerox/widgets/Sizer.hpp"

void MainScene::OnInit(vengine::Engine *owner) {
  Scene::OnInit(owner);
  const auto widgetSubsystem = GetOwner()->GetWidgetSubsystem().lock();

  widgetSubsystem->GetRoot(vengine::Engine::Get()->GetMainWindow()).lock()->Add(widgetSubsystem->CreateWidget<RootPanel>());
}
