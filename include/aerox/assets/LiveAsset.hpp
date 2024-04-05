#pragma once
#include "aerox/typedefs.hpp"
#include "aerox/containers/Serializable.hpp"
#include <string>
#include "gen/assets/LiveAsset.gen.hpp"


namespace aerox::assets {
class AssetSubsystem;
}



namespace aerox::assets {
struct AssetMeta;
}


namespace aerox::assets {
META_TYPE()
class LiveAsset : public Serializable, public meta::IMetadata {
  std::string _assetId;

protected:
  friend class AssetSubsystem;
  static std::unordered_map<std::string,std::weak_ptr<LiveAsset>> _liveAssetCache;
  virtual void SetAssetId(const std::string& id);
public:

  META_BODY()

  virtual std::string GetAssetId() const;

  static bool IsCached(const std::string& assetId);
  
  static std::shared_ptr<LiveAsset> Resolve(const std::string& assetId);
  static std::shared_ptr<LiveAsset> Resolve(const std::shared_ptr<AssetMeta>& asset);
  static std::shared_ptr<LiveAsset> Resolve(const fs::path& assetPath);
};
}
