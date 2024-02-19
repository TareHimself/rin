#include "vengine/scene/components/LightComponent.hpp"

#include <vengine/scene/objects/SceneObject.hpp>
#include <vengine/scene/Scene.hpp>
#include <vengine/scene/components/RenderedComponent.hpp>

namespace vengine::scene {


Managed<SceneComponent> SceneObject::CreateRootComponent() {
  return newManagedObject<SceneComponent>();
}

Ref<SceneComponent> SceneObject::GetRootComponent() const {
  return _rootComponent;
}

void SceneObject::AttachComponentsToRoot(const Ref<SceneComponent> &root) {
  /* Attach scene components here */
}

void SceneObject::AttachTo(const Ref<SceneComponent> &parent) {
  if(auto comp = GetRootComponent()) {
    comp.Reserve()->AttachTo(parent);
  }
}

void SceneObject::AttachTo(const Ref<SceneObject> &parent) {
  if(auto comp = GetRootComponent()) {
    comp.Reserve()->AttachTo(parent.Reserve()->GetRootComponent());
  }
}

math::Vector SceneObject::GetWorldLocation() const {
  if(auto comp = GetRootComponent()) {
    return comp.Reserve()->GetWorldLocation();
  }

  return {};
}

math::Quat SceneObject::GetWorldRotation() const {
  if(auto comp = GetRootComponent()) {
    return comp.Reserve()->GetWorldRotation();
  }

  return {};
}

math::Vector SceneObject::GetWorldScale() const {
  if(auto comp = GetRootComponent()) {
    return comp.Reserve()->GetWorldScale();
  }

  return {};
}

math::Transform SceneObject::GetWorldTransform() const {
  if(auto comp = GetRootComponent()) {
    return comp.Reserve()->GetWorldTransform();
  }

  return {};
}

void SceneObject::SetWorldLocation(const math::Vector &val) {
  if(auto comp = GetRootComponent()) {
    return comp.Reserve()->SetWorldLocation(val);
  }
}

void SceneObject::SetWorldRotation(const math::Quat &val) {
  if(auto comp = GetRootComponent()) {
    return comp.Reserve()->SetWorldRotation(val);
  }
}

void SceneObject::SetWorldScale(const math::Vector &val) {
  if(auto comp = GetRootComponent()) {
    return comp.Reserve()->SetWorldScale(val);
  }
}

void SceneObject::SetWorldTransform(const math::Transform &val) {
  if(auto comp = GetRootComponent()) {
    return comp.Reserve()->SetWorldTransform(val);
  }
}

void SceneObject::Init(scene::Scene * outer) {
  Object<Scene>::Init(outer);
  _rootComponent = CreateRootComponent();
  InitComponent(_rootComponent);
  AttachComponentsToRoot(_rootComponent);
  for(const auto &component : _components) {
    InitComponent(component);
  }
  _components.emplace_back(_rootComponent);
}

Engine *SceneObject::GetEngine() const {
  return GetScene()->GetEngine();
}

Ref<input::SceneInputConsumer> SceneObject::GetInput() {
  return GetScene()->GetInput();
}

Scene *SceneObject::GetScene() const {
  return GetOuter();
}

void SceneObject::Tick(float deltaTime) {
  
}

void SceneObject::AddToRenderList(const Ref<RenderedComponent> &comp) {
  _renderedComponents.push_back(comp);
}

void SceneObject::RemoveFromRenderList(const Ref<RenderedComponent> &comp) {
  _renderedComponents.remove(comp);
}


void SceneObject::InitComponent(const Managed<Component> &comp) {
  if(comp) {
    comp->Init(this);
    
    if(auto asRendered = comp.Cast<RenderedComponent>()) {
      AddToRenderList(asRendered);
    }
    if(auto asLight = comp.Cast<LightComponent>()) {
      GetScene()->RegisterLight(asLight);
    }
  }
}

void SceneObject::BeforeDestroy() {
  Object::BeforeDestroy();
  _renderedComponents.clear();
  
  // for(const auto &component : _components) {
  //   if(component == _rootComponent) continue;
  //   component->Destroy();
  // }
  
  _components.clear();

  _rootComponent.Clear();
}

void SceneObject::Draw(
    drawing::SceneFrameData *frameData, const math::Transform &parentTransform) {
  for(const auto &comp : _renderedComponents) {
    if(comp) {
      comp.Reserve()->Draw(frameData,comp == _rootComponent ? math::Transform{} : _rootComponent->GetRelativeTransform());
    }
  }
}

void SceneObject::ReadFrom(Buffer &store) {
  
}

void SceneObject::WriteTo(Buffer &store) {
  
}

std::shared_ptr<reflect::wrap::Reflected> SceneObject::GetReflected() const {
  return {};
}

}
