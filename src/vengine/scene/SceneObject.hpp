#pragma once
#include "vengine/Object.hpp"
#include "vengine/containers/Set.hpp"
#include "vengine/drawing/scene/SceneDrawable.hpp"
#include "vengine/math/Transform.hpp"

#include <vulkan/vulkan.hpp>

namespace vengine {
namespace scene {
class SceneComponent;
class RenderedComponent;
class Component;
}
}

namespace vengine {
namespace drawing {
class Drawer;
}
}

namespace vengine {
namespace scene {
class Component;
}
}

namespace vengine {
namespace scene {
class Scene;
}
}

namespace vengine {
namespace scene {
  
/**
 * \brief Base class for all object that exist in a world
 */
class SceneObject : public Object<Scene> , public drawing::SceneDrawable {

private:
  bool bCanEverUpdate = false;
  Set<Component *> _components;
  Set<RenderedComponent *> _renderedComponents;
  SceneComponent * _rootComponent = nullptr;
public:

  virtual SceneComponent * createRootComponent();

  SceneComponent * getRootComponet();

  virtual void attachComponentsToRoot(SceneComponent * root);
  
  math::Transform getTransform() const;
  void setTransform(const math::Transform& transform) const;

  
  /**
   * \brief Initializes all components, call after component creation is complete
   * \param outer The Scene
   */
  virtual void init(Scene *outer) override;
  
  virtual void update(float deltaTime);
  
  template <typename T>
  T * createComponent();

  template <typename T>
  T * createRenderedComponent();

  void handleCleanup() override;

  void draw(drawing::SceneDrawer *renderer, drawing::SceneFrameData *frameData) override;
};

template <typename T> T * SceneObject::createComponent() {
  auto comp = dynamic_cast<Object<SceneObject>*>(newObject<T>());
  comp->init(this);
  _components.add(reinterpret_cast<Component *&>(comp));
  
  return reinterpret_cast<T *>(comp);
}

template <typename T> T * SceneObject::createRenderedComponent() {
  auto comp = createComponent<T>();
  _renderedComponents.add(reinterpret_cast<RenderedComponent *&>(comp));
  return reinterpret_cast<T *>(comp);
}

// template <typename T, std::enable_if_t<std::is_base_of_v<Component, T>> *> T *
  // SceneObject::addComponent(T *component) {
  //   components.add(component);
  //   if(utils::isType<RenderedComponent>(component)) {
  //     renderedComponents.add(component);
  //   }
  //   component->init();
  // }
  //
  // template <typename T, std::enable_if_t<std::is_base_of_v<Component, T>> *>
  // void SceneObject::removeComponent(T *component) {
  //   components.remove(component);
  //   
  //   if(utils::isType<RenderedComponent>(component)) {
  //     renderedComponents.remove(component);
  //   }
  //   
  //   component->destroy();
  //   delete component;
  // }
}
}
