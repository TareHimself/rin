#include "aerox/async/Task.hpp"


namespace aerox::async {
void Task::Enqueue() {
  GetOwner()->EnqueueTask(utils::cast<Task>(this->shared_from_this()));
}

void TaskNoReturn::Run() {
  _func();
}

TaskNoReturn::TaskNoReturn(std::function<void()> func) {
  _func = std::move(func);
}

std::shared_ptr<TaskNoReturn> newTask(const std::function<void()> &func) {
  auto t = newObject<TaskNoReturn>(func);
  Engine::Get()->GetAsyncSubsystem().lock()->InitTask(utils::castStatic<Task>(t));
  return t;
}
}
