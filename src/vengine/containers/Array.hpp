#pragma once
#include <fstream>
#include <vector>
#include <functional>
namespace vengine {


template <typename T>
class Array : public std::vector<T> {
public:

  using std::vector<T>::vector;
  
  // Array();
  // Array(size_t allocSize);
  // Array(const std::initializer_list<T> &other);
  // Array(T* begin,T*end);
  
  
  void push(T &data);
  void pop();
  void remove(size_t index);
  int64_t indexOf(T &data,std::function<bool(T &a,T &b)> equality);

  size_t typeSize() const;
  
  size_t byteSize() const;

  Array<T>& operator=(const std::vector<T>& other);
  

  friend std::ofstream &operator <<(std::ofstream &out, const Array<T> &a) {
    const uint64_t arraySize = a.size();
    
    out.write(reinterpret_cast<const char *>(&arraySize),sizeof(size_t));
    out.write(a.data(),a.byteSize());
    return out;
  }
  

  friend std::ifstream &operator >>(std::ifstream &in, Array<T> &a) {
    size_t elementNum;
    in.read(reinterpret_cast<char *>(&elementNum),sizeof(size_t));
    a.resize(elementNum);
    in.read(a.data(),a.byteSize());
    return in;
  }

  // Array<T>& operator=(const std::initializer_list<T>& other);
};

// template <typename T> Array<T>::Array() : std::vector<T>() {
//   
// }
//
// template <typename T> Array<T>::Array(size_t allocSize) : std::vector<T>(allocSize) {
//   
// }
//
// template <typename T> Array<T>::Array(const std::initializer_list<T> &other) : std::vector<T>(other) {
// }
//
// template <typename T> Array<T>::Array(T *begin, T *end) : std::vector<T>(begin,end) {
// }

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

template <typename T> int64_t Array<T>::indexOf(T &data,std::function<bool(T &a,T &b)> equality) {
  for(auto i = 0; i < this->size(); i++) {
    if(equality(data,this->at(i))) {
      return i;
    }
  }

  return -1;
}

template <typename T> size_t Array<T>::typeSize() const {
  return sizeof(T);
}

template <typename T> size_t Array<T>::byteSize() const {
  return this->size() * sizeof(T);
}

template <typename T> Array<T> & Array<T>::operator=(
    const std::vector<T> &other) {
  this->clear();
  for(auto item : other) {
    this->push(item);
  }

  return *this;
}

// template <typename T> Array<T> & Array<T>::operator=(
//     const std::initializer_list<T> &other) {
//   
//   Array<T> arr = Array(other.begin(),other.end());
//   
//   return *this;
// }

}
