#pragma once
#include "Component.hpp"
#include "SceneComponent.hpp"
#include "vengine/drawing/Drawable.hpp"
#include "vengine/drawing/scene/SceneDrawable.hpp"

#include <vulkan/vulkan.hpp>

namespace vengine {
namespace drawing {
class Drawer;
}
}

namespace vengine {
namespace scene {
class RenderedComponent : public SceneComponent, public drawing::SceneDrawable {
  
};
}
}
