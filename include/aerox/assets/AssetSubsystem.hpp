#pragma once
#include <ft2build.h>
#include FT_FREETYPE_H
#include FT_OUTLINE_H
#define UUID_SYSTEM_GENERATOR
#include "aerox/EngineSubsystem.hpp"
#include "aerox/Object.hpp"
#include "aerox/meta/Macro.hpp"
#include "gen/assets/AssetSubsystem.gen.hpp"

namespace msdfgen {
class Shape;
}

namespace aerox::drawing {
class Font;
class Texture;
class Mesh;
}

namespace aerox::audio {
class AudioBuffer;
}

namespace aerox {
class Engine;
}

namespace aerox::assets {
class AssetSubsystem;
struct AssetMeta;
class LiveAsset;

struct AssetInfo {
  std::shared_ptr<AssetMeta> meta;
  fs::path path;
};

META_TYPE()
class AssetSubsystem : public EngineSubsystem {

private:
  std::unordered_map<std::string, std::shared_ptr<AssetInfo>> _assets;
  std::unordered_map<std::string, std::weak_ptr<LiveAsset>> _liveAssets;

  std::shared_ptr<FT_Library> _library;

public:

  META_BODY()
  
  void OnInit(Engine *engine) override;

  static std::string CreateAssetId();

  static std::shared_ptr<AssetMeta> CreateAssetMeta(const std::string &type,
                                            const std::set<std::string> &tags);

  virtual std::shared_ptr<AssetMeta> LoadAssetMeta(const fs::path &path);

  virtual bool SaveAssetMeta(const std::string &assetId);

  virtual std::weak_ptr<AssetMeta> FindAssetMeta(const std::string &assetId);

  virtual std::weak_ptr<AssetMeta> CreateAsset(const fs::path &destPath,
                                     const std::function<std::shared_ptr<AssetMeta>()> &
                                     method);

  // virtual bool loadAsset(const fs::path &path, VEngineAssetHeader &asset, bool loadData = true);
  // virtual bool saveAsset(const fs::path &path,const VEngineAssetHeader &asset);
  virtual bool SaveAsset(const std::shared_ptr<LiveAsset> &asset);

  virtual std::shared_ptr<LiveAsset> ImportAsset(const fs::path &path,
                                         const std::set<std::string> &tags,
                                         const std::function<std::shared_ptr<LiveAsset>(
                                             const fs::path &)> &importFn);

  virtual std::shared_ptr<LiveAsset> LoadAsset(const std::string &assetId);

  virtual std::shared_ptr<drawing::Mesh> ImportMesh(
      const fs::path &path);

  virtual std::shared_ptr<drawing::Mesh> ImportMeshAsset(
      const fs::path &path);


  virtual std::shared_ptr<drawing::Texture> ImportTexture(
      const fs::path &path);

  virtual std::shared_ptr<drawing::Texture> ImportTextureAsset(
      const fs::path &path);


  virtual std::shared_ptr<drawing::Font> ImportFont(
      const fs::path &path);

  virtual FT_Error ShapeFromFontGlyph(msdfgen::Shape &shape,
                                      FT_Outline *outline);

  virtual std::shared_ptr<drawing::Font> ImportFontAsset(
      const fs::path &path);

  virtual std::shared_ptr<audio::AudioBuffer> ImportAudio(const fs::path &path);

  virtual std::shared_ptr<audio::AudioBuffer> ImportAudioAsset(const fs::path &path);

  String GetName() const override;
};
}
