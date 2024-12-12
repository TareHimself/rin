#include "rin/io/TaskRunner.h"


namespace rin::io
{
    void TaskRunner::__RunTask(const Shared<Task>& task)
    {
        {
            std::lock_guard m(_queueMutex);
            _tasks.push(task);
            _cond.notify_one();
        }
    }

    void TaskRunner::HandleThread()
    {
        std::unique_lock m(_notifyMutex);
        
        while(_running)
        {
            _cond.wait(m);

            if(!_running) return;

            
            Shared<Task> toRun{};
            {
                std::lock_guard guard(_queueMutex);
                if(!_tasks.empty())
                {
                    toRun = _tasks.front();
                    _tasks.pop();
                }
            }
            if(toRun)
            {
                toRun->Run();
            }
        }
    }

    TaskRunner::TaskRunner(const uint32_t& numThreads)
    {
        for(auto i = 0; i < numThreads; i++)
        {
            _threads.emplace_back([this]
            {
                HandleThread();   
            });
        }
    }

    TaskRunner::TaskRunner() : TaskRunner(std::thread::hardware_concurrency())
    {
        
    }

    TaskRunner::~TaskRunner()
    {
        _running = false;
        _cond.notify_all();
        {
            std::lock_guard guard(_queueMutex);
            _tasks = {};
        }
        for (auto &thread : _threads)
        {
            thread.join();
        }

        _threads.clear();
        
    }
}
