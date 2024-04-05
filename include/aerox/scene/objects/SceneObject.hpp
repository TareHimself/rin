#pragma once
#include <aerox/scene/Scene.hpp>
#include "aerox/Engine.hpp"
#include "aerox/Object.hpp"
#include "aerox/TObjectWithInit.hpp"
#include "aerox/containers/Serializable.hpp"
#include "aerox/drawing/scene/SceneDrawable.hpp"
#include "aerox/input/SceneInputConsumer.hpp"
#include "aerox/math/Transform.hpp"
#include "gen/scene/objects/SceneObject.gen.hpp"

namespace aerox::scene {
class SceneComponent;
class RenderedComponent;
class Component;
}

namespace aerox::drawing {
class DrawingSubsystem;
}

namespace aerox::scene {
class Component;
}

namespace aerox::scene {
class Scene;
}

namespace aerox::scene {

META_TYPE(Super=Object)
class SceneObject : public TOwnedBy<Scene>, public drawing::SceneDrawable, public Serializable {

private:
  bool _canEverUpdate = false;
  bool _isInitialized = false;
  std::weak_ptr<SceneObject> _owner;
  std::list<std::shared_ptr<Component>> _components;
  std::list<std::weak_ptr<RenderedComponent>> _renderedComponents;
  std::shared_ptr<SceneComponent> _rootComponent;
public:

  META_BODY()
  
  virtual std::shared_ptr<SceneComponent> CreateRootComponent();
  virtual std::weak_ptr<SceneComponent> GetRootComponent() const;
  virtual void AttachComponentsToRoot(const std::weak_ptr<SceneComponent> &root);
  
  virtual void AttachTo(const std::weak_ptr<SceneComponent> &parent);
  virtual void AttachTo(const std::weak_ptr<SceneObject> &parent);
  
  virtual math::Vec3<> GetWorldLocation() const;
  virtual math::Quat GetWorldRotation() const;
  virtual math::Vec3<> GetWorldScale() const;
  virtual math::Transform GetWorldTransform() const;

  virtual void SetWorldLocation(const math::Vec3<> &val);
  virtual void SetWorldRotation(const math::Quat &val);
  virtual void SetWorldScale(const math::Vec3<> &val);
  virtual void SetWorldTransform(const math::Transform &val);
  
  /**
   * \brief Initializes all components, call after component creation is complete
   * \param scene The Scene
   */
  void OnInit(Scene * scene) override;
  
  virtual Engine *GetEngine() const;
  virtual std::weak_ptr<input::SceneInputConsumer> GetInput();
  virtual Scene *GetScene() const;
  virtual void Tick(float deltaTime);
  
  template <typename T,typename... Args>
  TWeakConstruct<T,Args...> AddComponent(Args&&... args);

  template <typename T>
  std::weak_ptr<T> GetComponentByClass();

  void AddToRenderList(const std::weak_ptr<RenderedComponent> &comp);

  void RemoveFromRenderList(const std::weak_ptr<RenderedComponent> &comp);
  
  void InitComponent(const std::shared_ptr<Component>& comp);

  void OnDestroy() override;

  void Draw(drawing::SceneFrameData *frameData, const math::Transform &parentTransform) override;

  virtual void ReadFrom(Buffer &store) override;
  virtual void WriteTo(Buffer &store) override;
};

template <typename T, typename ... Args> TWeakConstruct<T,Args...> SceneObject::AddComponent(
    Args &&... args) {
  auto comp = newObject<T>(std::forward<Args>(args)...);
  auto compPtr = utils::castStatic<Component>(comp);
  if(IsInitialized() || IsInitializing()) {
    InitComponent(compPtr);
  }
  _components.push_back(compPtr);
  
  return comp;
}

template <typename T> std::weak_ptr<T> SceneObject::GetComponentByClass() {
  for(auto component : _components) {
    if(auto dCast = utils::cast<T>(component)) {
      return dCast;
    }
  }
  return {};
}

}
