#pragma once
#include "Asset.hpp"
#include "types.hpp"
#include "vengine/Object.hpp"

#include <filesystem>


namespace vengine {
namespace drawing {
class Texture;
}
}

namespace vengine {
class Engine;
}

namespace vengine::drawing {
class Mesh;
}

namespace vengine::assets {
class AssetManager : public Object<Engine>{
public:

  // virtual bool loadAsset(const std::filesystem::path &path, VEngineAssetHeader &asset, bool loadData = true);
  // virtual bool saveAsset(const std::filesystem::path &path,const VEngineAssetHeader &asset);
  virtual bool SaveAsset(const std::filesystem::path &path,Asset * asset);
  virtual drawing::Mesh * ImportMesh(const std::filesystem::path &path);
  virtual drawing::Mesh * LoadMeshAsset(const std::filesystem::path &path);

  virtual drawing::Texture * ImportTexture(const std::filesystem::path &path);
  virtual drawing::Texture * LoadTextureAsset(const std::filesystem::path &path);
};
}
