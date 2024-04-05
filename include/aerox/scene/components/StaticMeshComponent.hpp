#pragma once
#include "RenderedComponent.hpp"
#include "aerox/drawing/Mesh.hpp"
#include "gen/scene/components/StaticMeshComponent.gen.hpp"
namespace aerox::scene {
META_TYPE()
class StaticMeshComponent : public RenderedComponent {
  std::shared_ptr<drawing::Mesh> _mesh;

public:

  META_BODY()
  
  std::weak_ptr<drawing::Mesh> GetMesh() const;
  void SetMesh(const std::shared_ptr<drawing::Mesh> &newMesh);

  void Draw(
      drawing::SceneFrameData *frameData,
      const math::Transform &parentTransform) override;

  META_FUNCTION()

  static std::shared_ptr<StaticMeshComponent> Construct() {
    return newObject<StaticMeshComponent>();
  }
};
}
