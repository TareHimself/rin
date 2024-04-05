#include "aerox/async/AsyncSubsystem.hpp"
#include "aerox/async/Task.hpp"


namespace aerox::async {

String AsyncSubsystem::GetName() const {
  return "async";
}

void AsyncSubsystem::InitTask(const std::shared_ptr<Task> &task) {
  task->Init(this);
}

void AsyncSubsystem::OnInit(Engine *subsystem) {
  EngineSubsystem::OnInit(subsystem);
  auto numThreads = std::thread::hardware_concurrency();
  for(auto i = 0; i < numThreads; i++) {
    _pool.emplace_back([this] { RunOneThread(); });
  }

  GetLogger()->Info("Created task pool with {} threads",numThreads);
}

void AsyncSubsystem::RunOneThread() {
  while(true) {
    if(_tasks.empty()) {
      std::unique_lock<std::mutex> l(_taskMutex);
      _taskCond.wait(l);
    }

    _taskMutex.lock();
    if(!_tasks.empty()) {
      
      const auto front = _tasks.front();
      _tasks.pop();
      _taskMutex.unlock();

      if(!front) {
        break;
      }
      
      try {
        front->Run();
        GetLogger()->Info("Task completed, {} pending",_tasks.size());
      } catch (std::exception& e) {
        front->onException->Execute(e);
      }
      
    }
    else {
      _taskMutex.unlock();
    }
  }
}

void AsyncSubsystem::EnqueueTask(const std::shared_ptr<Task> &task) {
  _tasks.emplace(task);
  _taskCond.notify_one();
  GetLogger()->Info("Enqueued task, {} pending",_tasks.size());
}

void AsyncSubsystem::OnDestroy() {
  StopAll();
  EngineSubsystem::OnDestroy();
}

void AsyncSubsystem::StopAll() {
  decltype(_tasks)().swap(_tasks);
  for(auto i = 0; i < _pool.size(); i++) {
    _tasks.emplace();
  }
  _taskCond.notify_all();
  for(auto &q : _pool) {
    q.join();
  }
  _pool.clear();
}
}
