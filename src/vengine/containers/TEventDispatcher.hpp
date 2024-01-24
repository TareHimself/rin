#pragma once
#include <map>
#include <ranges>

namespace vengine {

template<class  ...T>
class TEventDispatcher {
  uint64_t _lastId = 0;
  
  std::map<uint64_t,std::function<void (T...)>>  _listeners;
public:
  std::function<void ()> On(const std::function<void (T...)>& listener);
  
  void Emit(T... args);
};

template <class ... T> std::function<void ()> TEventDispatcher<T...>::On(
    const std::function<void(T...)> &listener) {
  auto id = ++_lastId;
  _listeners.emplace(id,listener);
  return [=] {
    if(_listeners.contains(id)) {
      _listeners.erase(_listeners.find(id));
    }
  };
}

template <class ... T> void TEventDispatcher<T...>::Emit(T... args) {
  for(const auto &val : _listeners | std::views::values) {
    val(args...);
  }
}
}
