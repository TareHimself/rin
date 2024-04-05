#include "aerox/assets/LiveAsset.hpp"

#include "aerox/Engine.hpp"
#include "aerox/assets/AssetMeta.hpp"
#include "aerox/assets/AssetSubsystem.hpp"

namespace aerox::assets {
std::unordered_map<std::string,std::weak_ptr<LiveAsset>> LiveAsset::_liveAssetCache = {};

void LiveAsset::SetAssetId(const std::string &id) {
  _assetId = id;
}

std::string LiveAsset::GetAssetId() const {
  return _assetId;
}

bool LiveAsset::IsCached(const std::string &assetId) {
  return _liveAssetCache.contains(assetId) && !_liveAssetCache[assetId].expired();
}

std::shared_ptr<LiveAsset> LiveAsset::Resolve(const std::string &assetId) {
  if(const auto assetMeta = Engine::Get()->GetAssetSubsystem().lock()->FindAssetMeta(assetId).lock()) {
    return Resolve(assetMeta);
  }

  return {};
}

std::shared_ptr<LiveAsset> LiveAsset::Resolve(const std::shared_ptr<AssetMeta> &asset) {
  if(const auto assetSubsystem = Engine::Get()->GetAssetSubsystem().lock()) {
    return assetSubsystem->LoadAsset(asset->id);
  }

return {};
}

std::shared_ptr<LiveAsset> LiveAsset::Resolve(const fs::path &assetPath) {
  if(const auto meta = Engine::Get()->GetAssetSubsystem().lock()->LoadAssetMeta(assetPath)) {
    return Resolve(meta);
  }

  return {};
}
}
