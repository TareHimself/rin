#pragma once
#include "String.hpp"
#include "Serializable.hpp"

namespace vengine {
class Buffer;
}

namespace vengine {
class Serializable {
public:
  virtual ~Serializable() = default;

  // Used to identify this type in an archive
  virtual String GetSerializeId() = 0;
  virtual void WriteTo(Buffer &store) = 0;
  virtual void ReadFrom(Buffer &store) = 0;

  
};

}
