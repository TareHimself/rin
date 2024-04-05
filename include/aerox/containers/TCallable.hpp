#pragma once
#include "aerox/typedefs.hpp"
#include <functional>

namespace aerox {

template <typename TReturn, typename... TArgs>
class TCallable {
public:
  [[nodiscard]] virtual bool IsValid() const {
    return false;
  }

  virtual ~TCallable() = default;

  virtual TReturn Call(TArgs... args) { 
    static_assert(true,"This class is actually abstract");
  }
};

template <typename TReturn, typename... TArgs>
class TFunction : public TCallable<TReturn, TArgs...> {
  using FunctionType = std::function<TReturn(TArgs...)>;
  FunctionType _function;

public:
  TFunction(const FunctionType &func, bool move = true) {
    _function = move ? std::move(func) : func;
  }

  ~TFunction() override = default;

  bool IsValid() const override {
    return true;
  }

  TReturn Call(TArgs... args) override {
    return _function(std::forward<TArgs>(args)...);
  }
};

template <typename TReturn, typename... TArgs>
std::shared_ptr<TFunction<TReturn,TArgs...>> newCallable(const std::function<TReturn(TArgs...)>& func,bool move = true) {
  return manage<TFunction<TReturn,TArgs>>(func,move);
}


template <class TClass, typename TReturn, typename... TArgs>
using ClassFunctionDef = TReturn(TClass::*)(TArgs...);

template <class TClass, typename TReturn, typename... TArgs>
class TClassFunction : public TCallable<TReturn, TArgs...> {
  using ClassType = TClass *;
  

  ClassType _instance;
  ClassFunctionDef<TClass,TReturn,TArgs...> _function;

public:
  TClassFunction(ClassFunctionDef<TClass,TReturn,TArgs...> function, ClassType instance) {
    _instance = instance;
    _function = function;
  }

  ~TClassFunction() override = default;

  [[nodiscard]] bool IsValid() const override {
    return _instance != nullptr;
  }

  TReturn Call(TArgs... args) override {
    return (_instance->*_function)(std::forward<TArgs>(args)...);
  }

  TReturn Call(TClass *instance, TArgs... args) {
    return (instance->*_function)(std::forward<TArgs>(args)...);
  }
};

template <class TClass, typename TReturn, typename... TArgs>
std::shared_ptr<TClassFunction<TClass,TReturn,TArgs...>> newCallable(ClassFunctionDef<TClass,TReturn,TArgs...> func,TClass * instance) {
  return manage<TClassFunction<TClass,TReturn,TArgs...>>(func,instance);
}

template <class TClass, typename TReturn, typename... TArgs>
class TManagedFunction : public TCallable<TReturn, TArgs...> {

  std::weak_ptr<TClass> _instance;
  ClassFunctionDef<TClass,TReturn,TArgs...> _function;

public:
  TManagedFunction(ClassFunctionDef<TClass,TReturn,TArgs...> function, std::weak_ptr<TClass> instance) {
    _instance = instance;
    _function = function;
  }

  ~TManagedFunction() override = default;

  [[nodiscard]] bool IsValid() const override {
    return !_instance.expired();
  }

  TReturn Call(TArgs... args) override {
    return (_instance.lock().get()->*
            _function)(std::forward<TArgs>(args)...);
  }

  TReturn Call(std::shared_ptr<TClass> &instance, TArgs... args) {
    return (instance.Get()->*_function)(std::forward<TArgs>(args)...);
  }
};

template <class TClass, typename TReturn, typename... TArgs>
std::shared_ptr<TManagedFunction<TClass,TReturn,TArgs...>> newCallable(ClassFunctionDef<TClass,TReturn,TArgs...> func,std::weak_ptr<TClass> instance) {
  return manage<TManagedFunction<TClass,TReturn,TArgs...>>(func,instance);
}

}
