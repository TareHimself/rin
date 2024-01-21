#ifndef VENGINE_TYPES
#define VENGINE_TYPES
#include <deque>
#include <functional>

namespace vengine {
typedef std::function<void()> cleanupCallback;

struct CleanupQueue {
private:
  std::deque<std::function<void()>> _cleaners;

public:
  void Push(const std::function<void()> & cleaner) {
    _cleaners.push_front(cleaner);
  }

  void Run() {
    for(const auto &fn : _cleaners) {
      fn();
    }

    _cleaners.clear();
  }
};

enum EInputMode {
  GameOnly,
  UiOnly,
  GameAndUi
};
}
#endif
