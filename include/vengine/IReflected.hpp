#pragma once
#include <memory>
#include <reflect/Factory.hpp>
#include <reflect/wrap/Reflected.hpp>

namespace vengine {
class IReflected {
public:
  virtual ~IReflected() = default;
  virtual std::shared_ptr<reflect::wrap::Reflected> GetReflected() const = 0;
};
}

#ifndef VENGINE_IMPLEMENT_REFLECTED_INTERFACE
#define VENGINE_IMPLEMENT_REFLECTED_INTERFACE(Type) \
virtual std::shared_ptr<reflect::wrap::Reflected> GetReflected() const override { \
    return reflect::factory::find<Type>(); \
}
#endif

#ifndef VENGINE_IMMEDIATE_REFLECTED
#define VENGINE_IMMEDIATE_REFLECTED(Type) \
RFUNCTION() \
  static Managed<Type> Construct() { \
  return newManagedObject<Type>(); \
} \
  \
VENGINE_IMPLEMENT_REFLECTED_INTERFACE(Type) 
#endif
