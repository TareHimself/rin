// META_IGNORE
#pragma once
#include <functional>
#include "Field.hpp"
#include "Reference.hpp"
#include "Value.hpp"
#include "aerox/utils.hpp"

namespace aerox::meta {
typedef std::function<Value(const Reference& instance, const std::vector<Reference>& args)>
functionTypedef;

struct Function : Field
{
private:
  functionTypedef _call;

  template <typename T>
  static void AddArgument(std::vector<Reference>& vec, T& arg)
  {
    if(std::is_same_v<T,Reference>) {
      vec.push_back(arg);
    } else {
      vec.push_back(Reference(arg));
    }
  }

protected:
  bool _isStatic = false;
public:
  Function(const std::string& name, functionTypedef call,bool isStatic);

  bool IsStatic() const;
      
  template <typename T, typename... Args>
  Value Call(T* instance, Args&&... args);
      
  template <typename... Args>
    Value Call(Reference& instance, Args&&... args);

  template <typename... Args>
  Value CallStatic(Args&&... args);
};

template <typename T, typename... Args>
 Value Function::Call(T* instance, Args&&... args)
{
  Reference ref = Reference(instance);
  return Call(ref,std::forward<Args>(args)...);
}

template <typename ... Args> Value Function::Call(
    Reference &instance, Args &&... args) {

  const auto instTypeFlags = instance.GetType().GetFlags();
  utils::vassert(instTypeFlags.Has(ePrimitive) || instTypeFlags.Has(ePointer),"Instance must be a pointer");
      
  std::vector<Reference> values;
  ((AddArgument(values, args)), ...);

      

  return _call(instance, values);
}

template <typename ... Args>
Value Function::CallStatic(Args&&... args) {
  utils::vassert(_isStatic,"Function is not static");
      
  return Call<void *,Args...>(nullptr,std::forward<Args>(args)...);
}
}
