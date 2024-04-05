#pragma once
#include "aerox/EngineSubsystem.hpp"
#include "aerox/utils.hpp"
#include <queue>
#include "gen/async/AsyncSubsystem.gen.hpp"

namespace aerox::async {
class Task;

template<typename T>
class TaskWithReturn;

META_TYPE()
class AsyncSubsystem : public EngineSubsystem {
public:
  String GetName() const override;

  META_BODY()
protected:
  std::list<std::thread> _pool;
  std::queue<std::shared_ptr<Task>> _tasks;
  std::condition_variable _taskCond;
  std::mutex _taskMutex;

  
public:

  virtual void InitTask(const std::shared_ptr<Task>& task);
  
  void OnInit(Engine *subsystem) override;

  void RunOneThread();

  void EnqueueTask(const std::shared_ptr<Task>& task);
  
  void OnDestroy() override;

  virtual void StopAll();
  
};


}
