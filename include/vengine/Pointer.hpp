#pragma once
#include "log.hpp"

#include <functional>
#include <mutex>
#include <typeinfo>
#include <reflect/Factory.hpp>
#include <reflect/Reflect.hpp>

namespace vengine {
template <class T>
class WeakPointer;

template <class T>
class Pointer;

template <class T>
struct PointerBlock {
  friend class Pointer<T>;
  friend class WeakPointer<T>;
  T *data = nullptr;
  std::mutex * mutex = nullptr;
  uint64_t locks = 0;
  std::list<WeakPointer<T> *> weak{};
  std::list<Pointer<T> *> strong{};
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

  PointerBlock() {
    mutex = new std::mutex;
    if(auto reflected = reflect::factory::find<T>()) {
      log::engine->info("Created control block for {}",reflected->GetName());
    } else {
      log::engine->info("Created control block for unknown type");
    }
  }

  ~PointerBlock() {
    if(auto reflected = reflect::factory::find<T>()) {
      log::engine->info("Deleting control block for {}",reflected->GetName());
    } else {
      log::engine->info("Deleting control block for unknown type");
    }
    delete mutex;
    mutex = nullptr;
  }
};

template <class T>
class Pointer {
  PointerBlock<T> *_block = nullptr;

protected:
  void UseBlock(PointerBlock<T> * block);
public:
  Pointer();
  Pointer(std::nullptr_t);
  Pointer(PointerBlock<T> *block);
  Pointer(T *data);
  Pointer(T *data, std::function<void(T *)> deleter);
  Pointer(Pointer &other);
  Pointer(const Pointer &other);
  Pointer(Pointer &&other) noexcept;
  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  Pointer(const Pointer<E> &other);
  Pointer &operator=(const Pointer &other);
  Pointer &operator=(Pointer &&other) noexcept;

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  Pointer &operator=(const Pointer<E> &other);

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  Pointer &operator=(Pointer<E> &&other) noexcept;

  T *Get();
  T *Get() const;

  template <typename E>
  Pointer<E> Cast() const;

  template <typename E>
  Pointer<E> CastStatic() const;

  bool operator==(const Pointer &other) const;

  operator WeakPointer<T>();
  operator WeakPointer<T>() const;

  // operator T*();
  // operator T*() const;
  T *operator->();
  T *operator->() const;

  operator bool() const;

  void Clear();
  void ForceClear();

  Pointer<T> &Swap(T *data);
  Pointer<T> &Swap(T *data, std::function<void(T *)> deleter);

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator Pointer<E>();

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator Pointer<E>() const;

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator WeakPointer<E>();

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator WeakPointer<E>() const;

  ~Pointer();
  friend class WeakPointer<T>;
};


template <class T>
class WeakPointer {
  PointerBlock<T> *_block = nullptr;

protected:
  void UseBlock(PointerBlock<T> * block);
public:
  WeakPointer();
  WeakPointer(PointerBlock<T> *block);
  WeakPointer(const WeakPointer &other);
  WeakPointer(WeakPointer &&other) noexcept;
  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  WeakPointer(const WeakPointer<E> &other);

  WeakPointer &operator=(const WeakPointer &other);
  WeakPointer &operator=(WeakPointer &&other) noexcept;

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  WeakPointer &operator=(const WeakPointer<E> &other);
  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  WeakPointer &operator=(WeakPointer<E> &&other) noexcept;

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator WeakPointer<E>();

  template <typename E, typename = std::enable_if_t<std::is_base_of_v<T, E>>>
  operator WeakPointer<E>() const;

  template <typename E>
  WeakPointer<E> Cast() const;

  template <typename E>
  WeakPointer<E> CastStatic() const;

  operator bool() const;

  Pointer<T> Reserve();
  Pointer<T> Reserve() const;

  void Clear(bool bCheckRef);
  ~WeakPointer();
  friend class Pointer<T>;
};

template <class T> void Pointer<T>::UseBlock(PointerBlock<T> *block) {
  Clear();
  _block = block;
  if (_block != nullptr && !_block->bIsPendingDelete) {
    _block->Lock();
    _block->strong.push_back(this);
    _block->Unlock();
  }
}

template <class T> Pointer<T>::Pointer() {
  _block = nullptr;
}

template <class T> Pointer<T>::Pointer(std::nullptr_t) {
  _block = nullptr;
}

template <class T> Pointer<T>::Pointer(PointerBlock<T> *block) {
  UseBlock(block);
}

template <class T> Pointer<T>::Pointer(T *data) {
  _block = new PointerBlock<T>;
  _block->data = data;
  _block->strong.push_back(this);
}

template <class T> Pointer<T>::Pointer(T *data,
                                       std::function<void(T *)> deleter) {
  _block = new PointerBlock<T>;
  _block->data = data;
  _block->strong.push_back(this);
  _block->deleter = deleter;
}

template <class T> Pointer<T>::Pointer(Pointer &other) {
  UseBlock(other._block);
}

template <class T> Pointer<T>::Pointer(const Pointer &other) {
  UseBlock(other._block);
}

template <class T> Pointer<T>::Pointer(Pointer &&other) noexcept {
  UseBlock(other._block);
}

template <class T> template <typename E, typename> Pointer<T>::Pointer(
    const Pointer<E> &other) {
  *this = other.template Cast<T>();
}

template <class T> Pointer<T> &Pointer<T>::operator=(const Pointer &other) {
  if (&other != this) {
    UseBlock(other._block);
  }
  return *this;
}

template <class T> Pointer<T> &Pointer<T>::operator
=(Pointer &&other) noexcept {
  if (&other != this) {
    UseBlock(other._block);
  }
  return *this;
}

template <class T> template <typename E, typename> Pointer<T> &Pointer<T>::
operator=(const Pointer<E> &other) {
  if (&other != this) {
    *this = other.template Cast<T>();
  }
  return *this;
}

template <class T> template <typename E, typename> Pointer<T> &Pointer<T>::
operator=(Pointer<E> &&other) noexcept {
  if (&other != this) {
    *this = other.template Cast<T>();
  }
  return *this;
}

template <class T> T *Pointer<T>::Get() {
  if (_block == nullptr || _block->bIsPendingDelete)
    return nullptr;
  return _block->data;
}

template <class T> T *Pointer<T>::Get() const {
  if (_block == nullptr || _block->bIsPendingDelete)
    return nullptr;
  return _block->data;
}

template <class T> template <typename E> Pointer<E> Pointer<T>::Cast() const {
  if (_block == nullptr || _block->bIsPendingDelete || !(
        std::is_base_of_v<T, E> || std::is_base_of_v<E, T>)) {
    return {};
  }

  return Pointer<E>(reinterpret_cast<PointerBlock<E> *>(_block));
}

template <class T> template <typename E> Pointer<E> Pointer<
  T>::CastStatic() const {
  return Pointer<E>(reinterpret_cast<PointerBlock<E> *>(_block));
}

template <class T> bool Pointer<T>::operator==(const Pointer &other) const {
  return _block == other._block;
}

template <class T> Pointer<T>::operator WeakPointer<T>() {
  return WeakPointer<T>(_block);
}

template <class T> Pointer<T>::operator WeakPointer<T>() const {
  return WeakPointer<T>(_block);
}

template <class T> T *Pointer<T>::operator->() {
  if (_block == nullptr || _block->bIsPendingDelete)
    return nullptr;
  return _block->data;
}

template <class T> T *Pointer<T>::operator->() const {
  if (_block == nullptr || _block->bIsPendingDelete)
    return nullptr;
  return _block->data;
}

template <class T> Pointer<T>::operator bool() const {
  if (_block == nullptr || _block->bIsPendingDelete)
    return false;
  return _block->data != nullptr;
}

template <class T> void Pointer<T>::Clear() {
  if (_block != nullptr && !_block->bIsPendingDelete) {
    _block->Lock();
    _block->strong.remove(this);
    if (_block->strong.empty()) {
      _block->bIsPendingDelete = true;
    }
    _block->Unlock();
  }

  _block = nullptr;
}

template <class T> void Pointer<T>::ForceClear() {
  if (_block != nullptr && !_block->bIsPendingDelete) {
    _block->Lock();
    _block->strong.remove(this);
    _block->bIsPendingDelete = true;

    _block->strong.clear();
    _block->weak.clear();
    _block->Unlock();
  }

  _block = nullptr;
}

template <class T> Pointer<T> &Pointer<T>::Swap(T *data) {
  UseBlock(new PointerBlock<T>);
  return *this;
}

template <class T> Pointer<T> &Pointer<T>::Swap(T *data,
                                                std::function<void(T *)>
                                                deleter) {
  UseBlock(new PointerBlock<T>);
  
  return *this;
}

template <class T> template <typename E, typename> Pointer<T>::operator Pointer<
  E>() {
  return Cast<E>();
}

template <class T> template <typename E, typename> Pointer<T>::operator Pointer<
  E>() const {
  return Cast<E>();
}

template <class T> template <typename E, typename> Pointer<T>::operator
WeakPointer<E>() {
  return WeakPointer<E>(Cast<E>());
}

template <class T> template <typename E, typename> Pointer<T>::operator
WeakPointer<E>() const {
  return WeakPointer<E>(Cast<E>());
}

template <class T> Pointer<T>::~Pointer() {
  Clear();
}

template <class T> void WeakPointer<T>::UseBlock(PointerBlock<T> *block) {
  Clear(true);
  _block = block;
  if (_block != nullptr && !_block->bIsPendingDelete) {
    _block->Lock();
    _block->weak.push_back(this);
    _block->Unlock();
  }
}

template <class T> WeakPointer<T>::WeakPointer() {
  _block = nullptr;
}

template <class T> WeakPointer<T>::WeakPointer(PointerBlock<T> *block) {
  UseBlock(block);
}

template <class T> WeakPointer<T>::WeakPointer(const WeakPointer &other) {
  UseBlock(other._block);
}

template <class T> WeakPointer<T>::WeakPointer(WeakPointer &&other) noexcept {
  UseBlock(other._block);
}

template <class T> template <typename E, typename> WeakPointer<T>::WeakPointer(
    const WeakPointer<E> &other) {
  *this = other.template Cast<T>();
}

template <class T> WeakPointer<T> &WeakPointer<T>::operator=(
    const WeakPointer &other) {
  if (&other != this) {
    UseBlock(other._block);
  }
  return *this;
}

template <class T> WeakPointer<T> &WeakPointer<T>::operator=(
    WeakPointer &&other) noexcept {
  if (&other != this) {
    UseBlock(other._block);
  }
  return *this;
}

template <class T> template <typename E, typename> WeakPointer<T> &WeakPointer<
  T>::operator=(const WeakPointer<E> &other) {
  *this = other.template Cast<T>();
  return *this;
}

template <class T> template <typename E, typename> WeakPointer<T> &WeakPointer<
  T>::operator=(WeakPointer<E> &&other) noexcept {
  *this = other.template Cast<T>();
  return *this;
}

template <class T> template <typename E, typename> WeakPointer<T>::operator
WeakPointer<E>() {
  return WeakPointer<E>(reinterpret_cast<PointerBlock<E> *>(_block));
}

template <class T> template <typename E, typename> WeakPointer<T>::operator
WeakPointer<E>() const {
  return WeakPointer<E>(reinterpret_cast<PointerBlock<E> *>(_block));
}

template <class T> template <typename E> WeakPointer<E> WeakPointer<
  T>::Cast() const {
  if (_block == nullptr || _block->bIsPendingDelete || !(
        std::is_base_of_v<T, E> || std::is_base_of_v<E, T>)) {
    return {};
  }

  return Pointer<E>(reinterpret_cast<PointerBlock<E> *>(_block));
}

template <class T> template <typename E> WeakPointer<E> WeakPointer<
  T>::CastStatic() const {
  return Pointer<E>(reinterpret_cast<PointerBlock<E> *>(_block));
}

template <class T> WeakPointer<T>::operator bool() const {
  return _block != nullptr && !_block->bIsPendingDelete && _block->data !=
         nullptr;
}

template <class T> Pointer<T> WeakPointer<T>::Reserve() {
  return Pointer<T>(_block);
}

template <class T> Pointer<T> WeakPointer<T>::Reserve() const {
  return Pointer<T>(_block);
}

template <class T> void WeakPointer<T>::Clear(bool bCheckRef) {
  if(bCheckRef) {
    if (_block != nullptr && !_block->bIsPendingDelete) {
      _block->Lock();
      _block->weak.remove(this);
      _block->Unlock();
    }
  }
  _block = nullptr;
}

template <class T> WeakPointer<T>::~WeakPointer() {
  Clear(true);
}
}
