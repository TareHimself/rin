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
class DrawingSubsystem;
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


class SceneObject : public Object<Scene>, public drawing::SceneDrawable, public Serializable {

private:
  bool _bCanEverUpdate = false;
  std::list<Managed<Component>> _components;
  std::list<Ref<RenderedComponent>> _renderedComponents;
  Managed<SceneComponent> _rootComponent;
public:
  virtual Managed<SceneComponent> CreateRootComponent();
  virtual Ref<SceneComponent> GetRootComponent() const;
  virtual void AttachComponentsToRoot(const Ref<SceneComponent> &root);
  
  virtual void AttachTo(const Ref<SceneComponent> &parent);
  virtual void AttachTo(const Ref<SceneObject> &parent);
  
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
  virtual Ref<input::SceneInputConsumer> GetInput();
  virtual Scene *GetScene() const;
  virtual void Tick(float deltaTime);
  
  template <typename T,typename... Args>
  Ref<T> AddComponent(Args&&... args);

  template <typename T>
  Ref<T> GetComponentByClass();

  void AddToRenderList(const Ref<RenderedComponent> &comp);

  void RemoveFromRenderList(const Ref<RenderedComponent> &comp);
  
  void InitComponent(const Managed<Component>& comp);

  void BeforeDestroy() override;

  void Draw(drawing::SimpleFrameData *frameData, const math::Transform &parentTransform) override;

  virtual void ReadFrom(Buffer &store) override;
  virtual void WriteTo(Buffer &store) override;

  std::shared_ptr<reflect::wrap::Reflected> GetReflected() const override;
};

template <typename T, typename ... Args> Ref<T> SceneObject::AddComponent(
    Args &&... args) {
  auto comp = newManagedObject<T>(std::forward<Args>(args)...);
  auto compPtr = comp.template Cast<Component>();
  if(IsInitialized()) {
    InitComponent(compPtr);
  }
  _components.push_back(compPtr);
  
  return comp;
}

template <typename T> Ref<T> SceneObject::GetComponentByClass() {
  for(auto component : _components) {
    auto dCast = component.Cast<T>();
    if(dCast != nullptr) {
      return dCast;
    }
  }
  return {};
}

}
