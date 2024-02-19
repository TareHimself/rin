#pragma once

#include "TDispatcher.hpp"
#include "vengine/Managed.hpp"
#include "vengine/utils.hpp"
#include <functional>
#include <future>

namespace vengine {

enum ETaskState {
  TaskState_Idle,
  TaskState_Running,
  TaskState_Finished
};
class AsyncTaskBase {
  
protected:
  std::string _id;
  ETaskState _state = ETaskState::TaskState_Idle;
  std::thread _taskThread;
public:
  
  TDispatcher<> _internal_onFinish;
  std::string GetId() const;
  ETaskState GetState() const;
  void Join();
};

template<class T>
class TAsyncTask;

class AsyncTaskManager {
  std::unordered_map<std::string,Managed<AsyncTaskBase>> _tasks;
  std::vector<Managed<AsyncTaskBase>> _tasksPendingJoin;

  static Managed<AsyncTaskManager> _instance;
public:
  
  template<typename T>
  Managed<TAsyncTask<T>> CreateTask(const std::function<T()>& task);

  void Tick(float deltaTime);

  AsyncTaskManager();

  static AsyncTaskManager * Get();

  void Clear();
};

template<typename T>
class TAsyncTask : public AsyncTaskBase {
  std::function<T()> _taskFn;
  
public:

  TAsyncTask(const std::function<T()>& task);
  TDispatcher<T> onFinish;
  
  void Run();
};

template <typename T> Managed<TAsyncTask<T>> AsyncTaskManager::CreateTask(
    const std::function<T()>& task) {
  if(auto asyncTask = Managed<TAsyncTask<T>>(new TAsyncTask<T>(task))) {
    auto taskId = asyncTask->GetId();
    
    _tasks.emplace(taskId,asyncTask);
    
    asyncTask->_internal_onFinish.Bind([this,taskId] {
      _tasksPendingJoin.push_back(_tasks[taskId]);
      _tasks.erase(_tasks.find(taskId));
    });

    return asyncTask;
  }

  return {};
}

template <class T> TAsyncTask<T>::TAsyncTask(const std::function<T()>& task) {
  _taskFn = task;
  _id = utils::createUUID();
}

template <class T> void TAsyncTask<T>::Run() {
  // _taskThread = std::thread([this] {
  //   _state = ETaskState::TaskState_Running;
  //   auto result = _taskFn();
  //   _state = ETaskState::TaskState_Finished;
  //   onFinish(result);
  //   _internal_onFinish();
  // });
  _state = ETaskState::TaskState_Running;
  auto result = _taskFn();
  _state = ETaskState::TaskState_Finished;
  onFinish(result);
  _internal_onFinish();
}

}
