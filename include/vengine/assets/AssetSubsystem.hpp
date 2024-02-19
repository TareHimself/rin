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
class Texture2D;
class Mesh;
}

namespace vengine::assets {
RCLASS()
class AssetSubsystem : public EngineSubsystem {
  
public:
  // virtual bool loadAsset(const fs::path &path, VEngineAssetHeader &asset, bool loadData = true);
  // virtual bool saveAsset(const fs::path &path,const VEngineAssetHeader &asset);
  virtual bool SaveAsset(const fs::path &path, const Managed<Asset> &asset);
  

  virtual Managed<Asset> LoadAsset(const fs::path &path,
                                           const String &type,
                                           const std::function<Managed<Asset>()> &
                                           factory);
  
  virtual Managed<drawing::Mesh> ImportMesh(
      const fs::path &path);
  virtual std::vector<Managed<drawing::Mesh>> ImportMeshes(
      const std::vector<fs::path> &paths);
  virtual Managed<drawing::Mesh> LoadMeshAsset(
      const fs::path &path);

  virtual Managed<drawing::Texture2D> ImportTexture(
      const fs::path &path);
  virtual std::vector<Managed<drawing::Texture2D>> ImportTextures(
      const std::vector<fs::path> &paths);
  virtual Managed<drawing::Texture2D> LoadTextureAsset(
      const fs::path &path);

  virtual Managed<drawing::Font> ImportFont(
      const fs::path &path);
  virtual Managed<drawing::Font> LoadFontAsset(
      const fs::path &path);

  virtual Managed<audio::AudioBuffer> ImportAudio(const fs::path &path);

  String GetName() const override;
};

REFLECT_IMPLEMENT(AssetSubsystem)
}
