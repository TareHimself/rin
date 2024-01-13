#pragma once
#include "RenderedComponent.hpp"
#include "vengine/drawing/Mesh.hpp"

namespace vengine {
namespace scene {
class StaticMeshComponent : public RenderedComponent {
  drawing::Mesh * mesh = nullptr;
public:
  drawing::Mesh * getMesh() const;
  void setMesh(drawing::Mesh * newMesh);

  void draw(drawing::SceneDrawer *renderer, drawing::SceneFrameData *frameData) override;
};
}
}
