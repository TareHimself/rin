#pragma once
#include "Asset.hpp"
#include "vengine/EngineSubsystem.hpp"
#include "vengine/Object.hpp"
#include "vengine/widget/Font.hpp"

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
class AssetManager : public EngineSubsystem {
public:

  // virtual bool loadAsset(const std::filesystem::path &path, VEngineAssetHeader &asset, bool loadData = true);
  // virtual bool saveAsset(const std::filesystem::path &path,const VEngineAssetHeader &asset);
  virtual bool SaveAsset(const std::filesystem::path &path, const Pointer<Asset> &asset);

  virtual Pointer<Asset> LoadAsset(const std::filesystem::path &path,
                                           const String &type,
                                           const std::function<Pointer<Asset>()> &
                                           factory);
  virtual Pointer<drawing::Mesh> ImportMesh(
      const std::filesystem::path &path);
  virtual Pointer<drawing::Mesh> LoadMeshAsset(
      const std::filesystem::path &path);

  virtual Pointer<drawing::Texture> ImportTexture(
      const std::filesystem::path &path);
  virtual Pointer<drawing::Texture> LoadTextureAsset(
      const std::filesystem::path &path);

  virtual Pointer<widget::Font> ImportFont(
      const std::filesystem::path &path);
  virtual Pointer<widget::Font> LoadFontAsset(
      const std::filesystem::path &path);

  String GetName() const override;
};
}
