#include "SceneObject.hpp"
#include <vengine/scene/Scene.hpp>
#include "components/RenderedComponent.hpp"

namespace vengine::scene {


SceneComponent * SceneObject::CreateRootComponent() {
  return newObject<SceneComponent>();
}

SceneComponent * SceneObject::GetRootComponent() const {
  return _rootComponent;
}

void SceneObject::AttachComponentsToRoot(SceneComponent * root) {
  /* Attach scene components here */
}

math::Transform SceneObject::GetRelativeTransform() const {
  return GetRootComponent()->GetRelativeTransform();
}

void SceneObject::SetRelativeTransform(const math::Transform &val) {
  GetRootComponent()->SetRelativeTransform(val);
}

Transformable * SceneObject::GetParent() const {
  return GetRootComponent()->GetParent();
}

void SceneObject::AttachTo(SceneComponent * parent) {
  GetRootComponent()->AttachTo(parent);
}

void SceneObject::AttachTo(SceneObject * parent) {
  GetRootComponent()->AttachTo(parent->GetRootComponent());
}

void SceneObject::Init(scene::Scene *outer) {
  Object<Scene>::Init(outer);
  _rootComponent = CreateRootComponent();
  _rootComponent->Init(this);
  AttachComponentsToRoot(_rootComponent);
  for(const auto component : _components) {
    component->Init(this);
  }
}

Engine * SceneObject::GetEngine() const {
  return GetScene()->GetEngine();
}

input::SceneInputManager * SceneObject::GetInput() {
  return GetScene()->GetInput();
}

Scene * SceneObject::GetScene() const {
  return GetOuter();
}

void SceneObject::Update(float deltaTime) {
  
}

void SceneObject::AddToRenderList(RenderedComponent *comp) {
  _renderedComponents.Add(comp);
}

void SceneObject::RemoveFromRenderList(RenderedComponent *comp) {
  _renderedComponents.Remove(comp);
}

void SceneObject::HandleDestroy() {
  Object::HandleDestroy();
  _renderedComponents.clear();
  
  for(const auto &component : _components) {
    if(component == _rootComponent) continue;
    component->Destroy();
  }
  
  _components.clear();

  _rootComponent->Destroy();
  _rootComponent = nullptr;
}

void SceneObject::Draw(drawing::SceneDrawer *renderer,
                       drawing::SceneFrameData *frameData) {
  for(const auto comp : _renderedComponents) {
    comp->Draw(renderer,frameData);
  }
}

}
