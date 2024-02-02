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
  Pointer<physics::ScenePhysics> _physics;
  Pointer<drawing::SceneDrawer> _drawer;
  Pointer<SceneObject> _viewTarget;
  Pointer<SceneObject> _defaultViewTarget;
  Array<Pointer<SceneObject>> _sceneObjects;
  Array<Pointer<SceneObject>> _objectsPendingInit;
  Pointer<input::SceneInputConsumer> _inputConsumer;
  Set<WeakPointer<LightComponent>> _lights;

public:
  Engine *GetEngine() const;

  void Init(Engine * outer) override;
  void HandleDestroy() override;

  void ReadFrom(Buffer &store) override;
  void WriteTo(Buffer &store) override;

  virtual bool ShouldSerializeObject(const Pointer<SceneObject> &object);

  Array<WeakPointer<SceneObject>> GetSceneObjects() const;

  WeakPointer<drawing::SceneDrawer> GetDrawer() const;

  WeakPointer<physics::ScenePhysics> GetPhysics() const;

  WeakPointer<input::SceneInputConsumer> GetInput() const;

  void RegisterLight(const WeakPointer<LightComponent>& light);

  /**
   * \brief Called every tick
   */
  virtual void Update(float deltaTime);

  /**
   * \brief Create a new physics instance for this world
   * \return The created instance
   */
  virtual Pointer<physics::ScenePhysics> CreatePhysics();

  /**
   * \brief Create a new renderer for this world
   * \return The created instance
   */
  virtual Pointer<drawing::SceneDrawer> CreateDrawer();

  virtual Pointer<input::SceneInputConsumer> CreateInputManager();

  virtual Pointer<SceneObject> CreateDefaultViewTarget();

  WeakPointer<SceneObject> GetViewTarget() const;

  virtual Pointer<SceneObject> InitSceneObject(const Pointer<SceneObject> &object);

  template <typename T,typename ... Args>
  WeakPointer<T> CreateSceneObject(Args &&... args);

  template <typename T,typename ... Args>
  WeakPointer<T> CreateSceneObject(const math::Transform &transform,Args &&... args);

  VENGINE_IMPLEMENT_SCENE_ID(Scene)
};

template <typename T, typename ... Args> WeakPointer<T> Scene::CreateSceneObject(
    Args &&... args) {
  auto rawObj = newSharedObject<T>(args...);
  const auto obj = rawObj.template Cast<SceneObject>();
  InitSceneObject(obj);
  return rawObj;
}

template <typename T, typename ... Args> WeakPointer<T> Scene::CreateSceneObject(
    const math::Transform &transform, Args &&... args) {
  auto rawObj = CreateSceneObject<T>(args...);
  const auto obj = rawObj.template Cast<SceneObject>();
  obj->SetWorldTransform(transform);
  return rawObj;
}


}

}
