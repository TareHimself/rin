#pragma once
#include "Managed.hpp"
#include "log.hpp"
#include "types.hpp"
#include "containers/TDispatcher.hpp"


namespace vengine {

  template<typename T>
  std::shared_ptr<reflect::wrap::Reflected> findReflectedByPtr(T * inst) {
    return reflect::factory::find<T>();
  }
  class Allocatable {
  
  public:
    bool __internal__isOnStack = true;
    virtual void Destroy() = 0;
  };


  class Cleanable : public Allocatable {
  protected:
    CleanupQueue _cleaner;
  public:
    void AddCleanup(const std::function<void()> &callback);

    template <typename ...T>
    void AddCleanup(TDispatcher<T...>&dispatcher,uint64_t bindId);

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
    bool _hasBeenInitialized = false;

    bool _bPendingDestroy = false;
    OuterType * _outer;
  
  public:

    TDispatcher<> onDestroyed;
    
    OuterType * GetOuter() const;

    // void * operator new(size_t size);

    virtual bool IsInitialized() const;
    Object();
    
    virtual ~Object();
    
    virtual void Init(OuterType * outer);

    virtual void Destroy() override;
    
    virtual void BeforeDestroy();
    
    bool IsPendingDestroy() const;
  };

  template <typename ...T> void Cleanable::AddCleanup(
      TDispatcher<T...> &dispatcher, uint64_t bindId) {
    AddCleanup([&dispatcher,bindId] {
      dispatcher.UnBind(bindId);
    });
  }

  template <class OuterType> OuterType * Object<OuterType>::GetOuter() const {
    return _outer;
  }

  template <class OuterType> bool Object<OuterType>::IsInitialized() const {
    return _hasBeenInitialized;
  }

  template <class OuterType> Object<OuterType>::Object() {
  }

  template <class OuterType> Object<OuterType>::~Object() {
  }

  template <class OuterType> void Object<OuterType>::Init(OuterType * outer) {
    _outer = outer;
    _hasBeenInitialized = true;
  }

  template <class OuterType> void Object<OuterType>::Destroy() {
    _bPendingDestroy = true;
    onDestroyed();
    if(IsInitialized()) {
      BeforeDestroy();
    }

    _hasBeenInitialized = false;
    
    if(!__internal__isOnStack) {
      delete this;
    }
  }

  template <class OuterType> void Object<OuterType>::BeforeDestroy() {
    _cleaner.Run();
  }

  template <class OuterType> bool Object<OuterType>::IsPendingDestroy() const {
    return  _bPendingDestroy;
  }

  template <typename T, typename... Args>
static T *newObject(Args&&... args) {
    static_assert(std::is_base_of_v<Allocatable, T>, "T must be a child of Allocatable");
    auto obj = new T(args...);
    static_cast<Allocatable*>(obj)->__internal__isOnStack = false;
    return obj;
}

template <typename T, typename... Args>
static Managed<T> newManagedObject(Args&&... args) {
    static_assert(std::is_base_of_v<Allocatable, T>, "T must be a child of Allocatable");
    auto obj = new T(std::forward<Args>(args)...);
    static_cast<Allocatable*>(obj)->__internal__isOnStack = false;
    
    return Managed<T>(obj,[](T * ptr) {
      static_cast<Allocatable *>(ptr)->Destroy();
    });
  }
}

