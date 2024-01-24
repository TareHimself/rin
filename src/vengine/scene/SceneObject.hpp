#pragma once
#include "Scene.hpp"
#include "components/Transformable.hpp"
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
class SceneObject : public Object<Scene> ,public Transformable, public drawing::SceneDrawable, public Serializable {

private:
  bool _bCanEverUpdate = false;
  Set<Component *> _components;
  Set<RenderedComponent *> _renderedComponents;
  SceneComponent * _rootComponent = nullptr;
public:
  virtual SceneComponent *CreateRootComponent();
  virtual SceneComponent *GetRootComponent() const;
  virtual void AttachComponentsToRoot(SceneComponent *root);

  // Transformable Interface
  math::Transform GetRelativeTransform() const override;
  void SetRelativeTransform(const math::Transform &val) override;
  Transformable *GetParent() const override;
  
  virtual void AttachTo(SceneComponent *parent);
  virtual void AttachTo(SceneObject *parent);
  /**
   * \brief Initializes all components, call after component creation is complete
   * \param outer The Scene
   */
  virtual void Init(scene::Scene *outer) override;

  virtual Engine * GetEngine() const;
  virtual input::SceneInputConsumer * GetInput();
  virtual Scene * GetScene() const;
  virtual void Update(float deltaTime);
  
  template <typename T,typename... Args>
  T * AddComponent(Args&&... args);

  template <typename T>
  T * GetComponentByClass();

  void AddToRenderList(RenderedComponent * comp);

  void RemoveFromRenderList(RenderedComponent * comp);

  void HandleDestroy() override;

  void Draw(drawing::SceneDrawer *renderer, drawing::SimpleFrameData *frameData) override;

  virtual void ReadFrom(Buffer &store) override;
  virtual void WriteTo(Buffer &store) override;

  VENGINE_IMPLEMENT_SCENE_OBJECT_ID(SceneObject)
};

template <typename T, typename ... Args> T * SceneObject::AddComponent(
    Args &&... args) {
  auto comp = dynamic_cast<Object<SceneObject>*>(newObject<T>(std::forward<Args>(args)...));
  if(HasBeenInitialized()) {
    comp->Init(this);
  }
  _components.Add(reinterpret_cast<Component *&>(comp));
  
  return reinterpret_cast<T *>(comp);
}

template <typename T> T * SceneObject::GetComponentByClass() {
  for(auto component : _components) {
    auto dCast = dynamic_cast<T *>(component);
    if(dCast != nullptr) {
      return dCast;
    }
  }
  return nullptr;
}

}
