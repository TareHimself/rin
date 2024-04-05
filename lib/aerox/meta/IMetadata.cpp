#include "aerox/meta/IMetadata.hpp"

#include "aerox/meta/Factory.hpp"

namespace aerox::meta {

json IMetadata::ToJson()  {
  if(const auto meta = GetMeta()) {
    json result{};

    for(const auto &prop : meta->GetProperties()) {
      auto val = prop->Get(this);
      if(const auto propMeta = find(val.GetType())) {
        if(const auto serializer = propMeta->FindFunction("ToJson")) {

          const json r = serializer->CallStatic(val);
          result[prop->GetName()] = r;
        }
      }
    }

    return {{META_SERIALIZATION_KEY_ID,meta->GetName()},{META_SERIALIZATION_KEY_PROPERTIES,result}};
  }
  
  return {};
}

void IMetadata::FromJson(json &data) {
  if(const auto meta = GetMeta()) {
    auto props = data["properties"];
    for(const auto &prop : meta->GetProperties()) {
      auto val = prop->Get(this);
      if(const auto propMeta = find(val.GetType())) {
        if(const auto deserializer = propMeta->FindFunction("FromJson")) {
          auto ref = prop->Get(this);
          deserializer->Call(ref,props[prop->GetName()],ref);
        }
      }
    }
  }
}
}
