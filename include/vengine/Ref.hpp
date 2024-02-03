#pragma once
#include "log.hpp"

#include <functional>
#include <mutex>
#include <set>
#include <typeinfo>
#include <reflect/Factory.hpp>
#include <reflect/Reflect.hpp>

namespace vengine {
template <class T>
class WeakRef;

template <class T>
class Ref;

template <class T>
struct RefBlock {
  friend class Ref<T>;
  friend class WeakRef<T>;
  T *data = nullptr;
  std::mutex * mutex = nullptr;
  uint64_t locks = 0;
  std::set<WeakRef<T> *> weak{};
  std::set<Ref<T> *> strong{};
  std::function<void(T *)> deleter = [](const T *ptr) {
    delete ptr;
  };
  bool bIsPendingDelete = false;


  void Lock() const {
    mutex->lock();
  }

  void Unlock() {
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

  RefBlock() {
    mutex = new std::mutex;
    if(auto reflected = reflect::factory::find<T>()) {
      log::engine->info("Created control block for {}",reflected->GetName());
    } else {
      log::engine->info("Created control block for unknown type");
    }
  }

  ~RefBlock() {
    if(auto reflected = reflect::factory::find<T>()) {
      log::engine->info("Deleting control block for {}",reflected->GetName());
    } else {
      log::engine->info("Deleting control block for unknown type");
    }
    delete mutex;
    mutex = nullptr;
  }
};

// Stores a reference to T, will deallocate T once T has no more Refs or ForceClear is called
template <class T>
class Ref {
  RefBlock<T> *_block = nullptr;

protected:
  void UseBlock(RefBlock<T> * block);
public:
  Ref();
  Ref(std::nullptr_t);
  Ref(RefBlock<T> *block);
  Ref(T *data);
  Ref(T *data, std::function<void(T *)> deleter);
  Ref(Ref &other);
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

  T *Get();
  T *Get() const;

  template <typename E>
  Ref<E> Cast() const;

  template <typename E>
  Ref<E> CastStatic() const;

  bool operator==(const Ref &other) const;

  operator WeakRef<T>();
  operator WeakRef<T>() const;

  // operator T*();
  // operator T*() const;
  T *operator->();
  T *operator->() const;

  operator bool() const;

  void Clear();
  void ForceClear();

  Ref<T> &Swap(T *data);
  Ref<T> &Swap(T *data, std::function<void(T *)> deleter);

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator Ref<E>();

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator Ref<E>() const;

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator WeakRef<E>();

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator WeakRef<E>() const;

  ~Ref();
  friend class WeakRef<T>;
};

template <typename T>
struct CompareRef {
  bool operator()(const Ref<T> &a,const Ref<T> &b)const{
    return a.Get() == b.Get();
  }
};


// A weak version of Ref that will not be considered when deciding to delete the ref
template <class T>
class WeakRef {
  RefBlock<T> *_block = nullptr;

protected:
  void UseBlock(RefBlock<T> * block);
public:
  WeakRef();
  WeakRef(RefBlock<T> *block);
  WeakRef(const WeakRef &other);
  WeakRef(WeakRef &&other) noexcept;
  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  WeakRef(const WeakRef<E> &other);

  WeakRef &operator=(const WeakRef &other);
  WeakRef &operator=(WeakRef &&other) noexcept;

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  WeakRef &operator=(const WeakRef<E> &other);
  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  WeakRef &operator=(WeakRef<E> &&other) noexcept;

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator WeakRef<E>();

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator WeakRef<E>() const;

  template <typename E>
  WeakRef<E> Cast() const;

  template <typename E>
  WeakRef<E> CastStatic() const;

  operator bool() const;

  Ref<T> Reserve();
  Ref<T> Reserve() const;

  void Clear(bool bCheckRef);
  ~WeakRef();
  friend class Ref<T>;
};

template <typename T>
struct CompareWeakRef {
  bool operator()(const WeakRef<T> &a,const WeakRef<T> &b)const{
    return a.Reserve().Get() == b.Reserve().Get();
  }
};

template <class T> void Ref<T>::UseBlock(RefBlock<T> *block) {
  Clear();
  _block = block;
  if (_block != nullptr && !_block->bIsPendingDelete) {
    _block->Lock();
    _block->strong.emplace(this);
    _block->Unlock();
  }
}

template <class T> Ref<T>::Ref() {
  _block = nullptr;
}

template <class T> Ref<T>::Ref(std::nullptr_t) {
  _block = nullptr;
}

template <class T> Ref<T>::Ref(RefBlock<T> *block) {
  UseBlock(block);
}

template <class T> Ref<T>::Ref(T *data) {
  _block = new RefBlock<T>;
  _block->data = data;
  _block->strong.emplace(this);
}

template <class T> Ref<T>::Ref(T *data,
                                       std::function<void(T *)> deleter) {
  _block = new RefBlock<T>;
  _block->data = data;
  _block->strong.emplace(this);
  _block->deleter = deleter;
}

template <class T> Ref<T>::Ref(Ref &other) {
  UseBlock(other._block);
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

template <class T> Ref<T> &Ref<T>::operator=(const Ref &other) {
  if (&other != this) {
    UseBlock(other._block);
  }
  return *this;
}

template <class T> Ref<T> &Ref<T>::operator
=(Ref &&other) noexcept {
  if (&other != this) {
    UseBlock(other._block);
  }
  return *this;
}

template <class T> template <typename E, typename> Ref<T> &Ref<T>::
operator=(const Ref<E> &other) {
  if (&other != this) {
    *this = other.template Cast<T>();
  }
  return *this;
}

template <class T> template <typename E, typename> Ref<T> &Ref<T>::
operator=(Ref<E> &&other) noexcept {
  if (&other != this) {
    *this = other.template Cast<T>();
  }
  return *this;
}

template <class T> T *Ref<T>::Get() {
  if (_block == nullptr || _block->bIsPendingDelete)
    return nullptr;
  return _block->data;
}

template <class T> T *Ref<T>::Get() const {
  if (_block == nullptr || _block->bIsPendingDelete)
    return nullptr;
  return _block->data;
}

template <class T> template <typename E> Ref<E> Ref<T>::Cast() const {
  if(_block == nullptr) return {};

  if(_block->bIsPendingDelete) return {};

  if(auto castResult = dynamic_cast<E *>(_block->data)) {
    return Ref<E>(reinterpret_cast<RefBlock<E> *>(_block));
  }
  
  return {};
}

template <class T> template <typename E> Ref<E> Ref<
  T>::CastStatic() const {
  return Ref<E>(reinterpret_cast<RefBlock<E> *>(_block));
}

template <class T> bool Ref<T>::operator==(const Ref &other) const {
  return _block == other._block;
}

template <class T> Ref<T>::operator WeakRef<T>() {
  return WeakRef<T>(_block);
}

template <class T> Ref<T>::operator WeakRef<T>() const {
  return WeakRef<T>(_block);
}

template <class T> T *Ref<T>::operator->() {
  if (_block == nullptr || _block->bIsPendingDelete)
    return nullptr;
  return _block->data;
}

template <class T> T *Ref<T>::operator->() const {
  if (_block == nullptr || _block->bIsPendingDelete)
    return nullptr;
  return _block->data;
}

template <class T> Ref<T>::operator bool() const {
  if (_block == nullptr || _block->bIsPendingDelete)
    return false;
  return _block->data != nullptr;
}

template <class T> void Ref<T>::Clear() {
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

template <class T> void Ref<T>::ForceClear() {
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

template <class T> Ref<T> &Ref<T>::Swap(T *data) {
  UseBlock(new RefBlock<T>);
  return *this;
}

template <class T> Ref<T> &Ref<T>::Swap(T *data,
                                                std::function<void(T *)>
                                                deleter) {
  UseBlock(new RefBlock<T>);
  
  return *this;
}

template <class T> template <typename E, typename> Ref<T>::operator Ref<
  E>() {
  return Cast<E>();
}

template <class T> template <typename E, typename> Ref<T>::operator Ref<
  E>() const {
  return Cast<E>();
}

template <class T> template <typename E, typename> Ref<T>::operator
WeakRef<E>() {
  return WeakRef<E>(Cast<E>());
}

template <class T> template <typename E, typename> Ref<T>::operator
WeakRef<E>() const {
  return WeakRef<E>(Cast<E>());
}

template <class T> Ref<T>::~Ref() {
  Clear();
}

template <class T> void WeakRef<T>::UseBlock(RefBlock<T> *block) {
  Clear(true);
  _block = block;
  if (_block != nullptr && !_block->bIsPendingDelete) {
    _block->Lock();
    _block->weak.emplace(this);
    _block->Unlock();
  }
}

template <class T> WeakRef<T>::WeakRef() {
  _block = nullptr;
}

template <class T> WeakRef<T>::WeakRef(RefBlock<T> *block) {
  UseBlock(block);
}

template <class T> WeakRef<T>::WeakRef(const WeakRef &other) {
  UseBlock(other._block);
}

template <class T> WeakRef<T>::WeakRef(WeakRef &&other) noexcept {
  UseBlock(other._block);
}

template <class T> template <typename E, typename> WeakRef<T>::WeakRef(
    const WeakRef<E> &other) {
  *this = other.template Cast<T>();
}

template <class T> WeakRef<T> &WeakRef<T>::operator=(
    const WeakRef &other) {
  if (&other != this) {
    UseBlock(other._block);
  }
  return *this;
}

template <class T> WeakRef<T> &WeakRef<T>::operator=(
    WeakRef &&other) noexcept {
  if (&other != this) {
    UseBlock(other._block);
  }
  return *this;
}

template <class T> template <typename E, typename> WeakRef<T> &WeakRef<
  T>::operator=(const WeakRef<E> &other) {
  *this = other.template Cast<T>();
  return *this;
}

template <class T> template <typename E, typename> WeakRef<T> &WeakRef<
  T>::operator=(WeakRef<E> &&other) noexcept {
  *this = other.template Cast<T>();
  return *this;
}

template <class T> template <typename E, typename> WeakRef<T>::operator
WeakRef<E>() {
  return WeakRef<E>(reinterpret_cast<RefBlock<E> *>(_block));
}

template <class T> template <typename E, typename> WeakRef<T>::operator
WeakRef<E>() const {
  return WeakRef<E>(reinterpret_cast<RefBlock<E> *>(_block));
}

template <class T> template <typename E> WeakRef<E> WeakRef<
  T>::Cast() const {
  if(_block == nullptr) return {};

  if(_block->bIsPendingDelete) return {};

  if(auto castResult = dynamic_cast<E *>(_block->data)) {
    return WeakRef<E>(reinterpret_cast<RefBlock<E> *>(_block));
  }
  
  return {};
}

template <class T> template <typename E> WeakRef<E> WeakRef<
  T>::CastStatic() const {
  return WeakRef<E>(reinterpret_cast<RefBlock<E> *>(_block));
}

template <class T> WeakRef<T>::operator bool() const {
  return _block != nullptr && !_block->bIsPendingDelete && _block->data !=
         nullptr;
}

template <class T> Ref<T> WeakRef<T>::Reserve() {
  return Ref<T>(_block);
}

template <class T> Ref<T> WeakRef<T>::Reserve() const {
  return Ref<T>(_block);
}

template <class T> void WeakRef<T>::Clear(bool bCheckRef) {
  if(bCheckRef) {
    if (_block != nullptr && !_block->bIsPendingDelete) {
      _block->Lock();
      _block->weak.erase(this);
      _block->Unlock();
    }
  }
  _block = nullptr;
}

template <class T> WeakRef<T>::~WeakRef() {
  Clear(true);
}
}
