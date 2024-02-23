#include <glm/ext/matrix_clip_space.hpp>
#include <vengine/widget/WidgetSubsystem.hpp>
#include <vengine/widget/Widget.hpp>
#include <vengine/Engine.hpp>
#include <vengine/assets/AssetSubsystem.hpp>
#include <vengine/drawing/DrawingSubsystem.hpp>
#include <vengine/drawing/MaterialBuilder.hpp>
#include <vengine/io/io.hpp>

namespace vengine::widget {
void WidgetSubsystem::Init(Engine *outer) {
  EngineSubsystem::Init(outer);
  const auto existingWindows = window::getManager()->GetWindows();
  for(auto &window : existingWindows) {
    CreateRoot(window);
  }
  
  AddCleanup(window::getManager()->onWindowCreated,window::getManager()->onWindowCreated.Bind([this](const Ref<window::Window>& window) {
    CreateRoot(window);
  }));

  AddCleanup(window::getManager()->onWindowDestroyed,window::getManager()->onWindowDestroyed.Bind([this](const Ref<window::Window>& window) {
    DestroyRoot(window);
  }));

  
}

void WidgetSubsystem::BeforeDestroy() {
  EngineSubsystem::BeforeDestroy();

  GetOuter()->GetDrawingSubsystem().Reserve()->WaitDeviceIdle();

  _roots.clear();
}

String WidgetSubsystem::GetName() const {
  return "widgets";
}

void WidgetSubsystem::Draw(
    drawing::RawFrameData *frameData) {
  for(auto &val : _rootsArr) {
    if(auto reserved = val.Reserve(); reserved) {
      reserved->Draw(frameData);
    }
  }
}

void WidgetSubsystem::InitWidget(const Managed<Widget> &widget) {
  widget->Init(this);
}

Ref<WidgetRoot> WidgetSubsystem::GetRoot(const Ref<window::Window> &window) {
  const auto reserved = window.Reserve().Get();
  if(_roots.contains(reserved->GetId())) {  
    return _roots[reserved->GetId()];
  }

  return {};
}

void WidgetSubsystem::CreateRoot(const Ref<window::Window> &window) {
  auto root = newManagedObject<WidgetRoot>();
  root->Init(window,this);
  _roots.emplace(window.Reserve()->GetId(),root);
  _rootsArr.push(root);
}

void WidgetSubsystem::DestroyRoot(const Ref<window::Window> &window) {
  if(const auto kv = _roots.find(window.Reserve()->GetId()); kv != _roots.end()) {
    if(const auto idx = _rootsArr.index_of(kv->second); idx.has_value()) {
      _rootsArr.remove(idx.value());
    }
    
    _roots.erase(kv);
    
  }
}

Managed<drawing::MaterialInstance> WidgetSubsystem::CreateMaterialInstance(
    const Array<Managed<drawing::Shader>> &shaders) {
  return drawing::MaterialBuilder().AddShaders(shaders).SetType(drawing::EMaterialType::UI).AddAttachmentFormats({vk::Format::eR16G16B16A16Sfloat}).Create();
}
}
