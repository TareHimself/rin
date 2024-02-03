#pragma once
#include "Ref.hpp"
#include "log.hpp"
#include "types.hpp"
#include "containers/TEventDispatcher.hpp"
// #include <functional>
// #include <iostream>

namespace vengine {
  

  class Allocatable {
  
  public:
    bool bWasAllocated = false;
    virtual void Destroy() = 0;
  };

  template <typename T>
  class SharableThis : public std::enable_shared_from_this<T> {
    
  };

  class Cleanable : public Allocatable {
  protected:
    CleanupQueue _cleaner;
  public:
    void AddCleanup(const std::function<void()> &callback);

    void Destroy() override;
  };

  inline void Cleanable::AddCleanup(const std::function<void()> &callback) {
    _cleaner.Push(callback);
  }

  inline void Cleanable::Destroy() {
    _cleaner.Run();
  }

  template <class OuterType>
  class Object : public Cleanable {
    bool _bHasBeenInitialized = false;

    bool _bPendingDestroy = false;
    OuterType * _outer;
    
  protected:
    
    
  public:

    TEventDispatcher<> onDestroyed;
    
    OuterType * GetOuter() const;

    // void * operator new(size_t size);

    virtual bool IsInitialized() const;
    Object();
    
    virtual ~Object();
    
    virtual void Init(OuterType * outer);

    virtual void Destroy() override;
    
    virtual void HandleDestroy();
    bool IsPendingDestroy() const;
  };

  template <class OuterType> OuterType * Object<OuterType>::GetOuter() const {
    return _outer;
  }

  template <class OuterType> bool Object<OuterType>::IsInitialized() const {
    return _bHasBeenInitialized;
  }

  template <class OuterType> Object<OuterType>::Object() {
  }

  template <class OuterType> Object<OuterType>::~Object() {
  }

  template <class OuterType> void Object<OuterType>::Init(OuterType * outer) {
    _outer = outer;
    _bHasBeenInitialized = true;
  }

  template <class OuterType> void Object<OuterType>::Destroy() {
    _bPendingDestroy = true;
    onDestroyed.Emit();
    if(IsInitialized()) {
      HandleDestroy();
    }
    if(bWasAllocated) {
      delete this;
    }
    _bHasBeenInitialized = false;
  }

  template <class OuterType> void Object<OuterType>::HandleDestroy() {
    _cleaner.Run();
  }

  template <class OuterType> bool Object<OuterType>::IsPendingDestroy() const {
    return  _bPendingDestroy;
  }

  template <typename T, typename... Args>
static T *newObject(Args&&... args) {
    static_assert(std::is_base_of_v<Allocatable, T>, "T must be a child of Allocatable");
    auto obj = new T(args...);
    static_cast<Allocatable*>(obj)->bWasAllocated = true;
    return obj;
}

template <typename T, typename... Args>
static Ref<T> newSharedObject(Args&&... args) {
    static_assert(std::is_base_of_v<Allocatable, T>, "T must be a child of Allocatable");
    auto obj = new T(std::forward<Args>(args)...);
    static_cast<Allocatable*>(obj)->bWasAllocated = true;
    
    return Ref<T>(obj,[](T * ptr) {
      static_cast<Allocatable *>(ptr)->Destroy();
    });
  }
}

