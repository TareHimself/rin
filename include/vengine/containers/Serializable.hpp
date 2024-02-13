#pragma once
#include "String.hpp"
#include "Serializable.hpp"
#include "vengine/IReflected.hpp"

namespace vengine {
class Buffer;
}

namespace vengine {
class Serializable : public IReflected {
public:
  virtual ~Serializable() = default;
  
  virtual void WriteTo(Buffer &store) = 0;
  virtual void ReadFrom(Buffer &store) = 0;
};

}
