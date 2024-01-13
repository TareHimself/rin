#pragma once
#include "log.hpp"
#include "types.hpp"
#include "containers/Array.hpp"

#include <deque>
#include <functional>
#include <iostream>

namespace vengine {
  

  class Allocatable {
  public:
    bool _bWasAllocated = false;
  };

  template <class OuterType>
  class Object : public Allocatable {
    bool bHasBeenInitialized = false;
    CleanupQueue cleaner;
    OuterType * _outer = nullptr;
    
  protected:
    
    void addCleanup(const std::function<void()> &callback);
  public:
    OuterType * getOuter() const;

    // void * operator new(size_t size);

    virtual bool hasBeenInitialized() const;
    Object() = default;
    
    virtual ~Object();
    
    virtual void init(OuterType * outer);

    void cleanup();
    
    virtual void handleCleanup();
    
  };

  template <class OuterType> void Object<OuterType>::addCleanup(
      const std::function<void()> &callback) {
    cleaner.push(callback);
  }

  template <class OuterType> OuterType * Object<OuterType>::getOuter() const {
    return _outer;
  }

  template <class OuterType> bool Object<OuterType>::hasBeenInitialized() const {
    return bHasBeenInitialized;
  }

  template <class OuterType> Object<OuterType>::~Object() {
  }

  template <class OuterType> void Object<OuterType>::init(OuterType *outer) {
    _outer = outer;
    bHasBeenInitialized = true;
  }

  template <class OuterType> void Object<OuterType>::cleanup() {
    
    handleCleanup();
    if(_bWasAllocated) {
      log::engine->info("Cleaned up allocated object");
      delete this;
    }
    bHasBeenInitialized = false;
  }

  template <class OuterType> void Object<OuterType>::handleCleanup() {
    cleaner.run();
  }

template <typename T>
static T *newObject() {
    auto obj = new T();
    static_cast<Allocatable*>(obj)->_bWasAllocated = true;
    log::engine->info("Allocated object");
    return obj;
}
}
