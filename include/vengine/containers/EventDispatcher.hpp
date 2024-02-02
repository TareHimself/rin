#pragma once
#include "Array.hpp"

#include <map>
#include <ranges>

namespace vengine {


class EventDispatcher {
  uint64_t _lastId = 0;
  
  std::map<uint64_t,std::function<void ()>>  _listeners;
public:
  std::function<void ()> On(const std::function<void ()>& listener);
  
  void Emit() const;
};

inline std::function<void ()> EventDispatcher::On(
    const std::function<void()> &listener) {
  auto id = ++_lastId;
  _listeners.emplace(id, listener);
  return [=] {
    if(_listeners.contains(id)) {
      _listeners.erase(_listeners.find(id));
    }
  };
}

inline void EventDispatcher::Emit() const {
  for(const auto &val : _listeners | std::views::values) {
    val();
  }
}
}
