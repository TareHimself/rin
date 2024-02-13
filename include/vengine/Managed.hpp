#pragma once
#include "log.hpp"

#include <functional>
#include <mutex>
#include <set>
#include <typeinfo>
#include <reflect/Factory.hpp>

namespace vengine {
template <class T>
class Ref;

template <class T>
class Managed;

template <class T>
struct ManagedBlock {
  friend class Managed<T>;
  friend class Ref<T>;
  T *data = nullptr;
  std::mutex * mutex = nullptr;
  uint64_t locks = 0;
  std::set<Ref<T> *> weak{};
  std::set<Managed<T> *> strong{};
  std::function<void(T *)> deleter = [](const T *ptr) {
    delete ptr;
  };
  bool bIsPendingDelete = false;


  void Lock() const;

  void Unlock();

  ManagedBlock();

  ~ManagedBlock();
};

// Stores a reference to T, will deallocate T once T has no more Managed references or ForceClear is called
template <class T>
class Managed {
  ManagedBlock<T> *_block = nullptr;

protected:
  void UseBlock(ManagedBlock<T> * block);
public:
  Managed();
  Managed(std::nullptr_t);
  Managed(ManagedBlock<T> *block);
  Managed(T *data);
  Managed(T *data, std::function<void(T *)> deleter);
  Managed(Managed &other);
  Managed(const Managed &other);
  Managed(Managed &&other) noexcept;
  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  Managed(const Managed<E> &other);
  Managed &operator=(const Managed &other);
  Managed &operator=(Managed &&other) noexcept;

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  Managed &operator=(const Managed<E> &other);

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  Managed &operator=(Managed<E> &&other) noexcept;

  T *Get();
  T *Get() const;

  template <typename E>
  Managed<E> Cast() const;

  template <typename E>
  Managed<E> CastStatic() const;

  bool operator==(const Managed &other) const;

  operator Ref<T>();
  operator Ref<T>() const;

  // operator T*();
  // operator T*() const;
  T *operator->();
  T *operator->() const;

  operator bool() const;

  void Clear();
  void ForceClear();

  Managed<T> &Swap(T *data);
  Managed<T> &Swap(T *data, std::function<void(T *)> deleter);

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator Managed<E>();

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator Managed<E>() const;

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator Ref<E>();

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator Ref<E>() const;

  ~Managed();
  friend class Ref<T>;
};

template <typename T>
struct CompareRef {
  bool operator()(const Managed<T> &a,const Managed<T> &b)const{
    return a.Get() == b.Get();
  }
};


// A weak version of Managed that will not be considered when deciding to delete the pointer
template <class T>
class Ref {
  ManagedBlock<T> *_block = nullptr;

protected:
  void UseBlock(ManagedBlock<T> * block);
public:
  Ref();
  Ref(ManagedBlock<T> *block);
  Ref(const Ref &other);
  Ref(Ref &&other) noexcept;
  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  Ref(const Ref<E> &other);

  Ref &operator=(const Ref &other);
  Ref &operator=(Ref &&other) noexcept;

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  Ref &operator=(const Ref<E> &other);
  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  Ref &operator=(Ref<E> &&other) noexcept;

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator Ref<E>();

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator Ref<E>() const;

  template <typename E>
  Ref<E> Cast() const;

  template <typename E>
  Ref<E> CastStatic() const;

  operator bool() const;

  Managed<T> Reserve();
  Managed<T> Reserve() const;

  void Clear(bool bCheckRef);
  ~Ref();
  friend class Managed<T>;
};

template <typename T>
struct CompareWeakRef {
  bool operator()(const Ref<T> &a,const Ref<T> &b)const{
    return a.Reserve().Get() == b.Reserve().Get();
  }
};

template <class T> void ManagedBlock<T>::Lock() const {
  mutex->lock();
}

template <class T> void ManagedBlock<T>::Unlock() {
  mutex->unlock();
    
  if (bIsPendingDelete && mutex->try_lock()) {
    if (data != nullptr) {
      deleter(data);
      data = nullptr;
    }

      

    for (const auto &val : strong) {
      val->Clear();
    }

    for (const auto &val : weak) {
      val->Clear(false);
    }

    strong.clear();
    weak.clear();
    mutex->unlock();

    delete this;
  }
}

template <class T> ManagedBlock<T>::ManagedBlock() {
  mutex = new std::mutex;
}

template <class T> ManagedBlock<T>::~ManagedBlock() {
  delete mutex;
  mutex = nullptr;
}

template <class T> void Managed<T>::UseBlock(ManagedBlock<T> *block) {
  Clear();
  _block = block;
  if (_block != nullptr && !_block->bIsPendingDelete) {
    _block->Lock();
    _block->strong.emplace(this);
    _block->Unlock();
  }
}

template <class T> Managed<T>::Managed() {
  _block = nullptr;
}

template <class T> Managed<T>::Managed(std::nullptr_t) {
  _block = nullptr;
}

template <class T> Managed<T>::Managed(ManagedBlock<T> *block) {
  UseBlock(block);
}

template <class T> Managed<T>::Managed(T *data) {
  _block = new ManagedBlock<T>;
  _block->data = data;
  _block->strong.emplace(this);
}

template <class T> Managed<T>::Managed(T *data,
                                       std::function<void(T *)> deleter) {
  _block = new ManagedBlock<T>;
  _block->data = data;
  _block->strong.emplace(this);
  _block->deleter = deleter;
}

template <class T> Managed<T>::Managed(Managed &other) {
  UseBlock(other._block);
}

template <class T> Managed<T>::Managed(const Managed &other) {
  UseBlock(other._block);
}

template <class T> Managed<T>::Managed(Managed &&other) noexcept {
  UseBlock(other._block);
}

template <class T> template <typename E, typename> Managed<T>::Managed(
    const Managed<E> &other) {
  *this = other.template Cast<T>();
}

template <class T> Managed<T> &Managed<T>::operator=(const Managed &other) {
  if (&other != this) {
    UseBlock(other._block);
  }
  return *this;
}

template <class T> Managed<T> &Managed<T>::operator
=(Managed &&other) noexcept {
  if (&other != this) {
    UseBlock(other._block);
  }
  return *this;
}

template <class T> template <typename E, typename> Managed<T> &Managed<T>::
operator=(const Managed<E> &other) {
  if (&other != this) {
    *this = other.template Cast<T>();
  }
  return *this;
}

template <class T> template <typename E, typename> Managed<T> &Managed<T>::
operator=(Managed<E> &&other) noexcept {
  if (&other != this) {
    *this = other.template Cast<T>();
  }
  return *this;
}

template <class T> T *Managed<T>::Get() {
  if (_block == nullptr || _block->bIsPendingDelete)
    return nullptr;
  return _block->data;
}

template <class T> T *Managed<T>::Get() const {
  if (_block == nullptr || _block->bIsPendingDelete)
    return nullptr;
  return _block->data;
}

template <class T> template <typename E> Managed<E> Managed<T>::Cast() const {
  if(_block == nullptr) return {};

  if(_block->bIsPendingDelete) return {};

  if(auto castResult = dynamic_cast<E *>(_block->data)) {
    return Managed<E>(reinterpret_cast<ManagedBlock<E> *>(_block));
  }
  
  return {};
}

template <class T> template <typename E> Managed<E> Managed<
  T>::CastStatic() const {
  return Managed<E>(reinterpret_cast<ManagedBlock<E> *>(_block));
}

template <class T> bool Managed<T>::operator==(const Managed &other) const {
  return _block == other._block;
}

template <class T> Managed<T>::operator Ref<T>() {
  return Ref<T>(_block);
}

template <class T> Managed<T>::operator Ref<T>() const {
  return Ref<T>(_block);
}

template <class T> T *Managed<T>::operator->() {
  if (_block == nullptr || _block->bIsPendingDelete)
    return nullptr;
  return _block->data;
}

template <class T> T *Managed<T>::operator->() const {
  if (_block == nullptr || _block->bIsPendingDelete)
    return nullptr;
  return _block->data;
}

template <class T> Managed<T>::operator bool() const {
  if (_block == nullptr || _block->bIsPendingDelete)
    return false;
  return _block->data != nullptr;
}

template <class T> void Managed<T>::Clear() {
  if (_block != nullptr && !_block->bIsPendingDelete) {
    _block->Lock();
    _block->strong.erase(this);
    if (_block->strong.empty()) {
      _block->bIsPendingDelete = true;
    }
    _block->Unlock();
  }

  _block = nullptr;
}

template <class T> void Managed<T>::ForceClear() {
  if (_block != nullptr && !_block->bIsPendingDelete) {
    _block->Lock();
    _block->strong.erase(this);
    _block->bIsPendingDelete = true;

    _block->strong.clear();
    _block->weak.clear();
    _block->Unlock();
  }

  _block = nullptr;
}

template <class T> Managed<T> &Managed<T>::Swap(T *data) {
  UseBlock(new ManagedBlock<T>);
  return *this;
}

template <class T> Managed<T> &Managed<T>::Swap(T *data,
                                                std::function<void(T *)>
                                                deleter) {
  UseBlock(new ManagedBlock<T>);
  
  return *this;
}

template <class T> template <typename E, typename> Managed<T>::operator Managed<
  E>() {
  return Cast<E>();
}

template <class T> template <typename E, typename> Managed<T>::operator Managed<
  E>() const {
  return Cast<E>();
}

template <class T> template <typename E, typename> Managed<T>::operator
Ref<E>() {
  return Ref<E>(Cast<E>());
}

template <class T> template <typename E, typename> Managed<T>::operator
Ref<E>() const {
  return Ref<E>(Cast<E>());
}

template <class T> Managed<T>::~Managed() {
  Clear();
}

template <class T> void Ref<T>::UseBlock(ManagedBlock<T> *block) {
  Clear(true);
  _block = block;
  if (_block != nullptr && !_block->bIsPendingDelete) {
    _block->Lock();
    _block->weak.emplace(this);
    _block->Unlock();
  }
}

template <class T> Ref<T>::Ref() {
  _block = nullptr;
}

template <class T> Ref<T>::Ref(ManagedBlock<T> *block) {
  UseBlock(block);
}

template <class T> Ref<T>::Ref(const Ref &other) {
  UseBlock(other._block);
}

template <class T> Ref<T>::Ref(Ref &&other) noexcept {
  UseBlock(other._block);
}

template <class T> template <typename E, typename> Ref<T>::Ref(
    const Ref<E> &other) {
  *this = other.template Cast<T>();
}

template <class T> Ref<T> &Ref<T>::operator=(
    const Ref &other) {
  if (&other != this) {
    UseBlock(other._block);
  }
  return *this;
}

template <class T> Ref<T> &Ref<T>::operator=(
    Ref &&other) noexcept {
  if (&other != this) {
    UseBlock(other._block);
  }
  return *this;
}

template <class T> template <typename E, typename> Ref<T> &Ref<
  T>::operator=(const Ref<E> &other) {
  *this = other.template Cast<T>();
  return *this;
}

template <class T> template <typename E, typename> Ref<T> &Ref<
  T>::operator=(Ref<E> &&other) noexcept {
  *this = other.template Cast<T>();
  return *this;
}

template <class T> template <typename E, typename> Ref<T>::operator
Ref<E>() {
  return Ref<E>(reinterpret_cast<ManagedBlock<E> *>(_block));
}

template <class T> template <typename E, typename> Ref<T>::operator
Ref<E>() const {
  return Ref<E>(reinterpret_cast<ManagedBlock<E> *>(_block));
}

template <class T> template <typename E> Ref<E> Ref<
  T>::Cast() const {
  if(_block == nullptr) return {};

  if(_block->bIsPendingDelete) return {};

  if(auto castResult = dynamic_cast<E *>(_block->data)) {
    return Ref<E>(reinterpret_cast<ManagedBlock<E> *>(_block));
  }
  
  return {};
}

template <class T> template <typename E> Ref<E> Ref<
  T>::CastStatic() const {
  return Ref<E>(reinterpret_cast<ManagedBlock<E> *>(_block));
}

template <class T> Ref<T>::operator bool() const {
  return _block != nullptr && !_block->bIsPendingDelete && _block->data !=
         nullptr;
}

template <class T> Managed<T> Ref<T>::Reserve() {
  return Managed<T>(_block);
}

template <class T> Managed<T> Ref<T>::Reserve() const {
  return Managed<T>(_block);
}

template <class T> void Ref<T>::Clear(bool bCheckRef) {
  if(bCheckRef) {
    if (_block != nullptr && !_block->bIsPendingDelete) {
      _block->Lock();
      _block->weak.erase(this);
      _block->Unlock();
    }
  }
  _block = nullptr;
}

template <class T> Ref<T>::~Ref() {
  Clear(true);
}
}
