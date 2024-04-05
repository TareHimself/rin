#pragma once
#include "AsyncSubsystem.hpp"
#include "aerox/Engine.hpp"
#include "aerox/TOwnedBy.hpp"

namespace aerox::async {

class Task : public TOwnedBy<AsyncSubsystem> {
protected:
  friend AsyncSubsystem;
  virtual void Run() = 0;

public:
  DECLARE_DELEGATE(onException,std::exception&)
  virtual void Enqueue();
};

template<typename T>
class TaskWithReturn  : public Task {
  std::function<T()> _func;
protected:
  void Run() override;
public:
  TaskWithReturn(std::function<T()> func);

  DECLARE_DELEGATE(onCompleted,T)
};

class TaskNoReturn  : public Task {
  std::function<void()> _func;
protected:
  void Run() override;
public:
  TaskNoReturn(std::function<void()> func);
  
  DECLARE_DELEGATE(onCompleted)
  
};

template <typename T> void TaskWithReturn<T>::Run() {
  onCompleted->Execute(_func());
}

template <typename T> TaskWithReturn<T>::TaskWithReturn(std::function<T()> func) {
  _func = std::move(func);
}


template<typename T,typename  = std::enable_if_t<!std::is_void_v<T>>>
std::shared_ptr<TaskWithReturn<T>> newTask(const std::function<T()>& func) {
  auto t = newObject<TaskWithReturn<T>>(func);
  Engine::Get()->GetAsyncSubsystem().lock()->InitTask(utils::castStatic<Task>(t));
  return t;
}

std::shared_ptr<TaskNoReturn> newTask(const std::function<void()>& func);

}
