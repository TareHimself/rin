#pragma once
#include "String.hpp"
#include "Serializable.hpp"
#include "aerox/meta/IMetadata.hpp"

namespace aerox {
class Buffer;
}

namespace aerox {
class Serializable {
public:
  virtual ~Serializable() = default;
  
  virtual void WriteTo(Buffer &store) = 0;
  virtual void ReadFrom(Buffer &store) = 0;
};

}
