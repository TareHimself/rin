#pragma once
#include "types.hpp"
#include "vengine/containers/Serializable.hpp"

namespace vengine::assets {
class Asset : public Serializable {
  VEngineAssetHeader _header{};
public:
  VEngineAssetHeader GetHeader() const;
  void SetHeader(const VEngineAssetHeader &header);
};
}
