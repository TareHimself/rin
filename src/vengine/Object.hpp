#pragma once
#include "log.hpp"
#include "containers/Array.hpp"

#include <deque>
#include <functional>
#include <iostream>

namespace vengine {
  typedef std::function<void()> cleanupCallback;

  class Allocatable {
  public:
    bool _bWasAllocated = false;
  };

  template <class OuterType>
  class Object : public Allocatable {

  private:
    std::deque<cleanupCallback> destroyCallbacks;
    OuterType * _outer = nullptr;
    
  protected:
    
    void addCleanup(cleanupCallback callback);
  public:
    OuterType * getOuter() const;

    // void * operator new(size_t size);

    Object() = default;
    
    virtual ~Object();
    
    virtual void init(OuterType * outer);

    void cleanup();
    
    virtual void onCleanup();
    
  };

  template <class OuterType> void Object<OuterType>::addCleanup(
      cleanupCallback callback) {
    destroyCallbacks.push_front(std::move(callback));
  }

  template <class OuterType> OuterType * Object<OuterType>::getOuter() const {
    return _outer;
  }

  // template <class OuterType> void * Object<OuterType>::operator
  // new(size_t size) {
  //   void * p = ::operator new(size); 
  //   //void * p = malloc(size); will also work fine
  //   
  //   const auto obj = reinterpret_cast<Object *>(p);
  //   obj->_bWasAllocated = true;
  //   //log::engine->info("New called on class" );
  //   log::engine->info("Allocating Class");
  //   return p;
  // }

  template <class OuterType> Object<OuterType>::~Object() {
  }

  template <class OuterType> void Object<OuterType>::init(OuterType *outer) {
    _outer = outer;
  }

  template <class OuterType> void Object<OuterType>::cleanup() {
    onCleanup();
    if(_bWasAllocated) {
      log::engine->info("Cleaned up allocated object");
      delete this;
    }
  }

  template <class OuterType> void Object<OuterType>::onCleanup() {
    for(const auto &cleanupFn : destroyCallbacks) {
      cleanupFn();
    }

    destroyCallbacks.clear();
  }

template <typename T>
static T *newObject() {
    auto obj = new T();
    static_cast<Allocatable*>(obj)->_bWasAllocated = true;
    log::engine->info("Allocated object");
    return obj;
}
}
