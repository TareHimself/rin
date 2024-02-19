#pragma once
#include "Set.hpp"
#include "vengine/types.hpp"
#include <list>
#include <map>
#include <ranges>

namespace vengine {

template<class  ...T>
class TDispatcher {
  std::atomic<uint64_t> _lastId = 0;

  
  std::list<std::pair<bool,TFunction<void,T...>>>  _listeners;
public:
  uint64_t Bind(const std::function<void(T...)>& listener,bool bTriggerOnce = false);
  size_t GetNumListeners() const;
  void UnBind(uint64_t id);
  void operator() (T... args);
};

template <class ... T> uint64_t TDispatcher<T...>::Bind(
    const std::function<void(T...)> &listener,bool bTriggerOnce) {
  auto id = ++_lastId;
  TFunction<void,T...> func = {id,listener};
  _listeners.emplace_back(bTriggerOnce,func);
  return id;
}

template <class ... T> size_t TDispatcher<T...>::GetNumListeners() const {
  return _listeners.size();
}

template <class ... T> void TDispatcher<T...>::UnBind(uint64_t id) {
  _listeners.remove_if([&](const std::pair<bool,TFunction<void,T...>>& _Other) { return _Other.second.GetId() == id; });
}

template <class ... T> void TDispatcher<T...>::operator()(T... args){
  for(const auto &val : _listeners) {
    val.second(args...);
    if(val.first) {
      _listeners.remove(val);
    }
  }
}
}
