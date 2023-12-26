#pragma once
#include "vengine/Object.hpp"
#include "vengine/utils.hpp"
#include "vengine/containers/Array.hpp"
#include "vengine/containers/Set.hpp"

#include <vulkan/vulkan.hpp>

namespace vengine {
namespace scene {
class RenderedComponent;
}
}

namespace vengine {
namespace rendering {
class Renderer;
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
class SceneObject : public Object<Scene> {

  bool bCanEverUpdate = false;
  Set<Component *> components;
  Set<RenderedComponent *> renderedComponents;
public:

  // template<typename T, std::enable_if_t<std::is_base_of_v<Component, T>>* = nullptr>
  // T * addComponent(T * component);
  //
  // template<typename T, std::enable_if_t<std::is_base_of_v<Component, T>>* = nullptr>
  // void removeComponent(T * component);
  
  // void setWorld(Scene * newWorld);
  //
  virtual void update(float deltaTime);

  virtual void render(rendering::Renderer * renderer,const vk::CommandBuffer *cmd);
};

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
