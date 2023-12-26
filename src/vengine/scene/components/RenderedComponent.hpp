#pragma once
#include "Component.hpp"

#include <vulkan/vulkan.hpp>

namespace vengine {
namespace rendering {
class Renderer;
}
}

namespace vengine {
namespace scene {
class RenderedComponent : public Component {
public:
  
  virtual void render(rendering::Renderer * renderer,const vk::CommandBuffer *cmd);
};
}
}
