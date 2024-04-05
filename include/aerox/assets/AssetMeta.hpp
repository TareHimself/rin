#pragma once
#include "types.hpp"
#include "aerox/containers/Serializable.hpp"
#include "aerox/meta/Macro.hpp"

namespace aerox::assets {

struct AssetMeta : Serializable {

  uint32_t version;

  std::string type;

  std::string id;

  std::set<std::string> tags;

  void ReadFrom(Buffer &store) override;

  void WriteTo(Buffer &store) override;
};
}
