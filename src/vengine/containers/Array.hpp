#pragma once
#include <vector>

namespace vengine {
template <typename T>
class Array : public std::vector<T> {
public:

  void push(T &data);
  void pop();
  void remove(size_t index);
};

template <typename T> void Array<T>::push(T &data) {
  this->push_back(data);
}

template <typename T> void Array<T>::pop() {
  this->pop_back();
}

template <typename T> void Array<T>::remove(size_t index) {
  typename std::vector<T>::iterator it = this->begin();
  std::advance(it, index);
  this->erase(it);
}
}
