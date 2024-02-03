#pragma once
#include "vengine/Object.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/containers/Serializable.hpp"
#include "vengine/input/SceneInputConsumer.hpp"
#include "vengine/math/Transform.hpp"

#include <vulkan/vulkan.hpp>

namespace vengine {
namespace scene {
class LightComponent;
class Light;
}
}

namespace vengine::drawing {
class SceneDrawer;
class Drawer;
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

namespace scene {
class SceneObject;

/**
 * \brief Base class for worlds
 */
class Scene : public Object<Engine>,public SharableThis<Scene>,public Serializable {
  Ref<physics::ScenePhysics> _physics;
  Ref<drawing::SceneDrawer> _drawer;
  Ref<SceneObject> _viewTarget;
  Ref<SceneObject> _defaultViewTarget;
  Array<Ref<SceneObject>> _sceneObjects;
  Array<Ref<SceneObject>> _objectsPendingInit;
  Ref<input::SceneInputConsumer> _inputConsumer;
  std::list<WeakRef<LightComponent>> _lights;

public:
  Engine *GetEngine() const;

  void Init(Engine * outer) override;
  void HandleDestroy() override;

  void ReadFrom(Buffer &store) override;
  void WriteTo(Buffer &store) override;

  virtual bool ShouldSerializeObject(const Ref<SceneObject> &object);

  Array<WeakRef<SceneObject>> GetSceneObjects() const;

  std::list<WeakRef<LightComponent>> GetSceneLights() const;

  WeakRef<drawing::SceneDrawer> GetDrawer() const;

  WeakRef<physics::ScenePhysics> GetPhysics() const;

  WeakRef<input::SceneInputConsumer> GetInput() const;

  void RegisterLight(const WeakRef<LightComponent>& light);

  /**
   * \brief Called every tick
   */
  virtual void Update(float deltaTime);

  /**
   * \brief Create a new physics instance for this world
   * \return The created instance
   */
  virtual Ref<physics::ScenePhysics> CreatePhysics();

  /**
   * \brief Create a new renderer for this world
   * \return The created instance
   */
  virtual Ref<drawing::SceneDrawer> CreateDrawer();

  virtual Ref<input::SceneInputConsumer> CreateInputManager();

  virtual Ref<SceneObject> CreateDefaultViewTarget();

  WeakRef<SceneObject> GetViewTarget() const;

  virtual Ref<SceneObject> InitSceneObject(const Ref<SceneObject> &object);

  template <typename T,typename ... Args>
  WeakRef<T> CreateSceneObject(Args &&... args);

  template <typename T,typename ... Args>
  WeakRef<T> CreateSceneObject(const math::Transform &transform,Args &&... args);

  VENGINE_IMPLEMENT_SCENE_ID(Scene)
};

template <typename T, typename ... Args> WeakRef<T> Scene::CreateSceneObject(
    Args &&... args) {
  auto rawObj = newSharedObject<T>(args...);
  const auto obj = rawObj.template Cast<SceneObject>();
  InitSceneObject(obj);
  return rawObj;
}

template <typename T, typename ... Args> WeakRef<T> Scene::CreateSceneObject(
    const math::Transform &transform, Args &&... args) {
  auto rawObj = CreateSceneObject<T>(args...);
  const auto obj = rawObj.template Cast<SceneObject>();
  obj->SetWorldTransform(transform);
  return rawObj;
}


}

}
