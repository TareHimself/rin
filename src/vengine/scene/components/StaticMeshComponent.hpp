#pragma once
#include "RenderedComponent.hpp"
#include "vengine/drawing/Mesh.hpp"

namespace vengine::scene {
class StaticMeshComponent : public RenderedComponent {
  drawing::Mesh * _mesh = nullptr;
public:
  drawing::Mesh * GetMesh() const;
  void SetMesh(drawing::Mesh * newMesh);

  void Draw(drawing::SceneDrawer *renderer, drawing::SimpleFrameData *frameData) override;

  VENGINE_IMPLEMENT_COMPONENT_ID(StaticMeshComponent)
};


}
