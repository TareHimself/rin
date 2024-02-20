#pragma once
#include "Widget.hpp"
#include "vengine/drawing/Mesh.hpp"
#include <glm/fwd.hpp>
#include <glm/gtc/matrix_transform.hpp>

namespace vengine::widget {


class GeometryWidget : public Widget {

public:
  void Init(WidgetSubsystem *outer) override;
};
}
