#include "vengine/scene/components/LightComponent.hpp"

#include <vengine/scene/Transformable.hpp>
#include <vengine/scene/objects/SceneObject.hpp>
#include <vengine/scene/Scene.hpp>
#include <vengine/scene/components/RenderedComponent.hpp>

namespace vengine::scene {


Pointer<SceneComponent> SceneObject::CreateRootComponent() {
  return newSharedObject<SceneComponent>();
}

WeakPointer<SceneComponent> SceneObject::GetRootComponent() const {
  return _rootComponent;
}

void SceneObject::AttachComponentsToRoot(const WeakPointer<SceneComponent> &root) {
  /* Attach scene components here */
}

math::Transform SceneObject::GetRelativeTransform() const {
  if(auto comp = GetRootComponent()) {
    return comp.Reserve()->GetRelativeTransform();
  }
  return {};
}

void SceneObject::SetRelativeTransform(const math::Transform &val) {
  if(auto comp = GetRootComponent()) {
    Transformable::SetRelativeTransform(val);
    comp.Reserve()->SetRelativeTransform(val);
  }
}

WeakPointer<Transformable> SceneObject::GetParent() const {
  if(auto comp = GetRootComponent()) {
    return comp.Reserve()->GetParent();
  }
  return {};
}

void SceneObject::AttachTo(const WeakPointer<SceneComponent> &parent) {
  if(auto comp = GetRootComponent()) {
    comp.Reserve()->AttachTo(parent);
  }
}

void SceneObject::AttachTo(const WeakPointer<SceneObject> &parent) {
  if(auto comp = GetRootComponent()) {
    comp.Reserve()->AttachTo(parent.Reserve()->GetRootComponent());
  }
}

void SceneObject::Init(scene::Scene * outer) {
  Object<Scene>::Init(outer);
  _rootComponent = CreateRootComponent();
  _rootComponent->Init(this);
  AttachComponentsToRoot(_rootComponent);
  for(const auto &component : _components) {
    InitComponent(component);
  }
  _components.Add(_rootComponent);
}

Engine *SceneObject::GetEngine() const {
  return GetScene()->GetEngine();
}

WeakPointer<input::SceneInputConsumer> SceneObject::GetInput() {
  return GetScene()->GetInput();
}

Scene *SceneObject::GetScene() const {
  return GetOuter();
}

void SceneObject::Update(float deltaTime) {
  
}

void SceneObject::AddToRenderList(const WeakPointer<RenderedComponent> &comp) {
  _renderedComponents.Add(comp);
}

void SceneObject::RemoveFromRenderList(const WeakPointer<RenderedComponent> &comp) {
  _renderedComponents.Remove(comp);
}


void SceneObject::InitComponent(const Pointer<Component> &comp) {
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

void SceneObject::HandleDestroy() {
  Object::HandleDestroy();
  _renderedComponents.clear();
  
  // for(const auto &component : _components) {
  //   if(component == _rootComponent) continue;
  //   component->Destroy();
  // }
  
  _components.clear();

  _rootComponent.Clear();
}

void SceneObject::Draw(drawing::SceneDrawer *drawer,
                       drawing::SimpleFrameData *frameData) {
  for(const auto &comp : _renderedComponents.Clone()) {
    if(comp) {
      comp.Reserve()->Draw(drawer,frameData);
    }
  }
}

void SceneObject::ReadFrom(Buffer &store) {
  
}

void SceneObject::WriteTo(Buffer &store) {
  
}

}
