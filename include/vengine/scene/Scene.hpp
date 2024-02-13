#pragma once
#include "vengine/IReflected.hpp"
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/containers/Serializable.hpp"
#include "vengine/input/SceneInputConsumer.hpp"
#include "vengine/math/Transform.hpp"
#include "generated/scene/Scene.reflect.hpp"
#include <vulkan/vulkan.hpp>

namespace vengine {
namespace scene {
class LightComponent;
class Light;
}
}

namespace vengine::drawing {
class SceneDrawer;
class DrawingSubsystem;
class Viewport;
}

#ifndef VENGINE_IMPLEMENT_SCENE_ID
#define VENGINE_IMPLEMENT_SCENE_ID(Type) \
inline static String classID = #Type; \
String GetSerializeId() override { \
return std::string("VENGINE_SCENE_") + #Type; \
}
#endif

namespace vengine {
class Engine;
namespace physics {
class ScenePhysics;
}
}
namespace vengine::scene {
class SceneObject;

RCLASS()
class Scene : public Object<Engine>,public Serializable {
  Managed<physics::ScenePhysics> _physics;
  Managed<drawing::SceneDrawer> _drawer;
  Managed<SceneObject> _viewTarget;
  Managed<SceneObject> _defaultViewTarget;
  Array<Managed<SceneObject>> _sceneObjects;
  Array<Managed<SceneObject>> _objectsPendingInit;
  Managed<input::SceneInputConsumer> _inputConsumer;
  std::list<Ref<LightComponent>> _lights;

public:

  RFUNCTION()
  static Managed<Scene> Construct() {
    return newManagedObject<Scene>();
  }

  VENGINE_IMPLEMENT_REFLECTED_INTERFACE(Scene)
  
  Engine *GetEngine() const;

  void Init(Engine * outer) override;
  void BeforeDestroy() override;

  void ReadFrom(Buffer &store) override;
  void WriteTo(Buffer &store) override;

  virtual bool ShouldSerializeObject(const Managed<SceneObject> &object);

  Array<Ref<SceneObject>> GetSceneObjects() const;

  std::list<Ref<LightComponent>> GetSceneLights() const;

  Ref<drawing::SceneDrawer> GetDrawer() const;

  Ref<physics::ScenePhysics> GetPhysics() const;

  Ref<input::SceneInputConsumer> GetInput() const;

  void RegisterLight(const Ref<LightComponent>& light);

  /**
   * \brief Called every tick
   */
  virtual void Tick(float deltaTime);

  /**
   * \brief Create a new physics instance for this world
   * \return The created instance
   */
  virtual Managed<physics::ScenePhysics> CreatePhysics();

  /**
   * \brief Create a new renderer for this world
   * \return The created instance
   */
  virtual Managed<drawing::SceneDrawer> CreateDrawer();

  virtual Managed<input::SceneInputConsumer> CreateInputManager();

  virtual Managed<SceneObject> CreateDefaultViewTarget();

  Ref<SceneObject> GetViewTarget() const;

  virtual Managed<SceneObject> InitSceneObject(const Managed<SceneObject> &object);

  template <typename T,typename ... Args>
  Ref<T> CreateSceneObject(Args &&... args);

  template <typename T,typename ... Args>
  Ref<T> CreateSceneObject(const math::Transform &transform,Args &&... args);
};

template <typename T, typename ... Args> Ref<T> Scene::CreateSceneObject(
    Args &&... args) {
  auto rawObj = newManagedObject<T>(args...);
  const auto obj = rawObj.template Cast<SceneObject>();
  InitSceneObject(obj);
  return rawObj;
}

template <typename T, typename ... Args> Ref<T> Scene::CreateSceneObject(
    const math::Transform &transform, Args &&... args) {
  auto rawObj = CreateSceneObject<T>(args...);
  const auto obj = rawObj.template Cast<SceneObject>();
  obj->SetWorldTransform(transform);
  return rawObj;
}

REFLECT_IMPLEMENT(Scene)
}
