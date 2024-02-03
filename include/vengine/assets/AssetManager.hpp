#pragma once
#include "Asset.hpp"
#include "vengine/EngineSubsystem.hpp"
#include "vengine/Object.hpp"
#include "vengine/widget/Font.hpp"

namespace vengine {
class Engine;
}

namespace vengine::drawing {
class Texture;
class Mesh;
}

namespace vengine::assets {
class AssetManager : public EngineSubsystem {
public:

  // virtual bool loadAsset(const std::filesystem::path &path, VEngineAssetHeader &asset, bool loadData = true);
  // virtual bool saveAsset(const std::filesystem::path &path,const VEngineAssetHeader &asset);
  virtual bool SaveAsset(const std::filesystem::path &path, const Ref<Asset> &asset);

  virtual Ref<Asset> LoadAsset(const std::filesystem::path &path,
                                           const String &type,
                                           const std::function<Ref<Asset>()> &
                                           factory);
  virtual Ref<drawing::Mesh> ImportMesh(
      const std::filesystem::path &path);
  virtual std::vector<Ref<drawing::Mesh>> ImportMeshes(
      const std::vector<std::filesystem::path> &paths);
  virtual Ref<drawing::Mesh> LoadMeshAsset(
      const std::filesystem::path &path);

  virtual Ref<drawing::Texture> ImportTexture(
      const std::filesystem::path &path);
  virtual std::vector<Ref<drawing::Texture>> ImportTextures(
      const std::vector<std::filesystem::path> &paths);
  virtual Ref<drawing::Texture> LoadTextureAsset(
      const std::filesystem::path &path);

  virtual Ref<widget::Font> ImportFont(
      const std::filesystem::path &path);
  virtual Ref<widget::Font> LoadFontAsset(
      const std::filesystem::path &path);

  String GetName() const override;
};
}
