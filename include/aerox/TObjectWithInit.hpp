#pragma once
#include "Object.hpp"

namespace  aerox {

template<typename ...TArgs>
class TObjectWithInit : public Object {
  bool _initialized = false;
  bool _initializing = false;
public:
  void Init(TArgs... args);
  virtual bool IsInitialized() const;
  virtual bool IsInitializing() const;
  virtual void OnInit(TArgs... args);
};

template <typename ... TArgs> void TObjectWithInit<TArgs...>::Init(
    TArgs ... args) {
  _initializing = true;
  OnInit(std::forward<TArgs>(args)...);
  _initialized = true;
  _initializing = false;
}

template <typename ... TArgs> bool TObjectWithInit<TArgs...>::
IsInitialized() const {
  return _initialized;
}

template <typename ... TArgs> bool TObjectWithInit<TArgs...>::
IsInitializing() const {
  return _initializing;
}


template <typename ... TArgs> void TObjectWithInit<TArgs...>::OnInit(
    TArgs ... args) {
}
}
