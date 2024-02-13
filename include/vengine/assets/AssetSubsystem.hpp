#pragma once
#include "Asset.hpp"
#include "vengine/EngineSubsystem.hpp"
#include "vengine/Object.hpp"
#include "generated/assets/AssetSubsystem.reflect.hpp"

namespace vengine {
namespace drawing {
class Font;
}
}

namespace vengine {
namespace audio {
class AudioBuffer;
}
}

namespace vengine {
class Engine;
}

namespace vengine::drawing {
class Texture;
class Mesh;
}

namespace vengine::assets {
RCLASS()
class AssetSubsystem : public EngineSubsystem {
public:

  // virtual bool loadAsset(const std::filesystem::path &path, VEngineAssetHeader &asset, bool loadData = true);
  // virtual bool saveAsset(const std::filesystem::path &path,const VEngineAssetHeader &asset);
  virtual bool SaveAsset(const std::filesystem::path &path, const Managed<Asset> &asset);
  

  virtual Managed<Asset> LoadAsset(const std::filesystem::path &path,
                                           const String &type,
                                           const std::function<Managed<Asset>()> &
                                           factory);

  
  virtual Managed<drawing::Mesh> ImportMesh(
      const std::filesystem::path &path);
  virtual std::vector<Managed<drawing::Mesh>> ImportMeshes(
      const std::vector<std::filesystem::path> &paths);
  virtual Managed<drawing::Mesh> LoadMeshAsset(
      const std::filesystem::path &path);

  virtual Managed<drawing::Texture> ImportTexture(
      const std::filesystem::path &path);
  virtual std::vector<Managed<drawing::Texture>> ImportTextures(
      const std::vector<std::filesystem::path> &paths);
  virtual Managed<drawing::Texture> LoadTextureAsset(
      const std::filesystem::path &path);

  virtual Managed<drawing::Font> ImportFont(
      const std::filesystem::path &path);
  virtual Managed<drawing::Font> LoadFontAsset(
      const std::filesystem::path &path);

  virtual Managed<audio::AudioBuffer> ImportAudio(const std::filesystem::path &path);

  String GetName() const override;
};

REFLECT_IMPLEMENT(AssetSubsystem)
}
