#include <glm/ext/matrix_clip_space.hpp>
#include <aerox/widgets/WidgetSubsystem.hpp>
#include <aerox/widgets/Widget.hpp>
#include <aerox/Engine.hpp>
#include <aerox/assets/AssetSubsystem.hpp>
#include <aerox/drawing/DrawingSubsystem.hpp>
#include <aerox/drawing/MaterialBuilder.hpp>
#include <aerox/io/io.hpp>

namespace aerox::widgets {
void WidgetSubsystem::OnInit(Engine *outer) {
  EngineSubsystem::OnInit(outer);
  const auto existingWindows = window::getManager()->GetWindows();
  for (auto &window : existingWindows) {
    CreateRoot(window);
  }

  const auto sharedThis = utils::castStatic<WidgetSubsystem>(this->shared_from_this());
  
  AddCleanup(window::getManager()->onWindowCreated->BindManagedFunction<WidgetSubsystem>(
                 sharedThis,
                 &WidgetSubsystem::CreateRoot));

  AddCleanup(window::getManager()->onWindowDestroyed->BindManagedFunction<WidgetSubsystem>(
                 sharedThis,
                 &WidgetSubsystem::DestroyRoot));

}

void WidgetSubsystem::OnDestroy() {
  Engine::Get()->GetDrawingSubsystem().lock()->WaitDeviceIdle();
  _roots.clear();

  EngineSubsystem::OnDestroy();
}

String WidgetSubsystem::GetName() const {
  return "widgets";
}

void WidgetSubsystem::Draw(
    drawing::RawFrameData *frameData) {
  for (auto &val : _rootsArr) {
    if (const auto reserved = _roots[val]) {
      reserved->Draw(frameData);
    }
  }
}

void WidgetSubsystem::InitWidget(const std::shared_ptr<Widget> &widget) const {
  widget->Init(const_cast<WidgetSubsystem *>(this));
}

std::weak_ptr<WidgetRoot> WidgetSubsystem::GetRoot(const std::weak_ptr<window::Window> &window) {
  const auto reserved = window.lock().get();
  if (_roots.contains(reserved->GetId())) {
    return _roots[reserved->GetId()];
  }

  return {};
}

void WidgetSubsystem::CreateRoot(const std::weak_ptr<window::Window> &window) {
  auto root = newObject<WidgetRoot>();
  root->Init(this,window);
  _roots.emplace(window.lock()->GetId(), root);
  _rootsArr.push(window.lock()->GetId());
}

void WidgetSubsystem::DestroyRoot(const std::weak_ptr<window::Window> &window) {
  if(const auto lWindow = window.lock()) {
    if (const auto kv = _roots.find(lWindow->GetId());
    kv != _roots.end()) {
      if (const auto idx = _rootsArr.index_of(lWindow->GetId()); idx.has_value()) {
        _rootsArr.remove(idx.value());
      }

      _roots.erase(kv);

    }
  }
}

std::shared_ptr<drawing::MaterialInstance> WidgetSubsystem::CreateMaterialInstance(
    const Array<std::shared_ptr<drawing::Shader>> &shaders) {
  return drawing::MaterialBuilder().AddShaders(shaders).
                                    SetType(drawing::EMaterialType::UI).
                                    AddAttachmentFormats(
                                        {vk::Format::eR16G16B16A16Sfloat}).
                                    Create();
}

void WidgetSubsystem::Tick(float deltaTime) {
  for (auto &val : _rootsArr) {
    if (const auto reserved = _roots[val]) {
      reserved->Tick(deltaTime);
    }
  }
}
}
