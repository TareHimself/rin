#pragma once
#include "Component.hpp"

#include <vulkan/vulkan.hpp>

namespace vengine {
namespace scene {
class RenderedComponent : public Component {
public:
  
  virtual void render(const vk::CommandBuffer *cmd);
};

inline void RenderedComponent::render(const vk::CommandBuffer *cmd) {
}
}
}
