#pragma once
#include "vengine/containers/Array.hpp"
#include "vengine/containers/String.hpp"
#include "vengine/drawing/types.hpp"

#include <cstdint>
#include <string>
namespace vengine {
namespace assets {
namespace types {
const String MESH = "MESH";
const String TEXTURE = "TEXTURE";
}
struct VEngineAsset {
  uint32_t version;
  String type;
  String name;
  String meta;
  virtual void save(const std::ofstream &stream) const = 0;
  virtual void load(const std::ifstream &stream) = 0;
};

struct MeshSurface {
  uint32_t startIndex;
  uint32_t count;
};

struct MeshAsset : VEngineAsset {
  
  MeshAsset() {
    type = types::MESH;
  }

  Array<MeshSurface> surfaces;
  Array<drawing::Vertex> vertices;
  Array<uint32_t> indices;

  void save(const std::ofstream &stream) const override;

  void load(const std::ifstream &stream) override;
};
}

}
