#pragma once
#include "TCallable.hpp"
#include <list>
#include <mutex>

namespace aerox {

template <typename... TArgs>
struct TDelegateCallableWrap {
private:
  uint64_t _calls = 0;
  uint64_t _maxCalls = 0;
  bool _pendingRemove = false;
  std::shared_ptr<TCallable<void, TArgs...>> _callable;

public:
  bool callOnce = false;
  TDelegateCallableWrap() = default;

  TDelegateCallableWrap(const std::shared_ptr<TCallable<void, TArgs...>> &callable,uint64_t maxCalls) {
    _callable = callable;
    _maxCalls = maxCalls;
  }

  bool operator==(const TDelegateCallableWrap &other) const {
    return _callable == other._callable;
  }
  
  bool IsValid() {

    if(_pendingRemove) return false;
    
    if(_maxCalls != 0 && _calls >= _maxCalls) return false;
    
    return _callable && _callable->IsValid();
  }

  void MarkForRemove() {
    _pendingRemove = true;
  }

  void Call(TArgs... args){
    _callable->Call(std::forward<TArgs>(args)...);
    _calls++;
  }
};

template <class... TArgs>
class TDelegate;

template <class... TArgs>
struct TDelegateHandle {
private:
  std::weak_ptr<TDelegateCallableWrap<TArgs...>> _callback;
  
public:
  TDelegateHandle() = default;
  TDelegateHandle(const std::weak_ptr<TDelegateCallableWrap<TArgs...>> &callback);

  [[nodiscard]] bool IsValid() const;
  void UnBind();
};


template <class... TArgs>
class TDelegate : std::enable_shared_from_this<TDelegate<TArgs...>>{
  std::mutex _guard;
  std::vector<std::shared_ptr<TDelegateCallableWrap<TArgs...>>> _listeners;
public:
  friend TDelegateHandle<TArgs...>;

  std::shared_ptr<TDelegateHandle<TArgs...>> Bind(
      const std::shared_ptr<TCallable<void, TArgs...>> &listener,
      uint64_t maxCalls = 0);

  std::shared_ptr<TDelegateHandle<TArgs...>> BindFunction(
      std::function<void(TArgs...)> listener,
      uint64_t maxCalls = 0);

  template <class TClass>
  using TClassFunctionConcept = void(TClass::*)(TArgs...);

  template <class TClass>
  std::shared_ptr<TDelegateHandle<TArgs...>> BindClassFunction(TClass *instance,
    TClassFunctionConcept<TClass>
    listener,
    uint64_t maxCalls = 0);

  template <class TClass>
  std::shared_ptr<TDelegateHandle<TArgs...>> BindManagedFunction(std::weak_ptr<TClass> instance,
    TClassFunctionConcept<TClass>
    listener,
    uint64_t maxCalls = 0);

  size_t GetNumListeners() const;

  bool IsBound() const;
  
  void Execute(TArgs... args);
};


template <class... TArgs>
TDelegateHandle<TArgs...>::TDelegateHandle(const std::weak_ptr<TDelegateCallableWrap<TArgs...>> &callback) {
  _callback = callback;
}

template <class... TArgs>
bool TDelegateHandle<TArgs...>::IsValid() const {
  return _callback && _callback.Reserve()->IsValid();
}

template <class... TArgs>
void TDelegateHandle<TArgs...>::UnBind() {
  if (auto ref = _callback.lock()) {
    ref->MarkForRemove();
    ref = {};
  }
}

template <class... TArgs> std::shared_ptr<TDelegateHandle<TArgs...>> TDelegate<TArgs
  ...>::Bind(
    const std::shared_ptr<TCallable<void, TArgs...>> &listener, uint64_t maxCalls) {
  std::lock_guard<std::mutex> m(_guard);

  auto wrap = std::make_shared<TDelegateCallableWrap<TArgs...>>(listener,maxCalls);
  _listeners.push_back(wrap);
  return std::make_shared<TDelegateHandle<TArgs...>>(wrap);
}

template <class... TArgs> std::shared_ptr<TDelegateHandle<TArgs...>> TDelegate<TArgs
  ...>::BindFunction(
    std::function<void(TArgs...)> listener, uint64_t maxCalls) {
  return Bind(std::make_shared<TFunction<void, TArgs...>>(listener), maxCalls);
}

template <class... TArgs> template <class TClass> std::shared_ptr<TDelegateHandle<TArgs
  ...>>
TDelegate<TArgs
  ...>::BindClassFunction(TClass *instance,
                          TClassFunctionConcept<TClass> listener,
                          uint64_t maxCalls) {
  return Bind(
      std::make_shared<TClassFunction<TClass, void, TArgs...>>(listener, instance),
      maxCalls);
}

template <class... TArgs> template <class TClass> std::shared_ptr<TDelegateHandle<TArgs
  ...>>
TDelegate<TArgs
  ...>::BindManagedFunction(std::weak_ptr<TClass> instance,
                            TClassFunctionConcept<TClass> listener,
                            uint64_t maxCalls) {
  return Bind(
      std::make_shared<TManagedFunction<TClass, void, TArgs...>>(listener, instance),
      maxCalls);
}

template <class... TArgs> size_t TDelegate<TArgs...>::GetNumListeners() const {
  return _listeners.size();
}

template <class ... TArgs> bool TDelegate<TArgs...>::IsBound() const {
  return GetNumListeners() > 0;
}

template <class... TArgs> void TDelegate<TArgs...>::Execute(TArgs... args) {
  std::lock_guard<std::mutex> m(_guard);
  std::vector<uint32_t> pendingDelete;
  auto i = 0;
  for (auto &val : _listeners) {
    if(val->IsValid()) {
      val->Call(std::forward<TArgs>(args)...);
    } else {
      pendingDelete.push_back(i);
    }

    i++;
  }

  auto offset = 0;

  for(const auto idx : pendingDelete) {
    _listeners.erase(_listeners.begin() + (idx - offset));
    offset++;
  }
}

template <typename... TArgs>
inline std::shared_ptr<TDelegate<TArgs...>> newDelegate() {
  return std::make_shared<TDelegate<TArgs...>>();
}
}

#ifndef DECLARE_DELEGATE
#define DECLARE_DELEGATE(DelegateName,...) \
std::shared_ptr<aerox::TDelegate<__VA_ARGS__>> ##DelegateName = aerox::newDelegate<__VA_ARGS__>();
#endif

#ifndef DECLARE_DELEGATE_HANDLE
#define DECLARE_DELEGATE_HANDLE(ParamName,...) \
std::shared_ptr<TDelegateHandle<__VA_ARGS__>> ##ParamName;
#endif
