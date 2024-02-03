#pragma once
#include <vengine/scene/Scene.hpp>
#include "vengine/Engine.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Serializable.hpp"
#include "vengine/containers/Set.hpp"
#include "vengine/drawing/scene/SceneDrawable.hpp"
#include "vengine/input/SceneInputConsumer.hpp"
#include "vengine/math/Transform.hpp"

#include <vulkan/vulkan.hpp>

namespace vengine::scene {
class SceneComponent;
class RenderedComponent;
class Component;
}

namespace vengine::drawing {
class Drawer;
}

namespace vengine::scene {
class Component;
}

namespace vengine::scene {
class Scene;
}

namespace vengine::scene {
#ifndef VENGINE_IMPLEMENT_SCENE_OBJECT_ID
#define VENGINE_IMPLEMENT_SCENE_OBJECT_ID(Type) \
inline static String classID = #Type; \
String GetSerializeId() override { \
return std::string("VENGINE_SCENE_OBJECT_") + #Type; \
}
#endif
/**
 * \brief Base class for all object that exist in a world
 */
class SceneObject : public Object<Scene>, public SharableThis<SceneObject>, public drawing::SceneDrawable, public Serializable {

private:
  bool _bCanEverUpdate = false;
  std::list<Ref<Component>> _components;
  std::list<WeakRef<RenderedComponent>> _renderedComponents;
  Ref<SceneComponent> _rootComponent;
public:
  virtual Ref<SceneComponent> CreateRootComponent();
  virtual WeakRef<SceneComponent> GetRootComponent() const;
  virtual void AttachComponentsToRoot(const WeakRef<SceneComponent> &root);
  
  virtual void AttachTo(const WeakRef<SceneComponent> &parent);
  virtual void AttachTo(const WeakRef<SceneObject> &parent);
  
  virtual math::Vector GetWorldLocation() const;
  virtual math::Quat GetWorldRotation() const;
  virtual math::Vector GetWorldScale() const;
  virtual math::Transform GetWorldTransform() const;

  virtual void SetWorldLocation(const math::Vector &val);
  virtual void SetWorldRotation(const math::Quat &val);
  virtual void SetWorldScale(const math::Vector &val);
  virtual void SetWorldTransform(const math::Transform &val);
  
  /**
   * \brief Initializes all components, call after component creation is complete
   * \param outer The Scene
   */
  virtual void Init(scene::Scene * outer) override;

  virtual Engine *GetEngine() const;
  virtual WeakRef<input::SceneInputConsumer> GetInput();
  virtual Scene *GetScene() const;
  virtual void Update(float deltaTime);
  
  template <typename T,typename... Args>
  WeakRef<T> AddComponent(Args&&... args);

  template <typename T>
  WeakRef<T> GetComponentByClass();

  void AddToRenderList(const WeakRef<RenderedComponent> &comp);

  void RemoveFromRenderList(const WeakRef<RenderedComponent> &comp);
  
  void InitComponent(const Ref<Component>& comp);

  void HandleDestroy() override;

  void Draw(drawing::SceneDrawer *drawer, const math::Transform &parentTransform, drawing::SimpleFrameData *frameData) override;

  virtual void ReadFrom(Buffer &store) override;
  virtual void WriteTo(Buffer &store) override;

  VENGINE_IMPLEMENT_SCENE_OBJECT_ID(SceneObject)
};

template <typename T, typename ... Args> WeakRef<T> SceneObject::AddComponent(
    Args &&... args) {
  auto comp = newSharedObject<T>(std::forward<Args>(args)...);
  auto compPtr = comp.template Cast<Component>();
  if(IsInitialized()) {
    InitComponent(compPtr);
  }
  _components.push_back(compPtr);
  
  return comp;
}

template <typename T> WeakRef<T> SceneObject::GetComponentByClass() {
  for(auto component : _components) {
    auto dCast = component.Cast<T>();
    if(dCast != nullptr) {
      return dCast;
    }
  }
  return {};
}

}
