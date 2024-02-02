#pragma once
#include <vengine/scene/Scene.hpp>
#include <vengine/scene/Transformable.hpp>
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
class SceneObject : public Object<Scene>, public SharableThis<SceneObject>,public Transformable, public drawing::SceneDrawable, public Serializable {

private:
  bool _bCanEverUpdate = false;
  Set<Pointer<Component>> _components;
  Set<WeakPointer<RenderedComponent>> _renderedComponents;
  Pointer<SceneComponent> _rootComponent;
public:
  virtual Pointer<SceneComponent> CreateRootComponent();
  virtual WeakPointer<SceneComponent> GetRootComponent() const;
  virtual void AttachComponentsToRoot(const WeakPointer<SceneComponent> &root);

  // Transformable Interface
  math::Transform GetRelativeTransform() const override;
  void SetRelativeTransform(const math::Transform &val) override;
  WeakPointer<Transformable> GetParent() const override;
  
  virtual void AttachTo(const WeakPointer<SceneComponent> &parent);
  virtual void AttachTo(const WeakPointer<SceneObject> &parent);
  /**
   * \brief Initializes all components, call after component creation is complete
   * \param outer The Scene
   */
  virtual void Init(scene::Scene * outer) override;

  virtual Engine *GetEngine() const;
  virtual WeakPointer<input::SceneInputConsumer> GetInput();
  virtual Scene *GetScene() const;
  virtual void Update(float deltaTime);
  
  template <typename T,typename... Args>
  WeakPointer<T> AddComponent(Args&&... args);

  template <typename T>
  WeakPointer<T> GetComponentByClass();

  void AddToRenderList(const WeakPointer<RenderedComponent> &comp);

  void RemoveFromRenderList(const WeakPointer<RenderedComponent> &comp);
  
  void InitComponent(const Pointer<Component>& comp);

  void HandleDestroy() override;

  void Draw(drawing::SceneDrawer *drawer, drawing::SimpleFrameData *frameData) override;

  virtual void ReadFrom(Buffer &store) override;
  virtual void WriteTo(Buffer &store) override;

  VENGINE_IMPLEMENT_SCENE_OBJECT_ID(SceneObject)
};

template <typename T, typename ... Args> WeakPointer<T> SceneObject::AddComponent(
    Args &&... args) {
  auto comp = newSharedObject<T>(std::forward<Args>(args)...);
  auto compPtr = comp.template Cast<Component>();
  if(IsInitialized()) {
    InitComponent(compPtr);
  }
  _components.Add(compPtr);
  
  return comp;
}

template <typename T> WeakPointer<T> SceneObject::GetComponentByClass() {
  for(auto component : _components) {
    auto dCast = component.Cast<T>();
    if(dCast != nullptr) {
      return dCast;
    }
  }
  return {};
}

}
