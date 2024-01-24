#pragma once
#include "log.hpp"
#include "types.hpp"
#include "containers/TEventDispatcher.hpp"
#include <functional>
#include <iostream>

namespace vengine {
  

  class Allocatable {
  public:
    bool bWasAllocated = false;
  };

  template <class OuterType>
  class Object : public Allocatable {
    bool _bHasBeenInitialized = false;
    CleanupQueue _cleaner;
    OuterType * _outer = nullptr;
    bool _bPendingDestroy = false;
  protected:
    
    void AddCleanup(const std::function<void()> &callback);
  public:

    TEventDispatcher<> onDestroyed;
    
    OuterType * GetOuter() const;

    // void * operator new(size_t size);

    virtual bool HasBeenInitialized() const;
    Object() = default;
    
    virtual ~Object();
    
    virtual void Init(OuterType *outer);

    virtual void Destroy();
    
    virtual void HandleDestroy();
    bool IsPendingDestroy();
  };

  template <class OuterType> void Object<OuterType>::AddCleanup(
      const std::function<void()> &callback) {
    _cleaner.Push(callback);
  }

  template <class OuterType> OuterType * Object<OuterType>::GetOuter() const {
    return _outer;
  }

  template <class OuterType> bool Object<OuterType>::HasBeenInitialized() const {
    return _bHasBeenInitialized;
  }

  template <class OuterType> Object<OuterType>::~Object() {
  }

  template <class OuterType> void Object<OuterType>::Init(OuterType *outer) {
    _outer = outer;
    _bHasBeenInitialized = true;
  }

  template <class OuterType> void Object<OuterType>::Destroy() {
    _bPendingDestroy = true;
    onDestroyed.Emit();
    if(HasBeenInitialized()) {
      HandleDestroy();
    }
    if(bWasAllocated) {
      log::engine->info("Cleaned up allocated object");
      delete this;
    }
    _bHasBeenInitialized = false;
  }

  template <class OuterType> void Object<OuterType>::HandleDestroy() {
    _cleaner.Run();
  }

  template <class OuterType> bool Object<OuterType>::IsPendingDestroy() {
    return  _bPendingDestroy;
  }

  template <typename T, typename... Args>
static T *newObject(Args&&... args) {
    static_assert(std::is_base_of_v<Allocatable, T>, "T must be a child of Allocatable");
    auto obj = new T(args...);
    static_cast<Allocatable*>(obj)->bWasAllocated = true;
    log::engine->info("Allocated object");
    return obj;
}
}
