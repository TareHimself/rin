#pragma once
#include "types.hpp"
#include "vengine/Object.hpp"
#include "vengine/assets/types.hpp"
#include "vengine/containers/Array.hpp"

namespace vengine {
namespace drawing {
class Material;
}
}

namespace vengine {
namespace assets {
class AssetManager;
}
}

namespace vengine {
class Engine;
}

namespace vengine {
namespace drawing {

class Mesh : public Object<Engine> {

protected:
  assets::MeshAsset _asset;
  Array<Material *> _materials;
public:
  Array<Vertex> getVertices() const;
  Array<uint32_t> getIndices() const;
  Array<assets::MeshSurface> getSurfaces() const;
  Array<Material*> getMaterials() const;
  
  
  GpuMeshBuffers buffers;

  void setMaterial(uint32_t index,Material * material);
  void upload();
  
  String getName() const;

  void handleCleanup() override;

  void setAsset(const assets::MeshAsset &asset);
};
}
}
