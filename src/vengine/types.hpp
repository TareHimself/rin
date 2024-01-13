#pragma once
#include <deque>
#include <functional>

namespace vengine {
typedef std::function<void()> cleanupCallback;

struct CleanupQueue {
private:
  std::deque<std::function<void()>> cleaners;

public:
  void push(const std::function<void()> & cleaner) {
    cleaners.push_front(cleaner);
  }

  void run() {
    for(auto fn : cleaners) {
      fn();
    }

    cleaners.clear();
  }
};
}
