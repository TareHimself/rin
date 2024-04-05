#pragma once
#include "Component.hpp"
#include "SceneComponent.hpp"
#include "aerox/drawing/scene/SceneDrawable.hpp"
#include "gen/scene/components/RenderedComponent.gen.hpp"

namespace aerox::scene {
META_TYPE()
class RenderedComponent : public SceneComponent, public drawing::SceneDrawable {

public:
  META_BODY()
};
}
