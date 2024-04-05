#include "aerox/scene/components/LightComponent.hpp"

#include <aerox/scene/objects/SceneObject.hpp>
#include <aerox/scene/Scene.hpp>
#include <aerox/scene/components/RenderedComponent.hpp>

namespace aerox::scene {


std::shared_ptr<SceneComponent> SceneObject::CreateRootComponent() {
  return newObject<SceneComponent>();
}

std::weak_ptr<SceneComponent> SceneObject::GetRootComponent() const {
  return _rootComponent;
}

void SceneObject::AttachComponentsToRoot(const std::weak_ptr<SceneComponent> &root) {
  /* Attach scene components here */
}

void SceneObject::AttachTo(const std::weak_ptr<SceneComponent> &parent) {
  if (const auto comp = GetRootComponent().lock()) {
    comp->AttachTo(parent);
  }
}

void SceneObject::AttachTo(const std::weak_ptr<SceneObject> &parent) {
  if(const auto lParent = parent.lock()) {
    if (const auto comp = GetRootComponent().lock()) {
      comp->AttachTo(lParent->GetRootComponent());
    }
  }
}

math::Vec3<> SceneObject::GetWorldLocation() const {
  if (const auto comp = GetRootComponent().lock()) {
    return comp->GetWorldLocation();
  }

  return {};
}

math::Quat SceneObject::GetWorldRotation() const {
  if (const auto comp = GetRootComponent().lock()) {
    return comp->GetWorldRotation();
  }

  return {};
}

math::Vec3<> SceneObject::GetWorldScale() const {
  if (const auto comp = GetRootComponent().lock()) {
    return comp->GetWorldScale();
  }

  return {};
}

math::Transform SceneObject::GetWorldTransform() const {
  if (const auto comp = GetRootComponent().lock()) {
    return comp->GetWorldTransform();
  }

  return {};
}

void SceneObject::SetWorldLocation(const math::Vec3<> &val) {
  if (const auto comp = GetRootComponent().lock()) {
    return comp->SetWorldLocation(val);
  }
}

void SceneObject::SetWorldRotation(const math::Quat &val) {
  if (const auto comp = GetRootComponent().lock()) {
    return comp->SetWorldRotation(val);
  }
}

void SceneObject::SetWorldScale(const math::Vec3<> &val) {
  if (const auto comp = GetRootComponent().lock()) {
    return comp->SetWorldScale(val);
  }
}

void SceneObject::SetWorldTransform(const math::Transform &val) {
  if (const auto comp = GetRootComponent().lock()) {
    return comp->SetWorldTransform(val);
  }
}

void SceneObject::OnInit(Scene *scene) {
  TOwnedBy::OnInit(scene);
  _rootComponent = CreateRootComponent();
  InitComponent(_rootComponent);
  AttachComponentsToRoot(_rootComponent);
  for (const auto &component : _components) {
    InitComponent(component);
  }
  _components.emplace_back(_rootComponent);
}

Engine *SceneObject::GetEngine() const {
  return GetScene()->GetEngine();
}

std::weak_ptr<input::SceneInputConsumer> SceneObject::GetInput() {
  return GetScene()->GetInput();
}

Scene *SceneObject::GetScene() const {
  return GetOwner();
}

void SceneObject::Tick(float deltaTime) {

}

void SceneObject::AddToRenderList(const std::weak_ptr<RenderedComponent> &comp) {
  _renderedComponents.push_back(comp);
}

void SceneObject::RemoveFromRenderList(const std::weak_ptr<RenderedComponent> &comp) {
  if(const auto compToRemove = comp.lock()) {
    _renderedComponents.remove_if(
          [&](const std::weak_ptr<RenderedComponent> &other) {
            if(const auto locked = other.lock()) {
              return locked == compToRemove;
            }
              return false;
          });
  }
  
}


void SceneObject::InitComponent(const std::shared_ptr<Component> &comp) {
  if (comp) {
    comp->Init(this);

    if (const auto asRendered = utils::cast<RenderedComponent>(comp)) {
      AddToRenderList(asRendered);
    }
    if (const auto asLight = utils::cast<LightComponent>(comp)) {
      GetScene()->RegisterLight(asLight);
    }
  }
}

void SceneObject::OnDestroy() {
  Object::OnDestroy();
  _renderedComponents.clear();

  _components.clear();

  _rootComponent.reset();
}

void SceneObject::Draw(
    drawing::SceneFrameData *frameData,
    const math::Transform &parentTransform) {
  for (const auto &comp : _renderedComponents) {
    if (auto r = comp.lock()) {
      r->Draw(frameData,r == _rootComponent
                             ? math::Transform{}
                             : _rootComponent->GetRelativeTransform());
    }
  }
}

void SceneObject::ReadFrom(Buffer &store) {

}

void SceneObject::WriteTo(Buffer &store) {

}


}
