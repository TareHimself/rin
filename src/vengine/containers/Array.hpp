#pragma once
#include <vector>
#include <functional>
namespace vengine {


template <typename T>
class Array : public std::vector<T> {
public:
  Array();
  Array(size_t allocSize);
  
  void push(T &data);
  void pop();
  void remove(size_t index);
  size_t indexOf(T &data,std::function<bool(T &a,T &b)> equality);
};

template <typename T> Array<T>::Array() : std::vector<T>() {
}

template <typename T> Array<T>::Array(size_t allocSize) : std::vector<T>(allocSize) {
  
}

template <typename T> void Array<T>::push(T &data) {
  this->push_back(data);
}

template <typename T> void Array<T>::pop() {
  this->pop_back();
}

template <typename T> void Array<T>::remove(size_t index) {
  auto it = this->begin();
  std::advance(it, index);
  this->erase(it);
}

template <typename T> size_t Array<T>::indexOf(T &data,std::function<bool(T &a,T &b)> equality) {
  for(auto i = 0; i < this->size(); i++) {
    if(equality(data,this->at(i))) {
      return i;
    }
  }

  return -1;
}
}
