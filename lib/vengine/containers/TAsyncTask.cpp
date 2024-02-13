#include "vengine/containers/TAsyncTask.hpp"

namespace vengine {

ETaskState AsyncTaskBase::GetState() const {
  return _state;
}

void AsyncTaskBase::Join() {
  if(_taskThread.joinable()) {
    _taskThread.join();
  }
}


std::string AsyncTaskBase::GetId() const {
  return _id;
}


Managed<AsyncTaskManager> AsyncTaskManager::_instance = Managed(new AsyncTaskManager());;

AsyncTaskManager::AsyncTaskManager() {
  log::engine->info("Created Task Manager");
}

AsyncTaskManager * AsyncTaskManager::Get() {
  return AsyncTaskManager::_instance.Get();
}

void AsyncTaskManager::Clear() {
  _tasks.clear();
  for(auto pending : _tasksPendingJoin) {
    pending->Join();
  }
  _tasksPendingJoin.clear();
}

void AsyncTaskManager::Tick(float deltaTime) {
  for(auto pending : _tasksPendingJoin) {
    pending->Join();
  }
  _tasksPendingJoin.clear();
}
}