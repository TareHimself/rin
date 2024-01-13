#pragma once
#include "types.hpp"
#include "vengine/Object.hpp"

#include <filesystem>


namespace vengine {
class Engine;
}

namespace vengine {
namespace drawing {
class Mesh;
}
}

namespace vengine {
namespace assets {
class AssetManager : public Object<Engine>{
public:

  virtual bool loadAsset(const std::filesystem::path &path, VEngineAsset &asset, bool loadData = true);
  virtual bool saveAsset(const std::filesystem::path &path,const VEngineAsset &asset);
  virtual std::optional<MeshAsset> importMesh(const std::filesystem::path &path);
};
}
}
