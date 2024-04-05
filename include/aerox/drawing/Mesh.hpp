#pragma once
#include "GpuNative.hpp"
#include "types.hpp"
#include "aerox/Object.hpp"
#include "aerox/assets/AssetMeta.hpp"
#include "aerox/containers/Array.hpp"
#include "aerox/assets/LiveAsset.hpp"
#include "gen/drawing/Mesh.gen.hpp"

namespace aerox::drawing {
class DrawingSubsystem;
}

namespace aerox::drawing {
class MaterialInstance;
}

namespace aerox::assets {
class AssetSubsystem;
}

namespace aerox {
class Engine;
}

namespace aerox::drawing {
struct MeshSurface {
  uint32_t startIndex;
  uint32_t count;
};

VENGINE_SIMPLE_ARRAY_SERIALIZER(Buffer, MeshSurface);

META_TYPE()
class Mesh : public Object, public assets::LiveAsset, public GpuNative {

protected:
  Array<Vertex> _vertices;
  Array<uint32_t> _indices;
  Array<MeshSurface> _surfaces;
  Array<std::shared_ptr<MaterialInstance>> _materials;
  std::shared_ptr<GpuGeometryBuffers> _gpuData;

public:

  META_BODY()
  
  std::weak_ptr<GpuGeometryBuffers> GetGpuData();
  Array<Vertex> GetVertices() const;
  Array<uint32_t> GetIndices() const;
  Array<MeshSurface> GetSurfaces() const;
  Array<std::weak_ptr<MaterialInstance>> GetMaterials() const;


  void SetVertices(const Array<Vertex> &vertices);
  void SetIndices(const Array<uint32_t> &indices);
  void SetSurfaces(const Array<MeshSurface> &surfaces);
  void SetMaterial(uint32_t index, const std::shared_ptr<MaterialInstance> &material);

  void Upload() override;
  bool IsUploaded() const override;
  String GetName() const;

  void OnDestroy() override;

  void ReadFrom(Buffer &store) override;

  void WriteTo(Buffer &store) override;

  META_FUNCTION()
  static std::shared_ptr<Mesh> Construct() { return newObject<Mesh>(); }
};
}
